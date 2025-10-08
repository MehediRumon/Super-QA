namespace SuperQA.Core.Interfaces;

public interface ITestExecutionService
{
    Task<int> ExecuteTestAsync(int testCaseId, string? baseUrl = null);
    Task<IEnumerable<object>> GetTestExecutionsAsync(int projectId);
    Task<object?> GetTestExecutionAsync(int executionId);
}
