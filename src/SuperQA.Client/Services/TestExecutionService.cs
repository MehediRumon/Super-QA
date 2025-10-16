using System.Net.Http.Json;
using SuperQA.Shared.DTOs;

namespace SuperQA.Client.Services;

public interface ITestExecutionService
{
    Task<int> ExecuteTestAsync(ExecuteTestRequest request);
    Task<IEnumerable<TestExecutionDto>> GetProjectExecutionsAsync(int projectId);
    Task<TestExecutionDto?> GetExecutionAsync(int executionId);
    Task RunAllTestsAsync(int projectId);
    Task<string> GetTestRunStatusAsync(int projectId);
    Task<HealTestResponse> HealTestAsync(HealTestRequest request);
}

public class TestExecutionService : ITestExecutionService
{
    private readonly HttpClient _httpClient;

    public TestExecutionService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<int> ExecuteTestAsync(ExecuteTestRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/testexecutions/execute", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<int>();
    }

    public async Task<IEnumerable<TestExecutionDto>> GetProjectExecutionsAsync(int projectId)
    {
        var response = await _httpClient.GetFromJsonAsync<IEnumerable<TestExecutionDto>>(
            $"api/testexecutions/project/{projectId}");
        return response ?? Enumerable.Empty<TestExecutionDto>();
    }

    public async Task<TestExecutionDto?> GetExecutionAsync(int executionId)
    {
        return await _httpClient.GetFromJsonAsync<TestExecutionDto>(
            $"api/testexecutions/{executionId}");
    }

    public async Task RunAllTestsAsync(int projectId)
    {
        var response = await _httpClient.PostAsync(
            $"api/testexecutions/project/{projectId}/run-all", null);
        response.EnsureSuccessStatusCode();
    }

    public async Task<string> GetTestRunStatusAsync(int projectId)
    {
        var response = await _httpClient.GetFromJsonAsync<StatusResponse>(
            $"api/testexecutions/project/{projectId}/status");
        return response?.Status ?? "Unknown";
    }

    public async Task<HealTestResponse> HealTestAsync(HealTestRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/testexecutions/heal", request);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<HealTestResponse>();
        return result ?? new HealTestResponse { Message = "Unknown error occurred" };
    }

    private class StatusResponse
    {
        public string Status { get; set; } = string.Empty;
    }
}
