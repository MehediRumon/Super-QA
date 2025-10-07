namespace SuperQA.Core.Entities;

public class Project
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    public ICollection<TestCase> TestCases { get; set; } = new List<TestCase>();
    public ICollection<Requirement> Requirements { get; set; } = new List<Requirement>();
    public ICollection<TestExecution> TestExecutions { get; set; } = new List<TestExecution>();
}
