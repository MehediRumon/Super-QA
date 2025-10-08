namespace SuperQA.Core.Interfaces;

public interface IOpenAIService
{
    Task<string> GeneratePlaywrightTestScriptAsync(string frsText, string applicationUrl, string apiKey, string model);
}
