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

    public TestCasesController(SuperQADbContext context, IAITestGeneratorService aiTestGenerator)
    {
        _context = context;
        _aiTestGenerator = aiTestGenerator;
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

            // Try to extract URL from test case first, fall back to provided ApplicationUrl
            string? urlToInspect = _aiTestGenerator.ExtractUrlFromTestCase(testCase);
            if (string.IsNullOrWhiteSpace(urlToInspect))
            {
                urlToInspect = request.ApplicationUrl;
            }

            // Validate we have a URL to work with
            if (string.IsNullOrWhiteSpace(urlToInspect))
                return BadRequest(new GenerateAutomationScriptResponse 
                { 
                    Success = false, 
                    ErrorMessage = "Application URL is required (either provide it in the request or include a URL in the test case steps/preconditions)" 
                });

            // Generate the automation script without page inspection
            // (Test cases should include specific selectors/locators or use extension)
            var automationScript = await _aiTestGenerator.GenerateAutomationScriptAsync(
                testCase,
                request.Framework,
                null); // No page structure - AI will use generic selectors based on test case details

            // Update the test case with the generated script
            testCase.AutomationScript = automationScript;
            testCase.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new GenerateAutomationScriptResponse
            {
                Success = true,
                AutomationScript = automationScript
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

    [HttpGet("{id}")]
    public async Task<ActionResult<TestCaseDto>> GetTestCase(int id)
    {
        var testCase = await _context.TestCases.FindAsync(id);
        
        if (testCase == null)
            return NotFound();

        return Ok(new TestCaseDto
        {
            Id = testCase.Id,
            ProjectId = testCase.ProjectId,
            RequirementId = testCase.RequirementId,
            Title = testCase.Title,
            Description = testCase.Description,
            Preconditions = testCase.Preconditions,
            Steps = testCase.Steps,
            ExpectedResults = testCase.ExpectedResults,
            IsAIGenerated = testCase.IsAIGenerated,
            AutomationScript = testCase.AutomationScript,
            CreatedAt = testCase.CreatedAt,
            UpdatedAt = testCase.UpdatedAt
        });
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateTestCase(int id, [FromBody] TestCaseDto testCaseDto)
    {
        if (id != testCaseDto.Id)
            return BadRequest("ID mismatch");

        var testCase = await _context.TestCases.FindAsync(id);
        if (testCase == null)
            return NotFound();

        testCase.Title = testCaseDto.Title;
        testCase.Description = testCaseDto.Description;
        testCase.Preconditions = testCaseDto.Preconditions;
        testCase.Steps = testCaseDto.Steps;
        testCase.ExpectedResults = testCaseDto.ExpectedResults;
        testCase.AutomationScript = testCaseDto.AutomationScript;
        testCase.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteTestCase(int id)
    {
        var testCase = await _context.TestCases.FindAsync(id);
        if (testCase == null)
            return NotFound();

        _context.TestCases.Remove(testCase);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
