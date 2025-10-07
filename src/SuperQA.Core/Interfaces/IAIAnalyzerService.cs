namespace SuperQA.Core.Interfaces;

public interface IAIAnalyzerService
{
    Task<string> AnalyzeTestFailureAsync(string logs, string? screenshot);
    Task<string> SuggestFixAsync(string errorMessage, string stackTrace);
}
