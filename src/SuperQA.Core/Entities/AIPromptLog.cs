namespace SuperQA.Core.Entities;

public class AIPromptLog
{
    public int Id { get; set; }
    public string PromptType { get; set; } = string.Empty; // TestGeneration, SelfHealing, Analysis
    public string Prompt { get; set; } = string.Empty;
    public string Response { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int TokensUsed { get; set; }
}
