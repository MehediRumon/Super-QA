namespace SuperQA.Core.Entities;

public class DefectPrediction
{
    public int Id { get; set; }
    public int TestExecutionId { get; set; }
    public string Module { get; set; } = string.Empty;
    public double RiskScore { get; set; }
    public string PredictedIssue { get; set; } = string.Empty;
    public DateTime PredictedAt { get; set; }
    
    public TestExecution TestExecution { get; set; } = null!;
}
