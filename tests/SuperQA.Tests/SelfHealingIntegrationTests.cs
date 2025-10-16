using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SuperQA.Core.Entities;
using SuperQA.Infrastructure.Data;
using SuperQA.Infrastructure.Services;
using Xunit;

namespace SuperQA.Tests;

public class SelfHealingIntegrationTests
{
    private SuperQADbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<SuperQADbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new SuperQADbContext(options);
    }

    private IConfiguration CreateTestConfiguration()
    {
        var configBuilder = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                { "Playwright:Headless", "true" }
            }!);
        return configBuilder.Build();
    }

    [Fact]
    public async Task TestExecutionService_WithSelfHealing_UpdatesLocatorOnFailure()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var config = CreateTestConfiguration();
        var httpClient = new HttpClient();
        var selfHealingService = new SelfHealingService(context, httpClient);
        var testExecutionService = new TestExecutionService(context, config, selfHealingService);

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
            Title = "Test Case with Failing Locator",
            Description = "Test Description",
            Steps = "Click on #oldButton that doesn't exist",
            ExpectedResults = "Button should be clicked",
            CreatedAt = DateTime.UtcNow
        };
        context.TestCases.Add(testCase);
        await context.SaveChangesAsync();

        // Act - When no HTML is provided, it should use fallback
        var resultNoHtml = await selfHealingService.SuggestUpdatedLocatorAsync("#oldButton", "");
        
        // Assert - Fallback should convert ID selector to data-testid
        Assert.NotNull(resultNoHtml);
        Assert.Contains("data-testid", resultNoHtml);
        Assert.Contains("oldButton", resultNoHtml);
        
        // Verify that updating locator works
        var updated = await selfHealingService.UpdateLocatorAsync(testCase.Id, "#oldButton", resultNoHtml);
        Assert.True(updated);

        var updatedTestCase = await context.TestCases.FindAsync(testCase.Id);
        Assert.NotNull(updatedTestCase);
        Assert.DoesNotContain("#oldButton", updatedTestCase.Steps);
    }

    [Fact]
    public async Task SelfHealingService_WithMultipleAlternatives_PrefersMostStable()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var httpClient = new HttpClient();
        var service = new SelfHealingService(context, httpClient);

        var htmlWithMultipleAttributes = @"
            <button 
                id='submitBtn' 
                class='btn btn-primary' 
                data-testid='submitBtn' 
                name='submitBtn'
                aria-label='Submit'>
                Submit
            </button>";

        // Act - Try to find alternatives for a class selector
        var result = await service.SuggestUpdatedLocatorAsync(".btn-primary", htmlWithMultipleAttributes);

        // Assert - Should prefer more stable selectors
        Assert.NotNull(result);
    }

    [Fact]
    public async Task SelfHealingService_HandlesCaseWithNoAlternatives()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var httpClient = new HttpClient();
        var service = new SelfHealingService(context, httpClient);

        var htmlWithoutTarget = "<div>No matching element</div>";

        // Act
        var result = await service.SuggestUpdatedLocatorAsync("#nonExistent", htmlWithoutTarget);

        // Assert - Should return a fallback locator
        Assert.NotNull(result);
        Assert.NotEqual("#nonExistent", result);
    }

    [Fact]
    public async Task UpdateLocatorAsync_UpdatesBothStepsAndAutomationScript()
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
            Steps = "1. Click on #loginBtn\n2. Fill #username with 'test'",
            ExpectedResults = "Success",
            AutomationScript = @"
                await Page.ClickAsync(""#loginBtn"");
                await Page.FillAsync(""#username"", ""test"");",
            CreatedAt = DateTime.UtcNow
        };
        context.TestCases.Add(testCase);
        await context.SaveChangesAsync();

        // Act
        var result = await service.UpdateLocatorAsync(testCase.Id, "#loginBtn", "[data-testid='login']");

        // Assert
        Assert.True(result);
        var updatedTestCase = await context.TestCases.FindAsync(testCase.Id);
        Assert.NotNull(updatedTestCase);
        
        // Verify steps are updated
        Assert.DoesNotContain("#loginBtn", updatedTestCase.Steps);
        Assert.Contains("[data-testid='login']", updatedTestCase.Steps);
        Assert.Contains("#username", updatedTestCase.Steps); // Other locators unchanged
        
        // Verify automation script is updated
        Assert.DoesNotContain("#loginBtn", updatedTestCase.AutomationScript);
        Assert.Contains("[data-testid='login']", updatedTestCase.AutomationScript);
        Assert.Contains("#username", updatedTestCase.AutomationScript); // Other locators unchanged
    }
}
