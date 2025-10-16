using System.Net.Http.Json;
using SuperQA.Shared.DTOs;

namespace SuperQA.Client.Services;

public interface ITestCaseService
{
    Task<IEnumerable<TestCaseDto>> GetTestCasesAsync(int projectId);
    Task<IEnumerable<TestCaseDto>> GenerateTestCasesAsync(int requirementId);
    Task<GenerateAutomationScriptResponse> GenerateAutomationScriptAsync(GenerateAutomationScriptRequest request);
    Task<TestCaseDto?> GetTestCaseAsync(int id);
    Task<bool> UpdateTestCaseAsync(int id, TestCaseDto testCase);
    Task<bool> DeleteTestCaseAsync(int id);
    Task<IEnumerable<TestCaseDto>> GetTestCasesWithAutomationScriptsAsync();
}

public class TestCaseService : ITestCaseService
{
    private readonly HttpClient _httpClient;

    public TestCaseService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IEnumerable<TestCaseDto>> GetTestCasesAsync(int projectId)
    {
        return await _httpClient.GetFromJsonAsync<IEnumerable<TestCaseDto>>($"api/testcases/project/{projectId}") 
            ?? Array.Empty<TestCaseDto>();
    }

    public async Task<IEnumerable<TestCaseDto>> GenerateTestCasesAsync(int requirementId)
    {
        var request = new GenerateTestCasesRequest { RequirementId = requirementId };
        var response = await _httpClient.PostAsJsonAsync("api/testcases/generate", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<IEnumerable<TestCaseDto>>() 
            ?? Array.Empty<TestCaseDto>();
    }

    public async Task<GenerateAutomationScriptResponse> GenerateAutomationScriptAsync(GenerateAutomationScriptRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/testcases/generate-automation-script", request);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<GenerateAutomationScriptResponse>()
                    ?? new GenerateAutomationScriptResponse { Success = false, ErrorMessage = "Failed to parse response" };
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return new GenerateAutomationScriptResponse 
                { 
                    Success = false, 
                    ErrorMessage = $"API error: {response.StatusCode} - {errorContent}" 
                };
            }
        }
        catch (Exception ex)
        {
            return new GenerateAutomationScriptResponse 
            { 
                Success = false, 
                ErrorMessage = $"Error: {ex.Message}" 
            };
        }
    }

    public async Task<TestCaseDto?> GetTestCaseAsync(int id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<TestCaseDto>($"api/testcases/{id}");
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> UpdateTestCaseAsync(int id, TestCaseDto testCase)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/testcases/{id}", testCase);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DeleteTestCaseAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/testcases/{id}");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<IEnumerable<TestCaseDto>> GetTestCasesWithAutomationScriptsAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<IEnumerable<TestCaseDto>>("api/testcases/with-automation-scripts")
                ?? Array.Empty<TestCaseDto>();
        }
        catch
        {
            return Array.Empty<TestCaseDto>();
        }
    }
}
