using Microsoft.EntityFrameworkCore;
using SuperQA.Core.Interfaces;
using SuperQA.Infrastructure.Data;

namespace SuperQA.Infrastructure.Services;

public class BackgroundTestRunnerService : IBackgroundTestRunner
{
    private readonly SuperQADbContext _context;
    private readonly ITestExecutionService _testExecutionService;
    private readonly Dictionary<int, string> _projectTestStatus = new();

    public BackgroundTestRunnerService(
        SuperQADbContext context,
        ITestExecutionService testExecutionService)
    {
        _context = context;
        _testExecutionService = testExecutionService;
    }

    public async Task RunTestsInBackgroundAsync(int projectId)
    {
        _projectTestStatus[projectId] = "Running";

        // Run tests in background without blocking
        _ = Task.Run(async () =>
        {
            try
            {
                var testCases = await _context.TestCases
                    .Where(tc => tc.ProjectId == projectId)
                    .ToListAsync();

                foreach (var testCase in testCases)
                {
                    try
                    {
                        await _testExecutionService.ExecuteTestAsync(testCase.Id);
                    }
                    catch
                    {
                        // Continue with next test even if one fails
                    }
                }

                _projectTestStatus[projectId] = "Completed";
            }
            catch (Exception)
            {
                _projectTestStatus[projectId] = "Failed";
            }
        });

        await Task.CompletedTask;
    }

    public Task<string> GetTestRunStatusAsync(int projectId)
    {
        if (_projectTestStatus.TryGetValue(projectId, out var status))
        {
            return Task.FromResult(status);
        }

        return Task.FromResult("Not Started");
    }
}
