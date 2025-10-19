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
    public async Task SuggestUpdatedLocatorAsync_WithClassSelector_ReturnsPartialMatchLocator()
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
        // Should return a more stable partial match locator for class selectors
        Assert.Contains("btn-primary", result);
        Assert.Contains("class*=", result);
    }

    [Fact]
    public async Task UpdateLocatorAsync_WithSimilarLocators_OnlyReplacesExactMatch()
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
            Steps = "Click on #btn and then click on #btn-submit",
            ExpectedResults = "Success",
            AutomationScript = "await Page.ClickAsync(\"#btn\");\nawait Page.ClickAsync(\"#btn-submit\");",
            CreatedAt = DateTime.UtcNow
        };
        context.TestCases.Add(testCase);
        await context.SaveChangesAsync();

        // Act
        var result = await service.UpdateLocatorAsync(testCase.Id, "#btn", "[data-testid='btn']");

        // Assert
        Assert.True(result);
        var updatedTestCase = await context.TestCases.FindAsync(testCase.Id);
        Assert.NotNull(updatedTestCase);
        
        // Should replace #btn but NOT affect #btn-submit
        Assert.Contains("[data-testid='btn']", updatedTestCase.Steps);
        Assert.Contains("#btn-submit", updatedTestCase.Steps);
        Assert.DoesNotContain("[data-testid='btn']-submit", updatedTestCase.Steps);
        
        Assert.Contains("[data-testid='btn']", updatedTestCase.AutomationScript);
        Assert.Contains("#btn-submit", updatedTestCase.AutomationScript);
        Assert.DoesNotContain("[data-testid='btn']-submit", updatedTestCase.AutomationScript);
    }

    [Fact]
    public async Task UpdateLocatorAsync_WithSubstringMatch_OnlyReplacesCompleteLocator()
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
            Steps = "Fill #user field and #username field",
            ExpectedResults = "Success",
            AutomationScript = "await Page.FillAsync(\"#user\", \"test\");\nawait Page.FillAsync(\"#username\", \"testuser\");",
            CreatedAt = DateTime.UtcNow
        };
        context.TestCases.Add(testCase);
        await context.SaveChangesAsync();

        // Act
        var result = await service.UpdateLocatorAsync(testCase.Id, "#user", ".user-field");

        // Assert
        Assert.True(result);
        var updatedTestCase = await context.TestCases.FindAsync(testCase.Id);
        Assert.NotNull(updatedTestCase);
        
        // Should replace #user but NOT affect #username
        Assert.Contains(".user-field", updatedTestCase.Steps);
        Assert.Contains("#username", updatedTestCase.Steps);
        Assert.DoesNotContain(".user-fieldname", updatedTestCase.Steps);
        
        Assert.Contains(".user-field", updatedTestCase.AutomationScript);
        Assert.Contains("#username", updatedTestCase.AutomationScript);
        Assert.DoesNotContain(".user-fieldname", updatedTestCase.AutomationScript);
    }

    [Fact]
    public async Task UpdateLocatorAsync_WithMultipleOccurrencesOfSameLocator_ReplacesAll()
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
            Steps = "Click #submit-button, verify #submit-button is disabled",
            ExpectedResults = "Success",
            AutomationScript = "await Page.ClickAsync(\"#submit-button\");\nawait Page.IsDisabledAsync(\"#submit-button\");",
            CreatedAt = DateTime.UtcNow
        };
        context.TestCases.Add(testCase);
        await context.SaveChangesAsync();

        // Act
        var result = await service.UpdateLocatorAsync(testCase.Id, "#submit-button", "[data-testid='submit']");

        // Assert
        Assert.True(result);
        var updatedTestCase = await context.TestCases.FindAsync(testCase.Id);
        Assert.NotNull(updatedTestCase);
        
        // Should replace ALL exact occurrences of #submit-button
        Assert.DoesNotContain("#submit-button", updatedTestCase.Steps);
        Assert.Equal(2, updatedTestCase.Steps.Split(new[] { "[data-testid='submit']" }, StringSplitOptions.None).Length - 1);
        
        Assert.DoesNotContain("#submit-button", updatedTestCase.AutomationScript);
        Assert.Equal(2, updatedTestCase.AutomationScript.Split(new[] { "[data-testid='submit']" }, StringSplitOptions.None).Length - 1);
    }
}
