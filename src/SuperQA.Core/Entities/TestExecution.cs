namespace SuperQA.Core.Entities;

public class TestExecution
{
    public int Id { get; set; }
    public int TestCaseId { get; set; }
    public int ProjectId { get; set; }
    public string Status { get; set; } = string.Empty; // Passed, Failed, Skipped
    public string? ErrorMessage { get; set; }
    public string? StackTrace { get; set; }
    public string? Screenshot { get; set; }
    public DateTime ExecutedAt { get; set; }
    public int DurationMs { get; set; }
    
    public TestCase TestCase { get; set; } = null!;
    public Project Project { get; set; } = null!;
    public ICollection<DefectPrediction> DefectPredictions { get; set; } = new List<DefectPrediction>();
}
