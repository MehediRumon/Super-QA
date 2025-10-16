namespace SuperQA.Shared.DTOs;

public class TestExecutionDto
{
    public int Id { get; set; }
    public int TestCaseId { get; set; }
    public int ProjectId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public string? StackTrace { get; set; }
    public string? Screenshot { get; set; }
    public DateTime ExecutedAt { get; set; }
    public int DurationMs { get; set; }
    public string TestCaseTitle { get; set; } = string.Empty;
}

public class ExecuteTestRequest
{
    public int TestCaseId { get; set; }
    public string? BaseUrl { get; set; }
}

public class HealTestRequest
{
    public int TestCaseId { get; set; }
    public int ExecutionId { get; set; }
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-4";
}

public class HealTestResponse
{
    public string HealedScript { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
