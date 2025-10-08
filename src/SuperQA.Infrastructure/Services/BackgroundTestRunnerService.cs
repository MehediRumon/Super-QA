using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SuperQA.Core.Interfaces;
using SuperQA.Infrastructure.Data;

namespace SuperQA.Infrastructure.Services;

public class BackgroundTestRunnerService : IBackgroundTestRunner
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly Dictionary<int, string> _projectTestStatus = new();

    public BackgroundTestRunnerService(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task RunTestsInBackgroundAsync(int projectId)
    {
        _projectTestStatus[projectId] = "Running";

        // Run tests in background without blocking
        _ = Task.Run(async () =>
        {
            try
            {
                // Create a new scope for this background task
                using var scope = _serviceScopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<SuperQADbContext>();
                var testExecutionService = scope.ServiceProvider.GetRequiredService<ITestExecutionService>();

                var testCases = await context.TestCases
                    .Where(tc => tc.ProjectId == projectId)
                    .ToListAsync();

                foreach (var testCase in testCases)
                {
                    try
                    {
                        await testExecutionService.ExecuteTestAsync(testCase.Id);
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
