namespace SuperQA.Shared.DTOs;

public class CodeEditorScriptDto
{
    public int Id { get; set; }
    public string TestName { get; set; } = string.Empty;
    public string ApplicationUrl { get; set; } = string.Empty;
    public string GherkinSteps { get; set; } = string.Empty;
    public string GeneratedScript { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class SaveCodeEditorScriptRequest
{
    public string TestName { get; set; } = string.Empty;
    public string ApplicationUrl { get; set; } = string.Empty;
    public string GherkinSteps { get; set; } = string.Empty;
    public string GeneratedScript { get; set; } = string.Empty;
}

public class SaveCodeEditorScriptResponse
{
    public bool Success { get; set; }
    public int? ScriptId { get; set; }
    public string? ErrorMessage { get; set; }
}
