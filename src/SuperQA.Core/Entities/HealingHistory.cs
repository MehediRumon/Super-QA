namespace SuperQA.Core.Entities;

/// <summary>
/// Tracks the history of healing attempts for test cases to prevent overwriting previously corrected locators
/// </summary>
public class HealingHistory
{
    public int Id { get; set; }
    public int TestCaseId { get; set; }
    public int? TestExecutionId { get; set; }
    public string HealingType { get; set; } = string.Empty; // "Self-Healing" or "AI-Healing"
    public string OldLocator { get; set; } = string.Empty;
    public string NewLocator { get; set; } = string.Empty;
    public string? OldScript { get; set; }
    public string? NewScript { get; set; }
    public bool WasSuccessful { get; set; }
    public bool WasApplied { get; set; } // True only when user actually applies the healed script to the test case
    public DateTime HealedAt { get; set; }
    public DateTime? AppliedAt { get; set; } // When the healed script was applied to the test case
    public string? ErrorMessage { get; set; }
    
    public TestCase TestCase { get; set; } = null!;
    public TestExecution? TestExecution { get; set; }
}
