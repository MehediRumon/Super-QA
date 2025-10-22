using System.Net.Http.Json;
using SuperQA.Shared.DTOs;

namespace SuperQA.Client.Services;

public class CodeEditorService : ICodeEditorService
{
    private readonly HttpClient _httpClient;

    public CodeEditorService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<SaveCodeEditorScriptResponse> SaveScriptAsync(SaveCodeEditorScriptRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/codeeditor/save", request);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<SaveCodeEditorScriptResponse>() 
                    ?? new SaveCodeEditorScriptResponse { Success = false, ErrorMessage = "Failed to parse response" };
            }
            
            return new SaveCodeEditorScriptResponse 
            { 
                Success = false, 
                ErrorMessage = $"API returned status: {response.StatusCode}" 
            };
        }
        catch (Exception ex)
        {
            return new SaveCodeEditorScriptResponse 
            { 
                Success = false, 
                ErrorMessage = ex.Message 
            };
        }
    }

    public async Task<List<CodeEditorScriptDto>> GetAllScriptsAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<CodeEditorScriptDto>>("api/codeeditor") 
                ?? new List<CodeEditorScriptDto>();
        }
        catch
        {
            return new List<CodeEditorScriptDto>();
        }
    }

    public async Task<CodeEditorScriptDto?> GetScriptAsync(int id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<CodeEditorScriptDto>($"api/codeeditor/{id}");
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> DeleteScriptAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/codeeditor/{id}");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
