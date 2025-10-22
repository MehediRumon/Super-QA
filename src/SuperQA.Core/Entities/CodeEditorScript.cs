namespace SuperQA.Core.Entities;

public class CodeEditorScript
{
    public int Id { get; set; }
    public string TestName { get; set; } = string.Empty;
    public string ApplicationUrl { get; set; } = string.Empty;
    public string GherkinSteps { get; set; } = string.Empty;
    public string GeneratedScript { get; set; } = string.Empty;
    public bool IsSaved { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
