using SuperQA.Shared.DTOs;

namespace SuperQA.Client.Services;

public interface ICodeEditorService
{
    Task<SaveCodeEditorScriptResponse> SaveScriptAsync(SaveCodeEditorScriptRequest request);
    Task<List<CodeEditorScriptDto>> GetAllScriptsAsync();
    Task<CodeEditorScriptDto?> GetScriptAsync(int id);
    Task<bool> DeleteScriptAsync(int id);
}
