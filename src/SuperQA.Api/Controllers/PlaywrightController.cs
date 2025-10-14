using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuperQA.Core.Entities;
using SuperQA.Core.Interfaces;
using SuperQA.Infrastructure.Data;
using SuperQA.Shared.DTOs;

namespace SuperQA.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlaywrightController : ControllerBase
{
    private readonly IOpenAIService _openAIService;
    private readonly IPlaywrightTestExecutor _playwrightTestExecutor;
    private readonly IPageInspectorService _pageInspectorService;
    private readonly IUserSettingsService _settingsService;
    private readonly SuperQADbContext _context;

    public PlaywrightController(
        IOpenAIService openAIService, 
        IPlaywrightTestExecutor playwrightTestExecutor, 
        IPageInspectorService pageInspectorService, 
        IUserSettingsService settingsService,
        SuperQADbContext context)
    {
        _openAIService = openAIService;
        _playwrightTestExecutor = playwrightTestExecutor;
        _pageInspectorService = pageInspectorService;
        _settingsService = settingsService;
        _context = context;
    }

    [HttpPost("generate")]
    public async Task<ActionResult<PlaywrightTestGenerationResponse>> GenerateTest([FromBody] PlaywrightTestGenerationRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.FrsText))
                return BadRequest(new PlaywrightTestGenerationResponse { Success = false, ErrorMessage = "FRS text is required" });

            if (string.IsNullOrWhiteSpace(request.ApplicationUrl))
                return BadRequest(new PlaywrightTestGenerationResponse { Success = false, ErrorMessage = "Application URL is required" });

            if (string.IsNullOrWhiteSpace(request.OpenAIApiKey))
                return BadRequest(new PlaywrightTestGenerationResponse { Success = false, ErrorMessage = "OpenAI API key is required" });

            string? pageStructure = null;
            string? inspectionWarning = null;
            try
            {
                // Pass FRS to inspector to focus on relevant elements
                pageStructure = await _pageInspectorService.GetPageStructureAsync(request.ApplicationUrl, request.FrsText);
                if (pageStructure != null && pageStructure.Contains("\"error\""))
                {
                    inspectionWarning = "⚠️ Page inspection failed. The AI will generate test scripts with generic selectors. For best results, ensure Playwright browsers are installed (run 'playwright install chromium').";
                    Console.WriteLine($"WARNING: {inspectionWarning}");
                    pageStructure = null;
                }
            }
            catch (Exception ex)
            {
                inspectionWarning = "⚠️ Page inspection failed. The AI will generate test scripts with generic selectors. For best results, ensure Playwright browsers are installed (run 'playwright install chromium').";
                Console.WriteLine($"Page inspection failed: {ex.Message}");
            }

            var generatedScript = await _openAIService.GeneratePlaywrightTestScriptAsync(
                request.FrsText,
                request.ApplicationUrl,
                request.OpenAIApiKey,
                request.Model,
                pageStructure);

            return Ok(new PlaywrightTestGenerationResponse
            {
                Success = true,
                GeneratedScript = generatedScript,
                Warnings = inspectionWarning != null ? new[] { inspectionWarning } : null
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new PlaywrightTestGenerationResponse
            {
                Success = false,
                ErrorMessage = $"Error generating test script: {ex.Message}"
            });
        }
    }

    [HttpPost("execute")]
    public async Task<ActionResult<PlaywrightTestExecutionResponse>> ExecuteTest([FromBody] PlaywrightTestExecutionRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.TestScript))
                return BadRequest(new PlaywrightTestExecutionResponse { Success = false, Status = "Error", ErrorMessage = "Test script is required" });

            var result = await _playwrightTestExecutor.ExecuteTestScriptAsync(request.TestScript, request.ApplicationUrl);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new PlaywrightTestExecutionResponse
            {
                Success = false,
                Status = "Error",
                ErrorMessage = $"Error executing test: {ex.Message}"
            });
        }
    }

    [HttpPost("generate-from-extension")]
    public async Task<ActionResult<PlaywrightTestGenerationResponse>> GenerateFromExtension([FromBody] GenerateFromExtensionRequest request)
    {
        try
        {
            if (request.Steps == null || !request.Steps.Any())
                return BadRequest(new PlaywrightTestGenerationResponse { Success = false, ErrorMessage = "Steps are required" });

            if (string.IsNullOrWhiteSpace(request.ApplicationUrl))
                return BadRequest(new PlaywrightTestGenerationResponse { Success = false, ErrorMessage = "Application URL is required" });

            // Get API key and model from settings if not provided
            string apiKey = request.OpenAIApiKey ?? string.Empty;
            string model = request.Model ?? "gpt-4o-mini";

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                var settings = await _settingsService.GetSettingsAsync();
                if (settings != null && !string.IsNullOrWhiteSpace(settings.OpenAIApiKey))
                {
                    apiKey = settings.OpenAIApiKey;
                    model = settings.SelectedModel;
                }
                else
                {
                    return BadRequest(new PlaywrightTestGenerationResponse 
                    { 
                        Success = false, 
                        ErrorMessage = "OpenAI API key is required. Please configure it in Settings." 
                    });
                }
            }

            // Convert extension steps to FRS text
            var frsText = GenerateFrsFromSteps(request.Steps, request.TestName);

            string? pageStructure = null;
            string? inspectionWarning = null;
            try
            {
                pageStructure = await _pageInspectorService.GetPageStructureAsync(request.ApplicationUrl, frsText);
                if (pageStructure != null && pageStructure.Contains("\"error\""))
                {
                    inspectionWarning = "⚠️ Page inspection failed. The AI will use the locators from the extension.";
                    Console.WriteLine($"WARNING: {inspectionWarning}");
                    pageStructure = GeneratePageStructureFromSteps(request.Steps);
                }
            }
            catch (Exception ex)
            {
                inspectionWarning = "⚠️ Page inspection failed. The AI will use the locators from the extension.";
                Console.WriteLine($"Page inspection failed: {ex.Message}");
                pageStructure = GeneratePageStructureFromSteps(request.Steps);
            }

            var generatedScript = await _openAIService.GeneratePlaywrightTestScriptAsync(
                frsText,
                request.ApplicationUrl,
                apiKey,
                model,
                pageStructure);

            // Find or create "Tests" project
            var testsProject = await _context.Projects
                .FirstOrDefaultAsync(p => p.Name == "Tests");
            
            if (testsProject == null)
            {
                testsProject = new Project
                {
                    Name = "Tests",
                    Description = "Tests imported from browser extension",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Projects.Add(testsProject);
                await _context.SaveChangesAsync();
            }

            // Create test case from extension data
            var testCase = new TestCase
            {
                ProjectId = testsProject.Id,
                Title = request.TestName ?? "Extension Test",
                Description = $"Test imported from browser extension for {request.ApplicationUrl}",
                Preconditions = $"Navigate to: {request.ApplicationUrl}",
                Steps = frsText,
                ExpectedResults = "Test completes successfully",
                IsAIGenerated = true,
                AutomationScript = generatedScript,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.TestCases.Add(testCase);
            await _context.SaveChangesAsync();

            return Ok(new PlaywrightTestGenerationResponse
            {
                Success = true,
                GeneratedScript = generatedScript,
                Warnings = inspectionWarning != null ? new[] { inspectionWarning } : null,
                TestCaseId = testCase.Id,
                ProjectId = testsProject.Id
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new PlaywrightTestGenerationResponse
            {
                Success = false,
                ErrorMessage = $"Error generating test script: {ex.Message}"
            });
        }
    }

    private string GenerateFrsFromSteps(List<BrowserExtensionStep> steps, string? testName = null)
    {
        var frs = "";
        
        if (!string.IsNullOrWhiteSpace(testName))
        {
            frs += $"Test Name: {testName}\n\n";
        }
        
        frs += "Browser Extension Recorded Steps:\n\n";
        for (int i = 0; i < steps.Count; i++)
        {
            var step = steps[i];
            frs += $"{i + 1}. {step.Description}\n";
            if (!string.IsNullOrWhiteSpace(step.Action))
            {
                frs += $"   Action: {step.Action}\n";
            }
            if (!string.IsNullOrWhiteSpace(step.Locator))
            {
                frs += $"   Locator: {step.Locator}\n";
            }
            if (!string.IsNullOrWhiteSpace(step.Value))
            {
                frs += $"   Value: {step.Value}\n";
            }
            frs += "\n";
        }
        return frs;
    }

    private string GeneratePageStructureFromSteps(List<BrowserExtensionStep> steps)
    {
        var elements = new List<object>();
        foreach (var step in steps)
        {
            if (!string.IsNullOrWhiteSpace(step.Locator))
            {
                elements.Add(new
                {
                    type = step.Action,
                    selector = step.Locator,
                    description = step.Description,
                    value = step.Value
                });
            }
        }
        return System.Text.Json.JsonSerializer.Serialize(elements);
    }
}
