using SuperQA.Core.Entities;

namespace SuperQA.Core.Interfaces;

public interface IMCPService
{
    Task<string> SendPromptAsync(string prompt, string context);
    Task<T> SendStructuredPromptAsync<T>(string prompt, string context);
    Task LogPromptAsync(AIPromptLog log);
}
