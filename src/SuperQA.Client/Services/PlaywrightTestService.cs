using System.Net.Http.Json;
using SuperQA.Shared.DTOs;

namespace SuperQA.Client.Services;

public class PlaywrightTestService : IPlaywrightTestService
{
    private readonly HttpClient _httpClient;

    public PlaywrightTestService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<PlaywrightTestGenerationResponse> GenerateTestScriptAsync(PlaywrightTestGenerationRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/playwright/generate", request);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<PlaywrightTestGenerationResponse>()
                    ?? new PlaywrightTestGenerationResponse { Success = false, ErrorMessage = "Failed to parse response" };
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return new PlaywrightTestGenerationResponse 
                { 
                    Success = false, 
                    ErrorMessage = $"API error: {response.StatusCode} - {errorContent}" 
                };
            }
        }
        catch (Exception ex)
        {
            return new PlaywrightTestGenerationResponse 
            { 
                Success = false, 
                ErrorMessage = $"Error: {ex.Message}" 
            };
        }
    }

    public async Task<PlaywrightTestExecutionResponse> ExecuteTestScriptAsync(PlaywrightTestExecutionRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/playwright/execute", request);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<PlaywrightTestExecutionResponse>()
                    ?? new PlaywrightTestExecutionResponse { Success = false, Status = "Error", ErrorMessage = "Failed to parse response" };
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return new PlaywrightTestExecutionResponse 
                { 
                    Success = false, 
                    Status = "Error",
                    ErrorMessage = $"API error: {response.StatusCode} - {errorContent}" 
                };
            }
        }
        catch (Exception ex)
        {
            return new PlaywrightTestExecutionResponse 
            { 
                Success = false,
                Status = "Error", 
                ErrorMessage = $"Error: {ex.Message}" 
            };
        }
    }

    public async Task<PlaywrightTestGenerationResponse> GenerateFromExtensionAsync(GenerateFromExtensionRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/playwright/generate-from-extension", request);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<PlaywrightTestGenerationResponse>()
                    ?? new PlaywrightTestGenerationResponse { Success = false, ErrorMessage = "Failed to parse response" };
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return new PlaywrightTestGenerationResponse 
                { 
                    Success = false, 
                    ErrorMessage = $"API error: {response.StatusCode} - {errorContent}" 
                };
            }
        }
        catch (Exception ex)
        {
            return new PlaywrightTestGenerationResponse 
            { 
                Success = false, 
                ErrorMessage = $"Error: {ex.Message}" 
            };
        }
    }
}
