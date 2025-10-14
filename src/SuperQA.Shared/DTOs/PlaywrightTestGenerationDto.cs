namespace SuperQA.Shared.DTOs;

public class PlaywrightTestGenerationRequest
{
    public string FrsText { get; set; } = string.Empty;
    public string ApplicationUrl { get; set; } = string.Empty;
    public string OpenAIApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-4o-mini";
}

public class PlaywrightTestGenerationResponse
{
    public string GeneratedScript { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string[]? Warnings { get; set; }
    public int? TestCaseId { get; set; }
    public int? ProjectId { get; set; }
}

public class PlaywrightTestExecutionRequest
{
    public string TestScript { get; set; } = string.Empty;
    public string ApplicationUrl { get; set; } = string.Empty;
}

public class PlaywrightTestExecutionResponse
{
    public bool Success { get; set; }
    public string Status { get; set; } = string.Empty; // "Pass", "Fail"
    public string Output { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public List<string> Logs { get; set; } = new();
}
