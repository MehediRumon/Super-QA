using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuperQA.Core.Entities;
using SuperQA.Core.Interfaces;
using SuperQA.Infrastructure.Data;
using SuperQA.Shared.DTOs;

namespace SuperQA.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestCasesController : ControllerBase
{
    private readonly SuperQADbContext _context;
    private readonly IAITestGeneratorService _aiTestGenerator;
    private readonly IPageInspectorService _pageInspectorService;

    public TestCasesController(SuperQADbContext context, IAITestGeneratorService aiTestGenerator, IPageInspectorService pageInspectorService)
    {
        _context = context;
        _aiTestGenerator = aiTestGenerator;
        _pageInspectorService = pageInspectorService;
    }

    [HttpGet("project/{projectId}")]
    public async Task<ActionResult<IEnumerable<TestCaseDto>>> GetTestCases(int projectId)
    {
        var testCases = await _context.TestCases
            .Where(tc => tc.ProjectId == projectId)
            .Select(tc => new TestCaseDto
            {
                Id = tc.Id,
                ProjectId = tc.ProjectId,
                RequirementId = tc.RequirementId,
                Title = tc.Title,
                Description = tc.Description,
                Preconditions = tc.Preconditions,
                Steps = tc.Steps,
                ExpectedResults = tc.ExpectedResults,
                IsAIGenerated = tc.IsAIGenerated,
                AutomationScript = tc.AutomationScript,
                CreatedAt = tc.CreatedAt,
                UpdatedAt = tc.UpdatedAt
            })
            .ToListAsync();

        return Ok(testCases);
    }

    [HttpPost("generate")]
    public async Task<ActionResult<IEnumerable<TestCaseDto>>> GenerateTestCases(GenerateTestCasesRequest request)
    {
        var requirement = await _context.Requirements.FindAsync(request.RequirementId);
        if (requirement == null)
            return NotFound("Requirement not found");

        var generatedTestCases = await _aiTestGenerator.GenerateTestCasesAsync(requirement);

        foreach (var testCase in generatedTestCases)
        {
            _context.TestCases.Add(testCase);
        }

        await _context.SaveChangesAsync();

        var dtos = generatedTestCases.Select(tc => new TestCaseDto
        {
            Id = tc.Id,
            ProjectId = tc.ProjectId,
            RequirementId = tc.RequirementId,
            Title = tc.Title,
            Description = tc.Description,
            Preconditions = tc.Preconditions,
            Steps = tc.Steps,
            ExpectedResults = tc.ExpectedResults,
            IsAIGenerated = tc.IsAIGenerated,
            AutomationScript = tc.AutomationScript,
            CreatedAt = tc.CreatedAt,
            UpdatedAt = tc.UpdatedAt
        });

        return Ok(dtos);
    }

    [HttpPost("generate-automation-script")]
    public async Task<ActionResult<GenerateAutomationScriptResponse>> GenerateAutomationScript([FromBody] GenerateAutomationScriptRequest request)
    {
        try
        {
            // Validate test case exists
            var testCase = await _context.TestCases.FindAsync(request.TestCaseId);
            if (testCase == null)
                return NotFound(new GenerateAutomationScriptResponse 
                { 
                    Success = false, 
                    ErrorMessage = "Test case not found" 
                });

            // Validate application URL
            if (string.IsNullOrWhiteSpace(request.ApplicationUrl))
                return BadRequest(new GenerateAutomationScriptResponse 
                { 
                    Success = false, 
                    ErrorMessage = "Application URL is required" 
                });

            // Inspect the actual page to get real selectors
            string? pageStructure = null;
            string? inspectionWarning = null;
            try
            {
                pageStructure = await _pageInspectorService.GetPageStructureAsync(request.ApplicationUrl);
                
                // Check if page inspection returned an error
                if (pageStructure != null && pageStructure.Contains("\"error\""))
                {
                    inspectionWarning = "⚠️ Page inspection failed. The automation script will be generated with generic selectors. " +
                        "For best results, ensure Playwright browsers are installed (run 'playwright install chromium').";
                    Console.WriteLine($"WARNING: {inspectionWarning}");
                    pageStructure = null; // Don't send error structure to AI
                }
            }
            catch (Exception ex)
            {
                // If page inspection fails, continue without it
                inspectionWarning = "⚠️ Page inspection failed. The automation script will be generated with generic selectors. " +
                    "For best results, ensure Playwright browsers are installed (run 'playwright install chromium').";
                Console.WriteLine($"Page inspection failed: {ex.Message}");
            }

            // Generate the automation script
            var automationScript = await _aiTestGenerator.GenerateAutomationScriptAsync(
                testCase,
                request.Framework,
                pageStructure);

            // Update the test case with the generated script
            testCase.AutomationScript = automationScript;
            testCase.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new GenerateAutomationScriptResponse
            {
                Success = true,
                AutomationScript = automationScript,
                Warnings = inspectionWarning != null ? new[] { inspectionWarning } : null
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new GenerateAutomationScriptResponse
            {
                Success = false,
                ErrorMessage = $"Error generating automation script: {ex.Message}"
            });
        }
    }
}
