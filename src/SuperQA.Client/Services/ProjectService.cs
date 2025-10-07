using System.Net.Http.Json;
using SuperQA.Shared.DTOs;

namespace SuperQA.Client.Services;

public interface IProjectService
{
    Task<IEnumerable<ProjectDto>> GetProjectsAsync();
    Task<ProjectDto?> GetProjectAsync(int id);
    Task<ProjectDto> CreateProjectAsync(CreateProjectDto dto);
    Task DeleteProjectAsync(int id);
}

public class ProjectService : IProjectService
{
    private readonly HttpClient _httpClient;

    public ProjectService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IEnumerable<ProjectDto>> GetProjectsAsync()
    {
        return await _httpClient.GetFromJsonAsync<IEnumerable<ProjectDto>>("api/projects") 
            ?? Array.Empty<ProjectDto>();
    }

    public async Task<ProjectDto?> GetProjectAsync(int id)
    {
        return await _httpClient.GetFromJsonAsync<ProjectDto>($"api/projects/{id}");
    }

    public async Task<ProjectDto> CreateProjectAsync(CreateProjectDto dto)
    {
        var response = await _httpClient.PostAsJsonAsync("api/projects", dto);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProjectDto>() 
            ?? throw new InvalidOperationException("Failed to create project");
    }

    public async Task DeleteProjectAsync(int id)
    {
        var response = await _httpClient.DeleteAsync($"api/projects/{id}");
        response.EnsureSuccessStatusCode();
    }
}
