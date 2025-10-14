using Microsoft.AspNetCore.Mvc;
using SuperQA.Core.Interfaces;
using SuperQA.Shared.DTOs;

namespace SuperQA.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SettingsController : ControllerBase
{
    private readonly IUserSettingsService _settingsService;

    public SettingsController(IUserSettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    [HttpGet]
    public async Task<ActionResult<UserSettingsDto>> GetSettings()
    {
        try
        {
            var settings = await _settingsService.GetSettingsAsync();
            
            if (settings == null)
            {
                // Return default settings
                return Ok(new UserSettingsDto
                {
                    Id = 0,
                    OpenAIApiKey = string.Empty,
                    SelectedModel = "gpt-4o-mini",
                    PlaywrightHeadless = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            return Ok(new UserSettingsDto
            {
                Id = settings.Id,
                OpenAIApiKey = settings.OpenAIApiKey,
                SelectedModel = settings.SelectedModel,
                PlaywrightHeadless = settings.PlaywrightHeadless,
                CreatedAt = settings.CreatedAt,
                UpdatedAt = settings.UpdatedAt
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = $"Error retrieving settings: {ex.Message}" });
        }
    }

    [HttpPost]
    public async Task<ActionResult<UserSettingsDto>> SaveSettings([FromBody] SaveUserSettingsRequest request)
    {
        try
        {
            var settings = await _settingsService.SaveSettingsAsync(
                request.OpenAIApiKey,
                request.SelectedModel,
                request.PlaywrightHeadless);

            return Ok(new UserSettingsDto
            {
                Id = settings.Id,
                OpenAIApiKey = settings.OpenAIApiKey,
                SelectedModel = settings.SelectedModel,
                PlaywrightHeadless = settings.PlaywrightHeadless,
                CreatedAt = settings.CreatedAt,
                UpdatedAt = settings.UpdatedAt
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = $"Error saving settings: {ex.Message}" });
        }
    }
}
