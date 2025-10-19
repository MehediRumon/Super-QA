using System.Net;
using Microsoft.EntityFrameworkCore;
using Moq;
using Moq.Protected;
using SuperQA.Core.Entities;
using SuperQA.Core.Interfaces;
using SuperQA.Infrastructure.Data;
using SuperQA.Infrastructure.Services;

namespace SuperQA.Tests;

/// <summary>
/// Tests for AI healing validation and protection mechanisms
/// </summary>
public class AIHealingValidationTests
{
    private SuperQADbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<SuperQADbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        return new SuperQADbContext(options);
    }

    private ILocatorValidationService CreateMockValidationService(bool isValid = true, bool hasMismatch = false)
    {
        var mock = new Mock<ILocatorValidationService>();
        mock.Setup(v => v.IsLocatorValid(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(isValid);
        mock.Setup(v => v.HasMismatchPatterns(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(hasMismatch);
        return mock.Object;
    }

    [Fact]
    public async Task HealTestScriptAsync_RejectsWhenPreviouslyHealedLocatorRemoved()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var project = new Project { Id = 1, Name = "Test Project" };
        var testCase = new TestCase
        {
            Id = 1,
            ProjectId = 1,
            Title = "Test Case",
            Description = "Test",
            Steps = "Click button",
            ExpectedResults = "Success",
            AutomationScript = "await Page.ClickAsync(\"[data-testid='submit']\");",
            Project = project
        };
        var execution = new TestExecution
        {
            Id = 1,
            TestCaseId = 1,
            ProjectId = 1,
            Status = "Failed",
            ErrorMessage = "Navigation failed",
            TestCase = testCase,
            Project = project
        };

        // Add healing history showing a previously corrected locator
        var healingHistory = new HealingHistory
        {
            Id = 1,
            TestCaseId = 1,
            TestExecutionId = 1,
            HealingType = "Self-Healing",
            OldLocator = "#submit-btn",
            NewLocator = "[data-testid='submit']",
            WasSuccessful = true,
            HealedAt = DateTime.UtcNow.AddDays(-1)
        };

        context.Projects.Add(project);
        context.TestCases.Add(testCase);
        context.TestExecutions.Add(execution);
        context.HealingHistories.Add(healingHistory);
        await context.SaveChangesAsync();

        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        // AI response removes the previously healed locator
        var jsonContent = "{\"choices\": [{\"message\": {\"content\": \"await Page.GotoAsync(\\\"https://example.com\\\");\\nawait Page.ClickAsync(\\\"#submit-btn\\\");\"}}]}";
        var mockResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(jsonContent)
        };

        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(mockResponse);

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var validationService = CreateMockValidationService();
        var service = new AITestHealingService(context, httpClient, validationService);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.HealTestScriptAsync(1, 1, "test-api-key", "gpt-4"));

        Assert.Contains("Previously corrected locator", exception.Message);
        Assert.Contains("[data-testid='submit']", exception.Message);

        // Verify failed healing was logged
        var failedHealing = await context.HealingHistories
            .Where(h => h.TestCaseId == 1 && h.WasSuccessful == false)
            .FirstOrDefaultAsync();
        Assert.NotNull(failedHealing);
    }

    [Fact]
    public async Task HealTestScriptAsync_RejectsGenericLocators()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var project = new Project { Id = 1, Name = "Test Project" };
        var testCase = new TestCase
        {
            Id = 1,
            ProjectId = 1,
            Title = "Test Case",
            Description = "Test",
            Steps = "Click button",
            ExpectedResults = "Success",
            AutomationScript = "await Page.ClickAsync(\"#submit\");",
            Project = project
        };
        var execution = new TestExecution
        {
            Id = 1,
            TestCaseId = 1,
            ProjectId = 1,
            Status = "Failed",
            ErrorMessage = "Element not found",
            TestCase = testCase,
            Project = project
        };

        context.Projects.Add(project);
        context.TestCases.Add(testCase);
        context.TestExecutions.Add(execution);
        await context.SaveChangesAsync();

        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        // AI response uses generic locator "button"  
        var jsonContent = "{\"choices\": [{\"message\": {\"content\": \"await Page.ClickAsync(\\\"button\\\");\"}}]}";
        var mockResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(jsonContent)
        };

        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(mockResponse);

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var validationService = CreateMockValidationService();
        var service = new AITestHealingService(context, httpClient, validationService);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.HealTestScriptAsync(1, 1, "test-api-key", "gpt-4"));

        Assert.Contains("overly generic locator", exception.Message);
        Assert.Contains("\"button\"", exception.Message);
    }

    [Fact]
    public async Task HealTestScriptAsync_RejectsMismatchedElementTypes()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var project = new Project { Id = 1, Name = "Test Project" };
        var testCase = new TestCase
        {
            Id = 1,
            ProjectId = 1,
            Title = "Test Case",
            Description = "Test",
            Steps = "Click login button",
            ExpectedResults = "Success",
            AutomationScript = "await Page.ClickAsync(\"#login-btn\");",
            Project = project
        };
        var execution = new TestExecution
        {
            Id = 1,
            TestCaseId = 1,
            ProjectId = 1,
            Status = "Failed",
            ErrorMessage = "Button element not found: #login-btn",
            TestCase = testCase,
            Project = project
        };

        context.Projects.Add(project);
        context.TestCases.Add(testCase);
        context.TestExecutions.Add(execution);
        await context.SaveChangesAsync();

        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        // AI response uses an input field locator instead of button
        var jsonContent = "{\"choices\": [{\"message\": {\"content\": \"await Page.FillAsync(\\\"#username-input\\\", \\\"test\\\");\"}}]}";
        var mockResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(jsonContent)
        };

        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(mockResponse);

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        
        // Configure validation service to detect mismatch
        var validationService = CreateMockValidationService(isValid: true, hasMismatch: true);
        var service = new AITestHealingService(context, httpClient, validationService);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.HealTestScriptAsync(1, 1, "test-api-key", "gpt-4"));

        Assert.Contains("incompatible element type", exception.Message);
    }

    [Fact]
    public async Task HealTestScriptAsync_AcceptsValidHealedScript()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var project = new Project { Id = 1, Name = "Test Project" };
        var testCase = new TestCase
        {
            Id = 1,
            ProjectId = 1,
            Title = "Login Test",
            Description = "Test user login",
            Steps = "Navigate to login page\nEnter credentials\nClick login",
            ExpectedResults = "User is logged in",
            AutomationScript = "await Page.ClickAsync(\"#old-login-btn\");",
            Project = project
        };
        var execution = new TestExecution
        {
            Id = 1,
            TestCaseId = 1,
            ProjectId = 1,
            Status = "Failed",
            ErrorMessage = "Element not found: #old-login-btn",
            TestCase = testCase,
            Project = project
        };

        context.Projects.Add(project);
        context.TestCases.Add(testCase);
        context.TestExecutions.Add(execution);
        await context.SaveChangesAsync();

        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        // Valid healed script with specific locators
        var jsonContent = "{\"choices\": [{\"message\": {\"content\": \"await Page.GotoAsync(\\\"https://example.com/login\\\");\\nawait Page.GetByRole(AriaRole.Button, new() { Name = \\\"Login\\\" }).ClickAsync();\"}}]}";
        var mockResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(jsonContent)
        };

        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(mockResponse);

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var validationService = CreateMockValidationService();
        var service = new AITestHealingService(context, httpClient, validationService);

        // Act
        var result = await service.HealTestScriptAsync(1, 1, "test-api-key", "gpt-4");

        // Assert
        Assert.Contains("GetByRole", result);
        Assert.DoesNotContain("#old-login-btn", result);

        // Verify successful healing was logged
        var successfulHealing = await context.HealingHistories
            .Where(h => h.TestCaseId == 1 && h.WasSuccessful)
            .FirstOrDefaultAsync();
        Assert.NotNull(successfulHealing);
        Assert.Equal("AI-Healing", successfulHealing.HealingType);
    }

    [Fact]
    public async Task HealTestScriptAsync_PreservesMultiplePreviouslyHealedLocators()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var project = new Project { Id = 1, Name = "Test Project" };
        var testCase = new TestCase
        {
            Id = 1,
            ProjectId = 1,
            Title = "Test Case",
            Description = "Test",
            Steps = "Click buttons",
            ExpectedResults = "Success",
            AutomationScript = "await Page.ClickAsync(\"[data-testid='submit']\");\nawait Page.ClickAsync(\"[data-testid='cancel']\");",
            Project = project
        };
        var execution = new TestExecution
        {
            Id = 1,
            TestCaseId = 1,
            ProjectId = 1,
            Status = "Failed",
            ErrorMessage = "Navigation failed",
            TestCase = testCase,
            Project = project
        };

        // Add multiple healing history entries
        var healingHistory1 = new HealingHistory
        {
            Id = 1,
            TestCaseId = 1,
            HealingType = "Self-Healing",
            OldLocator = "#submit-btn",
            NewLocator = "[data-testid='submit']",
            WasSuccessful = true,
            HealedAt = DateTime.UtcNow.AddDays(-2)
        };
        var healingHistory2 = new HealingHistory
        {
            Id = 2,
            TestCaseId = 1,
            HealingType = "Self-Healing",
            OldLocator = "#cancel-btn",
            NewLocator = "[data-testid='cancel']",
            WasSuccessful = true,
            HealedAt = DateTime.UtcNow.AddDays(-1)
        };

        context.Projects.Add(project);
        context.TestCases.Add(testCase);
        context.TestExecutions.Add(execution);
        context.HealingHistories.Add(healingHistory1);
        context.HealingHistories.Add(healingHistory2);
        await context.SaveChangesAsync();

        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        // AI response removes one of the previously healed locators
        var jsonContent = "{\"choices\": [{\"message\": {\"content\": \"await Page.GotoAsync(\\\"https://example.com\\\");\\nawait Page.ClickAsync(\\\"[data-testid='submit']\\\");\\nawait Page.ClickAsync(\\\"#cancel-btn\\\");\"}}]}";
        var mockResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(jsonContent)
        };

        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(mockResponse);

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var validationService = CreateMockValidationService();
        var service = new AITestHealingService(context, httpClient, validationService);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.HealTestScriptAsync(1, 1, "test-api-key", "gpt-4"));

        Assert.Contains("Previously corrected locator", exception.Message);
        Assert.Contains("[data-testid='cancel']", exception.Message);
    }

    [Fact]
    public async Task HealTestScriptAsync_AllowsRemovalIfLocatorCausedFailure()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var project = new Project { Id = 1, Name = "Test Project" };
        var testCase = new TestCase
        {
            Id = 1,
            ProjectId = 1,
            Title = "Test Case",
            Description = "Test",
            Steps = "Click button",
            ExpectedResults = "Success",
            AutomationScript = "await Page.ClickAsync(\"[data-testid='submit']\");",
            Project = project
        };
        var execution = new TestExecution
        {
            Id = 1,
            TestCaseId = 1,
            ProjectId = 1,
            Status = "Failed",
            ErrorMessage = "Element not found: [data-testid='submit']", // This locator caused the failure
            TestCase = testCase,
            Project = project
        };

        // Add healing history showing the locator that now fails
        var healingHistory = new HealingHistory
        {
            Id = 1,
            TestCaseId = 1,
            HealingType = "Self-Healing",
            OldLocator = "#submit-btn",
            NewLocator = "[data-testid='submit']",
            WasSuccessful = true,
            HealedAt = DateTime.UtcNow.AddDays(-1)
        };

        context.Projects.Add(project);
        context.TestCases.Add(testCase);
        context.TestExecutions.Add(execution);
        context.HealingHistories.Add(healingHistory);
        await context.SaveChangesAsync();

        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        // AI response replaces the failing locator with a new one
        var jsonContent = "{\"choices\": [{\"message\": {\"content\": \"await Page.GetByRole(AriaRole.Button, new() { Name = \\\"Submit\\\" }).ClickAsync();\"}}]}";
        var mockResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(jsonContent)
        };

        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(mockResponse);

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var validationService = CreateMockValidationService();
        var service = new AITestHealingService(context, httpClient, validationService);

        // Act - Should succeed because the removed locator was mentioned in the error
        var result = await service.HealTestScriptAsync(1, 1, "test-api-key", "gpt-4");

        // Assert
        Assert.Contains("GetByRole", result);
        Assert.DoesNotContain("[data-testid='submit']", result);
    }
}
