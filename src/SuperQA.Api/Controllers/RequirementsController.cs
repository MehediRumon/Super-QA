using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuperQA.Core.Entities;
using SuperQA.Infrastructure.Data;
using SuperQA.Shared.DTOs;

namespace SuperQA.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RequirementsController : ControllerBase
{
    private readonly SuperQADbContext _context;

    public RequirementsController(SuperQADbContext context)
    {
        _context = context;
    }

    [HttpGet("project/{projectId}")]
    public async Task<ActionResult<IEnumerable<RequirementDto>>> GetRequirements(int projectId)
    {
        var requirements = await _context.Requirements
            .Where(r => r.ProjectId == projectId)
            .Select(r => new RequirementDto
            {
                Id = r.Id,
                ProjectId = r.ProjectId,
                Title = r.Title,
                Description = r.Description,
                Type = r.Type,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync();

        return Ok(requirements);
    }

    [HttpPost]
    public async Task<ActionResult<RequirementDto>> CreateRequirement(CreateRequirementDto dto)
    {
        var requirement = new Requirement
        {
            ProjectId = dto.ProjectId,
            Title = dto.Title,
            Description = dto.Description,
            Type = dto.Type,
            CreatedAt = DateTime.UtcNow
        };

        _context.Requirements.Add(requirement);
        await _context.SaveChangesAsync();

        var resultDto = new RequirementDto
        {
            Id = requirement.Id,
            ProjectId = requirement.ProjectId,
            Title = requirement.Title,
            Description = requirement.Description,
            Type = requirement.Type,
            CreatedAt = requirement.CreatedAt
        };

        return CreatedAtAction(nameof(GetRequirements), new { projectId = requirement.ProjectId }, resultDto);
    }
}
