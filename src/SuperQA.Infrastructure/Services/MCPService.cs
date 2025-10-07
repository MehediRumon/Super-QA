using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using SuperQA.Core.Entities;
using SuperQA.Core.Interfaces;
using SuperQA.Infrastructure.Data;

namespace SuperQA.Infrastructure.Services;

public class MCPService : IMCPService
{
    private readonly SuperQADbContext _context;
    private readonly HttpClient _httpClient;
    private readonly string _apiEndpoint;

    public MCPService(SuperQADbContext context, HttpClient httpClient, IConfiguration configuration)
    {
        _context = context;
        _httpClient = httpClient;
        _apiEndpoint = configuration["MCP:Endpoint"] ?? "http://localhost:3000";
    }

    public async Task<string> SendPromptAsync(string prompt, string context)
    {
        var request = new
        {
            prompt,
            context,
            timestamp = DateTime.UtcNow
        };

        var response = await _httpClient.PostAsJsonAsync($"{_apiEndpoint}/api/prompt", request);
        response.EnsureSuccessStatusCode();
        
        var result = await response.Content.ReadAsStringAsync();
        return result;
    }

    public async Task<T> SendStructuredPromptAsync<T>(string prompt, string context)
    {
        var response = await SendPromptAsync(prompt, context);
        return JsonSerializer.Deserialize<T>(response) ?? throw new InvalidOperationException("Failed to deserialize response");
    }

    public async Task LogPromptAsync(AIPromptLog log)
    {
        _context.AIPromptLogs.Add(log);
        await _context.SaveChangesAsync();
    }
}
