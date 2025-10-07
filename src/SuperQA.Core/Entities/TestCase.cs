namespace SuperQA.Core.Entities;

public class TestCase
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
    
    public Project Project { get; set; } = null!;
    public Requirement? Requirement { get; set; }
    public ICollection<TestExecution> TestExecutions { get; set; } = new List<TestExecution>();
}
