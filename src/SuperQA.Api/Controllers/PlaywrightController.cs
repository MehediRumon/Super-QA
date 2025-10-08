using Microsoft.AspNetCore.Mvc;
using SuperQA.Core.Interfaces;
using SuperQA.Shared.DTOs;

namespace SuperQA.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlaywrightController : ControllerBase
{
    private readonly IOpenAIService _openAIService;
    private readonly IPlaywrightTestExecutor _playwrightTestExecutor;
    private readonly IPageInspectorService _pageInspectorService;

    public PlaywrightController(IOpenAIService openAIService, IPlaywrightTestExecutor playwrightTestExecutor, IPageInspectorService pageInspectorService)
    {
        _openAIService = openAIService;
        _playwrightTestExecutor = playwrightTestExecutor;
        _pageInspectorService = pageInspectorService;
    }

    [HttpPost("generate")]
    public async Task<ActionResult<PlaywrightTestGenerationResponse>> GenerateTest([FromBody] PlaywrightTestGenerationRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.FrsText))
                return BadRequest(new PlaywrightTestGenerationResponse 
                { 
                    Success = false, 
                    ErrorMessage = "FRS text is required" 
                });

            if (string.IsNullOrWhiteSpace(request.ApplicationUrl))
                return BadRequest(new PlaywrightTestGenerationResponse 
                { 
                    Success = false, 
                    ErrorMessage = "Application URL is required" 
                });

            if (string.IsNullOrWhiteSpace(request.OpenAIApiKey))
                return BadRequest(new PlaywrightTestGenerationResponse 
                { 
                    Success = false, 
                    ErrorMessage = "OpenAI API key is required" 
                });

            // Inspect the actual page to get real selectors
            string? pageStructure = null;
            try
            {
                pageStructure = await _pageInspectorService.GetPageStructureAsync(request.ApplicationUrl);
            }
            catch (Exception ex)
            {
                // If page inspection fails, continue without it
                // The AI will generate generic selectors
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
                return BadRequest(new PlaywrightTestExecutionResponse 
                { 
                    Success = false, 
                    Status = "Error",
                    ErrorMessage = "Test script is required" 
                });

            var result = await _playwrightTestExecutor.ExecuteTestScriptAsync(
                request.TestScript,
                request.ApplicationUrl);

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
}
