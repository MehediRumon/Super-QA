using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuperQA.Core.Entities;
using SuperQA.Infrastructure.Data;
using SuperQA.Shared.DTOs;

namespace SuperQA.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CodeEditorController : ControllerBase
{
    private readonly SuperQADbContext _context;

    public CodeEditorController(SuperQADbContext context)
    {
        _context = context;
    }

    [HttpPost("save")]
    public async Task<ActionResult<SaveCodeEditorScriptResponse>> SaveScript([FromBody] SaveCodeEditorScriptRequest request)
    {
        try
        {
            var script = new CodeEditorScript
            {
                TestName = request.TestName,
                ApplicationUrl = request.ApplicationUrl,
                GherkinSteps = request.GherkinSteps,
                GeneratedScript = request.GeneratedScript,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.CodeEditorScripts.Add(script);
            await _context.SaveChangesAsync();

            return Ok(new SaveCodeEditorScriptResponse
            {
                Success = true,
                ScriptId = script.Id
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new SaveCodeEditorScriptResponse
            {
                Success = false,
                ErrorMessage = $"Error saving script: {ex.Message}"
            });
        }
    }

    [HttpGet]
    public async Task<ActionResult<List<CodeEditorScriptDto>>> GetAllScripts()
    {
        try
        {
            var scripts = await _context.CodeEditorScripts
                .OrderByDescending(s => s.UpdatedAt)
                .Select(s => new CodeEditorScriptDto
                {
                    Id = s.Id,
                    TestName = s.TestName,
                    ApplicationUrl = s.ApplicationUrl,
                    GherkinSteps = s.GherkinSteps,
                    GeneratedScript = s.GeneratedScript,
                    CreatedAt = s.CreatedAt,
                    UpdatedAt = s.UpdatedAt
                })
                .ToListAsync();

            return Ok(scripts);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new List<CodeEditorScriptDto>());
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CodeEditorScriptDto>> GetScript(int id)
    {
        try
        {
            var script = await _context.CodeEditorScripts.FindAsync(id);
            if (script == null)
                return NotFound();

            return Ok(new CodeEditorScriptDto
            {
                Id = script.Id,
                TestName = script.TestName,
                ApplicationUrl = script.ApplicationUrl,
                GherkinSteps = script.GherkinSteps,
                GeneratedScript = script.GeneratedScript,
                CreatedAt = script.CreatedAt,
                UpdatedAt = script.UpdatedAt
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error retrieving script: {ex.Message}");
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteScript(int id)
    {
        try
        {
            var script = await _context.CodeEditorScripts.FindAsync(id);
            if (script == null)
                return NotFound();

            _context.CodeEditorScripts.Remove(script);
            await _context.SaveChangesAsync();

            return Ok();
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error deleting script: {ex.Message}");
        }
    }
}
