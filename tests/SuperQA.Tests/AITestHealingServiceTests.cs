using System.Net;
using Microsoft.EntityFrameworkCore;
using Moq;
using Moq.Protected;
using SuperQA.Core.Entities;
using SuperQA.Infrastructure.Data;
using SuperQA.Infrastructure.Services;

namespace SuperQA.Tests;

public class AITestHealingServiceTests
{
    private SuperQADbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<SuperQADbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        return new SuperQADbContext(options);
    }

    [Fact]
    public async Task HealTestScriptAsync_TestCaseNotFound_ThrowsArgumentException()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var httpClient = new HttpClient();
        var service = new AITestHealingService(context, httpClient);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => service.HealTestScriptAsync(999, 1, "test-api-key", "gpt-4"));

        Assert.Contains("Test case with ID 999 not found", exception.Message);
    }

    [Fact]
    public async Task HealTestScriptAsync_ExecutionNotFound_ThrowsArgumentException()
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
        await context.SaveChangesAsync();

        var httpClient = new HttpClient();
        var service = new AITestHealingService(context, httpClient);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => service.HealTestScriptAsync(1, 999, "test-api-key", "gpt-4"));

        Assert.Contains("Test execution with ID 999 not found", exception.Message);
    }

    [Fact]
    public async Task HealTestScriptAsync_ExecutionNotFailed_ThrowsInvalidOperationException()
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
        var execution = new TestExecution
        {
            Id = 1,
            TestCaseId = 1,
            ProjectId = 1,
            Status = "Passed", // Not failed
            TestCase = testCase,
            Project = project
        };

        context.Projects.Add(project);
        context.TestCases.Add(testCase);
        context.TestExecutions.Add(execution);
        await context.SaveChangesAsync();

        var httpClient = new HttpClient();
        var service = new AITestHealingService(context, httpClient);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.HealTestScriptAsync(1, 1, "test-api-key", "gpt-4"));

        Assert.Contains("Can only heal failed test executions", exception.Message);
    }

    [Fact]
    public async Task HealTestScriptAsync_SuccessfulHealing_ReturnsHealedScript()
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
            AutomationScript = "// Old script with issues",
            Project = project
        };
        var execution = new TestExecution
        {
            Id = 1,
            TestCaseId = 1,
            ProjectId = 1,
            Status = "Failed",
            ErrorMessage = "Element not found: #login-button",
            StackTrace = "at line 10",
            TestCase = testCase,
            Project = project
        };

        context.Projects.Add(project);
        context.TestCases.Add(testCase);
        context.TestExecutions.Add(execution);
        await context.SaveChangesAsync();

        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        var mockResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(@"{
                ""choices"": [{
                    ""message"": {
                        ""content"": ""// Healed script with robust selectors\nawait Page.GetByRole(AriaRole.Button, new() { Name = \""Login\"" }).ClickAsync();""
                    }
                }]
            }")
        };

        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(mockResponse);

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var service = new AITestHealingService(context, httpClient);

        // Act
        var result = await service.HealTestScriptAsync(1, 1, "test-api-key", "gpt-4");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Healed script", result);
        Assert.Contains("robust selectors", result);
    }

    [Fact]
    public async Task HealTestScriptAsync_OpenAIRateLimitError_ThrowsHttpRequestException()
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
        var execution = new TestExecution
        {
            Id = 1,
            TestCaseId = 1,
            ProjectId = 1,
            Status = "Failed",
            ErrorMessage = "Error",
            TestCase = testCase,
            Project = project
        };

        context.Projects.Add(project);
        context.TestCases.Add(testCase);
        context.TestExecutions.Add(execution);
        await context.SaveChangesAsync();

        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.TooManyRequests,
                Content = new StringContent("{\"error\": {\"message\": \"Rate limit exceeded\"}}")
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var service = new AITestHealingService(context, httpClient);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => service.HealTestScriptAsync(1, 1, "test-api-key", "gpt-4"));

        Assert.Contains("Rate limit exceeded", exception.Message);
    }

    [Fact]
    public async Task HealTestScriptAsync_OpenAIUnauthorizedError_ThrowsHttpRequestException()
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
        var execution = new TestExecution
        {
            Id = 1,
            TestCaseId = 1,
            ProjectId = 1,
            Status = "Failed",
            ErrorMessage = "Error",
            TestCase = testCase,
            Project = project
        };

        context.Projects.Add(project);
        context.TestCases.Add(testCase);
        context.TestExecutions.Add(execution);
        await context.SaveChangesAsync();

        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Unauthorized,
                Content = new StringContent("{\"error\": {\"message\": \"Invalid API key\"}}")
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var service = new AITestHealingService(context, httpClient);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => service.HealTestScriptAsync(1, 1, "test-api-key", "gpt-4"));

        Assert.Contains("Invalid API key", exception.Message);
    }

    [Fact]
    public async Task HealTestScriptAsync_CleansMarkdownFormatting()
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
        var execution = new TestExecution
        {
            Id = 1,
            TestCaseId = 1,
            ProjectId = 1,
            Status = "Failed",
            ErrorMessage = "Error",
            TestCase = testCase,
            Project = project
        };

        context.Projects.Add(project);
        context.TestCases.Add(testCase);
        context.TestExecutions.Add(execution);
        await context.SaveChangesAsync();

        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        var mockResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(@"{
                ""choices"": [{
                    ""message"": {
                        ""content"": ""```csharp\n// Healed script\nawait Page.ClickAsync(\""#button\"");\n```""
                    }
                }]
            }")
        };

        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(mockResponse);

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var service = new AITestHealingService(context, httpClient);

        // Act
        var result = await service.HealTestScriptAsync(1, 1, "test-api-key", "gpt-4");

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("```csharp", result);
        Assert.DoesNotContain("```", result);
        Assert.Contains("Healed script", result);
    }
}
