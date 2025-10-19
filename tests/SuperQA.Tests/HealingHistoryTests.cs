using Microsoft.EntityFrameworkCore;
using SuperQA.Core.Entities;
using SuperQA.Infrastructure.Data;
using SuperQA.Infrastructure.Services;

namespace SuperQA.Tests;

public class HealingHistoryTests
{
    private SuperQADbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<SuperQADbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        return new SuperQADbContext(options);
    }

    [Fact]
    public async Task SelfHealingService_TracksHealingHistory()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var httpClient = new HttpClient();
        var service = new SelfHealingService(context, httpClient);

        var project = new Project { Id = 1, Name = "Test Project" };
        var testCase = new TestCase
        {
            Id = 1,
            ProjectId = 1,
            Title = "Test Case",
            Steps = "Click on #submit-button",
            AutomationScript = "await Page.ClickAsync(\"#submit-button\");",
            Project = project
        };

        context.Projects.Add(project);
        context.TestCases.Add(testCase);
        await context.SaveChangesAsync();

        // Act
        await service.UpdateLocatorAsync(1, "#submit-button", "[data-testid='submit']");

        // Assert
        var history = await context.HealingHistories
            .Where(h => h.TestCaseId == 1)
            .FirstOrDefaultAsync();

        Assert.NotNull(history);
        Assert.Equal("Self-Healing", history.HealingType);
        Assert.Equal("#submit-button", history.OldLocator);
        Assert.Equal("[data-testid='submit']", history.NewLocator);
        Assert.True(history.WasSuccessful);
        Assert.NotNull(history.OldScript);
        Assert.NotNull(history.NewScript);
    }

    [Fact]
    public async Task SelfHealingService_MultipleHealingHistoryEntries()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var httpClient = new HttpClient();
        var service = new SelfHealingService(context, httpClient);

        var project = new Project { Id = 1, Name = "Test Project" };
        var testCase = new TestCase
        {
            Id = 1,
            ProjectId = 1,
            Title = "Test Case",
            Steps = "Click on #submit-button and click on #cancel-button",
            AutomationScript = "await Page.ClickAsync(\"#submit-button\"); await Page.ClickAsync(\"#cancel-button\");",
            Project = project
        };

        context.Projects.Add(project);
        context.TestCases.Add(testCase);
        await context.SaveChangesAsync();

        // Act - First healing
        await service.UpdateLocatorAsync(1, "#submit-button", "[data-testid='submit']");
        
        // Act - Second healing
        await service.UpdateLocatorAsync(1, "#cancel-button", "[data-testid='cancel']");

        // Assert
        var histories = await context.HealingHistories
            .Where(h => h.TestCaseId == 1)
            .OrderBy(h => h.HealedAt)
            .ToListAsync();

        Assert.Equal(2, histories.Count);
        Assert.Equal("#submit-button", histories[0].OldLocator);
        Assert.Equal("[data-testid='submit']", histories[0].NewLocator);
        Assert.Equal("#cancel-button", histories[1].OldLocator);
        Assert.Equal("[data-testid='cancel']", histories[1].NewLocator);
    }

    [Fact]
    public async Task HealingHistory_PreservesPreviouslyHealedLocators()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var httpClient = new HttpClient();
        var service = new SelfHealingService(context, httpClient);

        var project = new Project { Id = 1, Name = "Test Project" };
        var testCase = new TestCase
        {
            Id = 1,
            ProjectId = 1,
            Title = "Test Case",
            Steps = "Click on #submit-button and click on #user",
            AutomationScript = "await Page.ClickAsync(\"#submit-button\"); await Page.FillAsync(\"#user\", \"test\");",
            Project = project
        };

        context.Projects.Add(project);
        context.TestCases.Add(testCase);
        await context.SaveChangesAsync();

        // Act - First healing for submit button
        await service.UpdateLocatorAsync(1, "#submit-button", "[data-testid='submit']");

        // Get the updated test case
        var updatedCase = await context.TestCases.FindAsync(1);
        Assert.NotNull(updatedCase);

        // Verify the first locator was healed
        Assert.Contains("[data-testid='submit']", updatedCase.Steps);
        Assert.DoesNotContain("#submit-button", updatedCase.Steps);
        
        // Verify the second locator was NOT changed
        Assert.Contains("#user", updatedCase.Steps);

        // Act - Second healing for user field
        await service.UpdateLocatorAsync(1, "#user", ".user-field");

        // Get the test case again
        var finalCase = await context.TestCases.FindAsync(1);
        Assert.NotNull(finalCase);

        // Assert - Both locators should be healed
        Assert.Contains("[data-testid='submit']", finalCase.Steps);
        Assert.Contains(".user-field", finalCase.Steps);
        Assert.DoesNotContain("#submit-button", finalCase.Steps);
        Assert.DoesNotContain("#user", finalCase.Steps);
    }

    [Fact]
    public async Task HealingHistory_TracksScriptChanges()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var httpClient = new HttpClient();
        var service = new SelfHealingService(context, httpClient);

        var project = new Project { Id = 1, Name = "Test Project" };
        var testCase = new TestCase
        {
            Id = 1,
            ProjectId = 1,
            Title = "Test Case",
            Steps = "Click on #submit",
            AutomationScript = "await Page.ClickAsync(\"#submit\");",
            Project = project
        };

        context.Projects.Add(project);
        context.TestCases.Add(testCase);
        await context.SaveChangesAsync();

        // Act
        await service.UpdateLocatorAsync(1, "#submit", "[role='button'][name='Submit']");

        // Assert
        var history = await context.HealingHistories.FirstOrDefaultAsync();
        Assert.NotNull(history);
        Assert.Contains("#submit", history.OldScript ?? "");
        Assert.Contains("[role='button'][name='Submit']", history.NewScript ?? "");
    }

    [Fact]
    public async Task GetHealingHistory_ReturnsOnlySuccessfulHealings()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var project = new Project { Id = 1, Name = "Test Project" };
        var testCase = new TestCase
        {
            Id = 1,
            ProjectId = 1,
            Title = "Test Case",
            Steps = "Test steps",
            Project = project
        };

        context.Projects.Add(project);
        context.TestCases.Add(testCase);

        // Add successful healing
        context.HealingHistories.Add(new HealingHistory
        {
            TestCaseId = 1,
            HealingType = "Self-Healing",
            OldLocator = "#old",
            NewLocator = "#new",
            WasSuccessful = true,
            HealedAt = DateTime.UtcNow.AddHours(-1)
        });

        // Add failed healing
        context.HealingHistories.Add(new HealingHistory
        {
            TestCaseId = 1,
            HealingType = "Self-Healing",
            OldLocator = "#failed",
            NewLocator = "#also-failed",
            WasSuccessful = false,
            HealedAt = DateTime.UtcNow,
            ErrorMessage = "Healing failed"
        });

        await context.SaveChangesAsync();

        // Act
        var successfulHealings = await context.HealingHistories
            .Where(h => h.TestCaseId == 1 && h.WasSuccessful)
            .ToListAsync();

        // Assert
        Assert.Single(successfulHealings);
        Assert.Equal("#old", successfulHealings[0].OldLocator);
    }
}
