using System.Net.Http.Json;
using SuperQA.Shared.DTOs;

namespace SuperQA.Client.Services;

public interface IRequirementService
{
    Task<IEnumerable<RequirementDto>> GetRequirementsAsync(int projectId);
    Task<RequirementDto> CreateRequirementAsync(CreateRequirementDto dto);
}

public class RequirementService : IRequirementService
{
    private readonly HttpClient _httpClient;

    public RequirementService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IEnumerable<RequirementDto>> GetRequirementsAsync(int projectId)
    {
        return await _httpClient.GetFromJsonAsync<IEnumerable<RequirementDto>>($"api/requirements/project/{projectId}") 
            ?? Array.Empty<RequirementDto>();
    }

    public async Task<RequirementDto> CreateRequirementAsync(CreateRequirementDto dto)
    {
        var response = await _httpClient.PostAsJsonAsync("api/requirements", dto);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<RequirementDto>() 
            ?? throw new InvalidOperationException("Failed to create requirement");
    }
}
