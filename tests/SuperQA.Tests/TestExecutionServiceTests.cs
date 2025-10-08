using Microsoft.EntityFrameworkCore;
using SuperQA.Core.Entities;
using SuperQA.Infrastructure.Data;
using SuperQA.Infrastructure.Services;

namespace SuperQA.Tests;

public class TestExecutionServiceTests
{
    private SuperQADbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<SuperQADbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        return new SuperQADbContext(options);
    }

    [Fact]
    public async Task GetTestExecutionsAsync_ReturnsExecutionsForProject()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var project = new Project { Id = 1, Name = "Test Project" };
        var testCase = new TestCase 
        { 
            Id = 1, 
            ProjectId = 1, 
            Title = "Test Case 1",
            Steps = "Step 1",
            Project = project
        };
        var execution = new TestExecution
        {
            Id = 1,
            TestCaseId = 1,
            ProjectId = 1,
            Status = "Passed",
            ExecutedAt = DateTime.UtcNow,
            DurationMs = 1000,
            TestCase = testCase
        };

        context.Projects.Add(project);
        context.TestCases.Add(testCase);
        context.TestExecutions.Add(execution);
        await context.SaveChangesAsync();

        var service = new TestExecutionService(context);

        // Act
        var result = await service.GetTestExecutionsAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
    }

    [Fact]
    public async Task GetTestExecutionAsync_ReturnsExecution()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var project = new Project { Id = 1, Name = "Test Project" };
        var testCase = new TestCase 
        { 
            Id = 1, 
            ProjectId = 1, 
            Title = "Test Case 1",
            Steps = "Step 1",
            Project = project
        };
        var execution = new TestExecution
        {
            Id = 1,
            TestCaseId = 1,
            ProjectId = 1,
            Status = "Passed",
            ExecutedAt = DateTime.UtcNow,
            DurationMs = 1000,
            TestCase = testCase
        };

        context.Projects.Add(project);
        context.TestCases.Add(testCase);
        context.TestExecutions.Add(execution);
        await context.SaveChangesAsync();

        var service = new TestExecutionService(context);

        // Act
        var result = await service.GetTestExecutionAsync(1);

        // Assert
        Assert.NotNull(result);
    }
}
