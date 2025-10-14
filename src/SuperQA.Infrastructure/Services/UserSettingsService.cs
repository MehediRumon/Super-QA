using Microsoft.EntityFrameworkCore;
using SuperQA.Core.Entities;
using SuperQA.Core.Interfaces;
using SuperQA.Infrastructure.Data;

namespace SuperQA.Infrastructure.Services;

public class UserSettingsService : IUserSettingsService
{
    private readonly SuperQADbContext _context;

    public UserSettingsService(SuperQADbContext context)
    {
        _context = context;
    }

    public async Task<UserSettings?> GetSettingsAsync()
    {
        // Get the first (and only) settings record
        return await _context.UserSettings.FirstOrDefaultAsync();
    }

    public async Task<UserSettings> SaveSettingsAsync(string apiKey, string model, bool headless)
    {
        var settings = await GetSettingsAsync();
        
        if (settings == null)
        {
            settings = new UserSettings
            {
                OpenAIApiKey = apiKey,
                SelectedModel = model,
                PlaywrightHeadless = headless,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.UserSettings.Add(settings);
        }
        else
        {
            settings.OpenAIApiKey = apiKey;
            settings.SelectedModel = model;
            settings.PlaywrightHeadless = headless;
            settings.UpdatedAt = DateTime.UtcNow;
            _context.UserSettings.Update(settings);
        }

        await _context.SaveChangesAsync();
        return settings;
    }
}
