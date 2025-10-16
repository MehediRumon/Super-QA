namespace SuperQA.Core.Interfaces;

public interface IOpenAIService
{
    Task<string> GeneratePlaywrightTestScriptAsync(string frsText, string applicationUrl, string apiKey, string model, string? pageStructure = null);
    Task<string> HealTestScriptAsync(string testScript, string? errorMessage, List<string>? executionLogs, string apiKey, string model);
}
