namespace SuperQA.Core.Interfaces;

public interface IBackgroundTestRunner
{
    Task RunTestsInBackgroundAsync(int projectId);
    Task<string> GetTestRunStatusAsync(int projectId);
}
