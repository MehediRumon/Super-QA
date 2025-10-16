namespace SuperQA.Core.Interfaces;

/// <summary>
/// Service for AI-powered test script healing and self-fixing
/// </summary>
public interface IAITestHealingService
{
    /// <summary>
    /// Analyzes a failed test execution and generates a healed/fixed test script
    /// </summary>
    /// <param name="testCaseId">The ID of the test case that failed</param>
    /// <param name="executionId">The ID of the failed execution</param>
    /// <param name="apiKey">OpenAI API key for healing</param>
    /// <param name="model">AI model to use (e.g., gpt-4, gpt-3.5-turbo)</param>
    /// <returns>Healed test script with improvements and fixes</returns>
    Task<string> HealTestScriptAsync(int testCaseId, int executionId, string apiKey, string model = "gpt-4");
}
