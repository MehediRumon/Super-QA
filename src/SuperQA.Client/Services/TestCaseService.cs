using System.Net.Http.Json;
using SuperQA.Shared.DTOs;

namespace SuperQA.Client.Services;

public interface ITestCaseService
{
    Task<IEnumerable<TestCaseDto>> GetTestCasesAsync(int projectId);
    Task<IEnumerable<TestCaseDto>> GenerateTestCasesAsync(int requirementId);
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
}
