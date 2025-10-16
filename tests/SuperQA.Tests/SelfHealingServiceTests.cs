using Microsoft.EntityFrameworkCore;
using SuperQA.Core.Entities;
using SuperQA.Infrastructure.Data;
using SuperQA.Infrastructure.Services;
using Xunit;

namespace SuperQA.Tests;

public class SelfHealingServiceTests
{
    private SuperQADbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<SuperQADbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new SuperQADbContext(options);
    }

    [Fact]
    public async Task SuggestUpdatedLocatorAsync_WithEmptyHtml_ReturnsFallbackLocator()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var httpClient = new HttpClient();
        var service = new SelfHealingService(context, httpClient);
        var failedLocator = "#loginButton";

        // Act
        var result = await service.SuggestUpdatedLocatorAsync(failedLocator, string.Empty);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(failedLocator, result);
        Assert.Contains("data-testid", result);
    }

    [Fact]
    public async Task SuggestUpdatedLocatorAsync_WithHtmlStructure_ReturnsAlternativeLocator()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var httpClient = new HttpClient();
        var service = new SelfHealingService(context, httpClient);
        var failedLocator = "#submitBtn";
        var htmlStructure = "<button id='submitBtn' data-testid='submit'>Submit</button>";

        // Act
        var result = await service.SuggestUpdatedLocatorAsync(failedLocator, htmlStructure);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task UpdateLocatorAsync_WithValidTestCase_UpdatesLocator()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var httpClient = new HttpClient();
        var service = new SelfHealingService(context, httpClient);

        var project = new Project
        {
            Name = "Test Project",
            Description = "Test Description",
            CreatedAt = DateTime.UtcNow
        };
        context.Projects.Add(project);
        await context.SaveChangesAsync();

        var testCase = new TestCase
        {
            ProjectId = project.Id,
            Title = "Test Case",
            Description = "Test Description",
            Steps = "Click on #oldButton",
            ExpectedResults = "Button should be clicked",
            CreatedAt = DateTime.UtcNow
        };
        context.TestCases.Add(testCase);
        await context.SaveChangesAsync();

        // Act
        var result = await service.UpdateLocatorAsync(testCase.Id, "#oldButton", "#newButton");

        // Assert
        Assert.True(result);
        var updatedTestCase = await context.TestCases.FindAsync(testCase.Id);
        Assert.NotNull(updatedTestCase);
        Assert.Contains("#newButton", updatedTestCase.Steps);
        Assert.DoesNotContain("#oldButton", updatedTestCase.Steps);
    }

    [Fact]
    public async Task UpdateLocatorAsync_WithAutomationScript_UpdatesScript()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var httpClient = new HttpClient();
        var service = new SelfHealingService(context, httpClient);

        var project = new Project
        {
            Name = "Test Project",
            Description = "Test Description",
            CreatedAt = DateTime.UtcNow
        };
        context.Projects.Add(project);
        await context.SaveChangesAsync();

        var testCase = new TestCase
        {
            ProjectId = project.Id,
            Title = "Test Case",
            Description = "Test Description",
            Steps = "Click on #oldSelector",
            ExpectedResults = "Success",
            AutomationScript = "await Page.ClickAsync(\"#oldSelector\");",
            CreatedAt = DateTime.UtcNow
        };
        context.TestCases.Add(testCase);
        await context.SaveChangesAsync();

        // Act
        var result = await service.UpdateLocatorAsync(testCase.Id, "#oldSelector", "#newSelector");

        // Assert
        Assert.True(result);
        var updatedTestCase = await context.TestCases.FindAsync(testCase.Id);
        Assert.NotNull(updatedTestCase);
        Assert.Contains("#newSelector", updatedTestCase.AutomationScript);
        Assert.DoesNotContain("#oldSelector", updatedTestCase.AutomationScript);
    }

    [Fact]
    public async Task UpdateLocatorAsync_WithInvalidTestCase_ReturnsFalse()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var httpClient = new HttpClient();
        var service = new SelfHealingService(context, httpClient);

        // Act
        var result = await service.UpdateLocatorAsync(999, "#old", "#new");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task SuggestUpdatedLocatorAsync_WithIdSelector_ReturnsDataTestIdAlternative()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var httpClient = new HttpClient();
        var service = new SelfHealingService(context, httpClient);
        var failedLocator = "#loginButton";

        // Act
        var result = await service.SuggestUpdatedLocatorAsync(failedLocator, "");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("loginButton", result);
        Assert.Contains("data-testid", result);
    }

    [Fact]
    public async Task SuggestUpdatedLocatorAsync_WithClassSelector_ReturnsSameLocator()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var httpClient = new HttpClient();
        var service = new SelfHealingService(context, httpClient);
        var failedLocator = ".btn-primary";

        // Act
        var result = await service.SuggestUpdatedLocatorAsync(failedLocator, "");

        // Assert
        Assert.NotNull(result);
        // Should return the same locator as fallback for class selectors
        Assert.Equal(failedLocator, result);
    }
}
