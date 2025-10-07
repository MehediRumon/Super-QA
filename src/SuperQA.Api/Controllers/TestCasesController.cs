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
}
