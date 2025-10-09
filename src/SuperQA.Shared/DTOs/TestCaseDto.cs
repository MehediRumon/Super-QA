namespace SuperQA.Shared.DTOs;

public class TestCaseDto
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public int? RequirementId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Preconditions { get; set; } = string.Empty;
    public string Steps { get; set; } = string.Empty;
    public string ExpectedResults { get; set; } = string.Empty;
    public bool IsAIGenerated { get; set; }
    public string? AutomationScript { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class GenerateTestCasesRequest
{
    public int RequirementId { get; set; }
}

public class GenerateAutomationScriptRequest
{
    public int TestCaseId { get; set; }
    public string ApplicationUrl { get; set; } = string.Empty;
    public string Framework { get; set; } = "Playwright";
}

public class GenerateAutomationScriptResponse
{
    public bool Success { get; set; }
    public string AutomationScript { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public string[]? Warnings { get; set; }
}
