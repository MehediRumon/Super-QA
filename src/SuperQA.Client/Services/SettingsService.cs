using SuperQA.Shared.DTOs;
using System.Net.Http.Json;

namespace SuperQA.Client.Services;

public interface ISettingsService
{
    Task<UserSettingsDto?> GetSettingsAsync();
    Task<UserSettingsDto?> SaveSettingsAsync(SaveUserSettingsRequest request);
}

public class SettingsService : ISettingsService
{
    private readonly HttpClient _httpClient;

    public SettingsService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<UserSettingsDto?> GetSettingsAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<UserSettingsDto>("api/settings");
        }
        catch
        {
            return null;
        }
    }

    public async Task<UserSettingsDto?> SaveSettingsAsync(SaveUserSettingsRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/settings", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<UserSettingsDto>();
        }
        catch
        {
            return null;
        }
    }
}
