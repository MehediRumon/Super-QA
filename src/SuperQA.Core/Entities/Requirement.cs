namespace SuperQA.Core.Entities;

public class Requirement
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // User Story, Feature, etc.
    public DateTime CreatedAt { get; set; }
    
    public Project Project { get; set; } = null!;
    public ICollection<TestCase> TestCases { get; set; } = new List<TestCase>();
}
