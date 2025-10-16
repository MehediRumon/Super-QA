namespace SuperQA.Core.Entities;

public class ExtensionTestData
{
    public int Id { get; set; }
    public string TestName { get; set; } = string.Empty;
    public string ApplicationUrl { get; set; } = string.Empty;
    public string StepsJson { get; set; } = string.Empty; // JSON serialized steps
    public int? TestCaseId { get; set; } // Reference to generated test case
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    public TestCase? TestCase { get; set; }
}
