using SuperQA.Shared.DTOs;

namespace SuperQA.Core.Interfaces;

public interface IPlaywrightTestExecutor
{
    Task<PlaywrightTestExecutionResponse> ExecuteTestScriptAsync(string testScript, string applicationUrl, bool debugMode = false, int? slowMotion = null);
}
