using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
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
    private readonly IUserSettingsService _settingsService;
    private readonly SuperQADbContext _context;
    private readonly IMemoryCache _cache;

    public PlaywrightController(
        IOpenAIService openAIService, 
        IPlaywrightTestExecutor playwrightTestExecutor, 
        IUserSettingsService settingsService,
        SuperQADbContext context,
        IMemoryCache cache)
    {
        _openAIService = openAIService;
        _playwrightTestExecutor = playwrightTestExecutor;
        _settingsService = settingsService;
        _context = context;
        _cache = cache;
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

            // Generate test script without page inspection
            // (Use extension for collecting locators instead)
            var generatedScript = await _openAIService.GeneratePlaywrightTestScriptAsync(
                request.FrsText,
                request.ApplicationUrl,
                request.OpenAIApiKey,
                request.Model,
                null); // No page structure - AI will use generic selectors

            return Ok(new PlaywrightTestGenerationResponse
            {
                Success = true,
                GeneratedScript = generatedScript
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

            var result = await _playwrightTestExecutor.ExecuteTestScriptAsync(
                request.TestScript, 
                request.ApplicationUrl, 
                request.DebugMode, 
                request.SlowMotion);
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
    public async Task<ActionResult<PlaywrightTestGenerationResponse>> GenerateFromExtension([FromBody] GenerateFromExtensionRequest request, [FromQuery] int? extensionDataId = null)
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

            // Use extension-provided locators instead of page inspection
            var pageStructure = GeneratePageStructureFromSteps(request.Steps);

            var generatedScript = await _openAIService.GeneratePlaywrightTestScriptAsync(
                frsText,
                request.ApplicationUrl,
                apiKey,
                model,
                pageStructure);

            // Find or create "Generated Tests" project
            var testsProject = await _context.Projects
                .FirstOrDefaultAsync(p => p.Name == "Generated Tests");
            
            if (testsProject == null)
            {
                testsProject = new Project
                {
                    Name = "Generated Tests",
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

            // Link test case to extension data if provided
            if (extensionDataId.HasValue)
            {
                var extensionData = await _context.ExtensionTestData
                    .FirstOrDefaultAsync(e => e.Id == extensionDataId.Value);
                
                if (extensionData != null)
                {
                    extensionData.TestCaseId = testCase.Id;
                    extensionData.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }
            }

            return Ok(new PlaywrightTestGenerationResponse
            {
                Success = true,
                GeneratedScript = generatedScript,
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

    [HttpPost("store-extension-data")]
    public async Task<ActionResult<object>> StoreExtensionData([FromBody] GenerateFromExtensionRequest request)
    {
        try
        {
            if (request == null)
            {
                return BadRequest(new { error = "Request body is required" });
            }

            if (request.Steps == null || !request.Steps.Any())
            {
                return BadRequest(new { 
                    error = "No test steps provided", 
                    message = "At least one test step is required. Please record some steps using the browser extension before sending to SuperQA."
                });
            }

            // Serialize steps to JSON
            string stepsJson;
            try
            {
                stepsJson = System.Text.Json.JsonSerializer.Serialize(request.Steps);
            }
            catch (System.Text.Json.JsonException jsonEx)
            {
                return BadRequest(new { 
                    error = "Failed to serialize test steps", 
                    message = "The test steps data is invalid.",
                    details = jsonEx.Message
                });
            }
            
            // Create new ExtensionTestData entity
            var extensionData = new ExtensionTestData
            {
                TestName = request.TestName ?? "Unnamed Test",
                ApplicationUrl = request.ApplicationUrl ?? string.Empty,
                StepsJson = stepsJson,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            
            _context.ExtensionTestData.Add(extensionData);
            await _context.SaveChangesAsync();
            
            return Ok(new { 
                dataId = extensionData.Id.ToString(), 
                message = "Data stored successfully",
                testName = extensionData.TestName,
                stepCount = request.Steps.Count
            });
        }
        catch (DbUpdateException dbEx)
        {
            return StatusCode(500, new { 
                error = "Database error", 
                message = "Failed to save the test data to the database. Please ensure the database is accessible and try again.",
                details = dbEx.Message
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { 
                error = "Internal server error", 
                message = "An unexpected error occurred while storing the extension data.",
                details = ex.Message
            });
        }
    }

    [HttpGet("get-extension-data/{dataId}")]
    public async Task<ActionResult<GenerateFromExtensionRequest>> GetExtensionData(string dataId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dataId))
            {
                return BadRequest(new { error = "Data ID is required" });
            }

            if (!int.TryParse(dataId, out int id))
            {
                return BadRequest(new { error = $"Invalid data ID format: '{dataId}'. The ID must be a valid number." });
            }
            
            var extensionData = await _context.ExtensionTestData
                .FirstOrDefaultAsync(e => e.Id == id);
            
            if (extensionData == null)
            {
                return NotFound(new { 
                    error = "Extension data not found", 
                    message = "The requested test data does not exist or may have been deleted. Please record your test steps again using the browser extension.",
                    dataId = dataId
                });
            }
            
            // Deserialize steps from JSON
            List<BrowserExtensionStep>? steps = null;
            try
            {
                steps = System.Text.Json.JsonSerializer.Deserialize<List<BrowserExtensionStep>>(extensionData.StepsJson);
            }
            catch (System.Text.Json.JsonException jsonEx)
            {
                return StatusCode(500, new { 
                    error = "Failed to deserialize test steps", 
                    message = "The stored test data is corrupted. Please record your test steps again.",
                    details = jsonEx.Message
                });
            }
            
            return Ok(new GenerateFromExtensionRequest
            {
                TestName = extensionData.TestName,
                ApplicationUrl = extensionData.ApplicationUrl,
                Steps = steps ?? new List<BrowserExtensionStep>()
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { 
                error = "Internal server error", 
                message = "An unexpected error occurred while retrieving the extension data. Please try again or contact support if the issue persists.",
                details = ex.Message 
            });
        }
    }

    [HttpPut("update-extension-data/{dataId}")]
    public async Task<ActionResult<object>> UpdateExtensionData(string dataId, [FromBody] GenerateFromExtensionRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dataId))
            {
                return BadRequest(new { error = "Data ID is required" });
            }

            if (!int.TryParse(dataId, out int id))
            {
                return BadRequest(new { 
                    error = "Invalid data ID format", 
                    message = $"The ID '{dataId}' is not a valid number."
                });
            }
            
            var extensionData = await _context.ExtensionTestData
                .FirstOrDefaultAsync(e => e.Id == id);
            
            if (extensionData == null)
            {
                return NotFound(new { 
                    error = "Extension data not found", 
                    message = "The test data you're trying to update does not exist or may have been deleted.",
                    dataId = dataId
                });
            }
            
            // Update fields
            extensionData.TestName = request.TestName ?? extensionData.TestName;
            extensionData.ApplicationUrl = request.ApplicationUrl ?? extensionData.ApplicationUrl;
            
            try
            {
                extensionData.StepsJson = System.Text.Json.JsonSerializer.Serialize(request.Steps);
            }
            catch (System.Text.Json.JsonException jsonEx)
            {
                return BadRequest(new { 
                    error = "Failed to serialize test steps", 
                    message = "The test steps data is invalid.",
                    details = jsonEx.Message
                });
            }
            
            extensionData.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            
            return Ok(new { 
                message = "Data updated successfully", 
                dataId = extensionData.Id.ToString() 
            });
        }
        catch (DbUpdateException dbEx)
        {
            return StatusCode(500, new { 
                error = "Database error", 
                message = "Failed to save the updated data to the database. Please try again.",
                details = dbEx.Message
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { 
                error = "Internal server error", 
                message = "An unexpected error occurred while updating the extension data.",
                details = ex.Message
            });
        }
    }

    [HttpDelete("delete-extension-data/{dataId}")]
    public async Task<ActionResult<object>> DeleteExtensionData(string dataId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dataId))
            {
                return BadRequest(new { error = "Data ID is required" });
            }

            if (!int.TryParse(dataId, out int id))
            {
                return BadRequest(new { 
                    error = "Invalid data ID format", 
                    message = $"The ID '{dataId}' is not a valid number."
                });
            }
            
            var extensionData = await _context.ExtensionTestData
                .FirstOrDefaultAsync(e => e.Id == id);
            
            if (extensionData == null)
            {
                return NotFound(new { 
                    error = "Extension data not found", 
                    message = "The test data you're trying to delete does not exist or has already been deleted.",
                    dataId = dataId
                });
            }
            
            _context.ExtensionTestData.Remove(extensionData);
            await _context.SaveChangesAsync();
            
            return Ok(new { 
                message = "Data deleted successfully",
                dataId = dataId
            });
        }
        catch (DbUpdateException dbEx)
        {
            return StatusCode(500, new { 
                error = "Database error", 
                message = "Failed to delete the data from the database. Please try again.",
                details = dbEx.Message
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { 
                error = "Internal server error", 
                message = "An unexpected error occurred while deleting the extension data.",
                details = ex.Message
            });
        }
    }

    [HttpGet("list-extension-data")]
    public async Task<ActionResult<object>> ListExtensionData()
    {
        try
        {
            var extensionDataList = await _context.ExtensionTestData
                .OrderByDescending(e => e.CreatedAt)
                .Select(e => new
                {
                    e.Id,
                    e.TestName,
                    e.ApplicationUrl,
                    e.CreatedAt,
                    e.UpdatedAt,
                    e.TestCaseId
                })
                .ToListAsync();
            
            return Ok(extensionDataList);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = $"Error retrieving data: {ex.Message}" });
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
            else if (step.Action?.ToLower() == "fill" || step.Action?.ToLower() == "type")
            {
                // Indicate that AI should generate test data for fill/type actions without values
                frs += $"   Value: [AI: Generate appropriate test data based on field name/type]\n";
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
