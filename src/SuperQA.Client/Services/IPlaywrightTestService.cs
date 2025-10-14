using SuperQA.Shared.DTOs;

namespace SuperQA.Client.Services;

public interface IPlaywrightTestService
{
    Task<PlaywrightTestGenerationResponse> GenerateTestScriptAsync(PlaywrightTestGenerationRequest request);
    Task<PlaywrightTestExecutionResponse> ExecuteTestScriptAsync(PlaywrightTestExecutionRequest request);
    Task<PlaywrightTestGenerationResponse> GenerateFromExtensionAsync(GenerateFromExtensionRequest request);
}
