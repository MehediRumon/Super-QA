using SuperQA.Core.Entities;

namespace SuperQA.Core.Interfaces;

public interface IUserSettingsService
{
    Task<UserSettings?> GetSettingsAsync();
    Task<UserSettings> SaveSettingsAsync(string apiKey, string model, bool headless);
}
