using Microsoft.EntityFrameworkCore;
using Moq;
using Moq.Protected;
using SuperQA.Core.Entities;
using SuperQA.Core.Interfaces;
using SuperQA.Infrastructure.Data;
using SuperQA.Infrastructure.Services;
using System.Net;
using System.Text.Json;
using Xunit;

namespace SuperQA.Tests;

/// <summary>
/// Tests for the targeted AI healing approach that fixes only failing lines
/// This reduces token costs and prevents over-correction
/// </summary>
public class AITargetedHealingTests : IDisposable
{
    private readonly SuperQADbContext _context;
    private readonly AITestHealingService _service;
    private readonly Mock<HttpMessageHandler> _mockHttpHandler;
    private readonly Mock<ILocatorValidationService> _mockValidationService;
    private readonly Mock<IScriptComparisonService> _mockComparisonService;
    private readonly Mock<ICSharpSyntaxValidationService> _mockSyntaxValidationService;

    public AITargetedHealingTests()
    {
        var options = new DbContextOptionsBuilder<SuperQADbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new SuperQADbContext(options);

        _mockHttpHandler = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(_mockHttpHandler.Object);

        _mockValidationService = new Mock<ILocatorValidationService>();
        _mockComparisonService = new Mock<IScriptComparisonService>();
        _mockSyntaxValidationService = new Mock<ICSharpSyntaxValidationService>();

        _service = new AITestHealingService(
            _context,
            httpClient,
            _mockValidationService.Object,
            _mockComparisonService.Object,
            _mockSyntaxValidationService.Object);
    }

    [Fact]
    public async Task TargetedHealing_SuccessfullyHealsFailure()
    {
        // Arrange - Simple test case where AI should fix the issue
        var testCase = new TestCase
        {
            Id = 1,
            Title = "Test Element Not Found",
            Description = "Test where an element is not found",
            Steps = "Test steps",
            ExpectedResults = "Expected results",
            AutomationScript = @"await Page.GotoAsync(""https://test.com"");
await Page.ClickAsync(""#old-button"");
await Page.FillAsync(""#username"", ""test"");",
            ProjectId = 1
        };

        var execution = new TestExecution
        {
            Id = 1,
            TestCaseId = 1,
            ProjectId = 1,
            Status = "Failed",
            ErrorMessage = @"Locator(""#old-button"") not found",
            StackTrace = null,
            ExecutedAt = DateTime.UtcNow
        };

        _context.TestCases.Add(testCase);
        _context.TestExecutions.Add(execution);
        await _context.SaveChangesAsync();

        // Mock OpenAI response - return complete healed script
        var healedScriptResponse = @"await Page.GotoAsync(""https://test.com"");
await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
await Page.Locator(""[data-testid='submit-button']"").ClickAsync();
await Page.FillAsync(""#username"", ""test"");";

        SetupMockOpenAIResponse(healedScriptResponse);

        _mockSyntaxValidationService
            .Setup(s => s.ValidateSyntaxWithDetails(It.IsAny<string>()))
            .Returns((true, string.Empty));

        _mockComparisonService
            .Setup(s => s.ValidateHealedScript(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(true);

        // Act
        var healedScript = await _service.HealTestScriptAsync(
            testCaseId: 1,
            executionId: 1,
            apiKey: "test-key",
            model: "gpt-4");

        // Assert
        Assert.NotNull(healedScript);
        Assert.Contains("WaitForLoadStateAsync", healedScript);
        Assert.Contains("[data-testid='submit-button']", healedScript);
        Assert.Contains("#username", healedScript); // Working locator preserved
        
        // Verify healing history was created
        var history = await _context.HealingHistories
            .FirstOrDefaultAsync(h => h.TestCaseId == 1);
        
        Assert.NotNull(history);
        Assert.True(history.WasSuccessful);
    }

    [Fact]
    public async Task TargetedHealing_FallsBackToFullHealing_WhenContextCannotBeExtracted()
    {
        // Arrange
        var testCase = new TestCase
        {
            Id = 2,
            Title = "Test without clear failing line",
            Description = "Test where we can't identify the exact failing line",
            Steps = "Test steps",
            ExpectedResults = "Expected results",
            AutomationScript = @"await Page.GotoAsync(""https://test.com"");
await Page.ClickAsync(""button"");",
            ProjectId = 1
        };

        var execution = new TestExecution
        {
            Id = 2,
            TestCaseId = 2,
            ProjectId = 1,
            Status = "Failed",
            ErrorMessage = "Generic timeout error with no specific locator information",
            StackTrace = null,
            ExecutedAt = DateTime.UtcNow
        };

        _context.TestCases.Add(testCase);
        _context.TestExecutions.Add(execution);
        await _context.SaveChangesAsync();

        // Mock OpenAI response for full healing
        var mockResponse = @"await Page.GotoAsync(""https://test.com"");
await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
await Page.Locator(""[data-testid='submit-button']"").ClickAsync();";

        SetupMockOpenAIResponse(mockResponse);

        _mockSyntaxValidationService
            .Setup(s => s.ValidateSyntaxWithDetails(It.IsAny<string>()))
            .Returns((true, string.Empty));

        _mockComparisonService
            .Setup(s => s.ValidateHealedScript(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(true);

        // Act
        var healedScript = await _service.HealTestScriptAsync(
            testCaseId: 2,
            executionId: 2,
            apiKey: "test-key",
            model: "gpt-4");

        // Assert
        Assert.NotNull(healedScript);
        
        // Verify it fell back to full healing (returned complete script)
        Assert.Contains("WaitForLoadStateAsync", healedScript);
    }

    [Fact]
    public async Task TargetedHealing_ExtractsFailingLocator_FromErrorMessage()
    {
        // Arrange
        var testCase = new TestCase
        {
            Id = 3,
            Title = "Test locator extraction",
            Description = "Test that we can extract the failing locator from error",
            Steps = "Test steps",
            ExpectedResults = "Expected results",
            AutomationScript = @"await Page.Locator(""#username"").FillAsync(""test"");
await Page.Locator(""#password"").FillAsync(""pass"");
await Page.Locator(""#submit-button"").ClickAsync();
await Page.Locator(""//div[@class='result']"").WaitForAsync();",
            ProjectId = 1
        };

        var execution = new TestExecution
        {
            Id = 3,
            TestCaseId = 3,
            ProjectId = 1,
            Status = "Failed",
            ErrorMessage = @"Locator(""//div[@class='result']"") not found",
            StackTrace = null,
            ExecutedAt = DateTime.UtcNow
        };

        _context.TestCases.Add(testCase);
        _context.TestExecutions.Add(execution);
        await _context.SaveChangesAsync();

        // Mock OpenAI response - return complete healed script
        var healedScriptResponse = @"await Page.Locator(""#username"").FillAsync(""test"");
await Page.Locator(""#password"").FillAsync(""pass"");
await Page.Locator(""#submit-button"").ClickAsync();
await Page.Locator(""[data-testid='result']"").WaitForAsync();";
        
        SetupMockOpenAIResponse(healedScriptResponse);

        _mockSyntaxValidationService
            .Setup(s => s.ValidateSyntaxWithDetails(It.IsAny<string>()))
            .Returns((true, string.Empty));

        _mockComparisonService
            .Setup(s => s.ValidateHealedScript(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(true);

        // Act
        var healedScript = await _service.HealTestScriptAsync(
            testCaseId: 3,
            executionId: 3,
            apiKey: "test-key",
            model: "gpt-4");

        // Assert
        Assert.NotNull(healedScript);
        
        // Verify the healed script contains all locators with the failing one fixed
        Assert.Contains("#username", healedScript);
        Assert.Contains("#password", healedScript);
        Assert.Contains("#submit-button", healedScript);
        Assert.Contains("[data-testid='result']", healedScript);
        Assert.DoesNotContain("//div[@class='result']", healedScript);
    }

    [Fact]
    public async Task TargetedHealing_PreservesIndentation_WhenReplacingLine()
    {
        // Arrange
        var testCase = new TestCase
        {
            Id = 4,
            Title = "Test indentation preservation",
            Description = "Test that we preserve indentation when fixing lines",
            Steps = "Test steps",
            ExpectedResults = "Expected results",
            AutomationScript = @"public async Task Test()
{
    await Page.GotoAsync(""https://test.com"");
    
    if (true)
    {
        await Page.Locator(""button"").ClickAsync();
    }
}",
            ProjectId = 1
        };

        var execution = new TestExecution
        {
            Id = 4,
            TestCaseId = 4,
            ProjectId = 1,
            Status = "Failed",
            ErrorMessage = @"Locator(""button"") resolved to multiple elements",
            StackTrace = null,
            ExecutedAt = DateTime.UtcNow
        };

        _context.TestCases.Add(testCase);
        _context.TestExecutions.Add(execution);
        await _context.SaveChangesAsync();

        var mockResponse = @"await Page.Locator(""[data-testid='submit-button']"").ClickAsync();";
        
        SetupMockOpenAIResponse(mockResponse);

        _mockSyntaxValidationService
            .Setup(s => s.ValidateSyntaxWithDetails(It.IsAny<string>()))
            .Returns((true, string.Empty));

        _mockComparisonService
            .Setup(s => s.ValidateHealedScript(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(true);

        // Act
        var healedScript = await _service.HealTestScriptAsync(
            testCaseId: 4,
            executionId: 4,
            apiKey: "test-key",
            model: "gpt-4");

        // Assert
        Assert.NotNull(healedScript);
        
        // Verify the indentation is preserved (8 spaces before the line in the if block)
        Assert.Contains("        await Page.Locator(\"[data-testid='submit-button']\").ClickAsync();", healedScript);
    }

    [Fact]
    public async Task TargetedHealing_FallsBackToFullHealing_WhenSyntaxValidationFails()
    {
        // Arrange
        var testCase = new TestCase
        {
            Id = 5,
            Title = "Test fallback on syntax error",
            Description = "Test that we fall back to full healing if targeted healing has syntax errors",
            Steps = "Test steps",
            ExpectedResults = "Expected results",
            AutomationScript = @"await Page.Locator(""#test"").ClickAsync();
await Page.Locator(""#broken"").ClickAsync();",
            ProjectId = 1
        };

        var execution = new TestExecution
        {
            Id = 5,
            TestCaseId = 5,
            ProjectId = 1,
            Status = "Failed",
            ErrorMessage = @"Locator(""#broken"") not found",
            StackTrace = null,
            ExecutedAt = DateTime.UtcNow
        };

        _context.TestCases.Add(testCase);
        _context.TestExecutions.Add(execution);
        await _context.SaveChangesAsync();

        var callCount = 0;
        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                string responseContent;
                
                if (callCount == 1)
                {
                    // First call: targeted healing returns invalid syntax
                    responseContent = "await Page.Locator(#fixed).ClickAsync();"; // Missing quotes
                }
                else
                {
                    // Second call: full healing returns valid code
                    responseContent = @"await Page.Locator(""#test"").ClickAsync();
await Page.Locator(""[data-testid='fixed']"").ClickAsync();";
                }

                var mockApiResponse = new
                {
                    choices = new[]
                    {
                        new
                        {
                            message = new
                            {
                                content = responseContent
                            }
                        }
                    }
                };

                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonSerializer.Serialize(mockApiResponse))
                };
            });

        // First validation (targeted healing) fails
        // Second validation (full healing) succeeds
        var validationCallCount = 0;
        _mockSyntaxValidationService
            .Setup(s => s.ValidateSyntaxWithDetails(It.IsAny<string>()))
            .Returns(() =>
            {
                validationCallCount++;
                if (validationCallCount == 1)
                    return (false, "Syntax error in targeted healing");
                return (true, string.Empty);
            });

        _mockComparisonService
            .Setup(s => s.ValidateHealedScript(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(true);

        // Act
        var healedScript = await _service.HealTestScriptAsync(
            testCaseId: 5,
            executionId: 5,
            apiKey: "test-key",
            model: "gpt-4");

        // Assert
        Assert.NotNull(healedScript);
        Assert.Contains("[data-testid='fixed']", healedScript);
        
        // Verify it fell back to full healing (made 2 API calls)
        Assert.Equal(2, callCount);
    }

    [Fact]
    public async Task TargetedHealing_WorksWithLargeScripts()
    {
        // Arrange - Demonstrate that healing works even with large scripts
        var largeScript = string.Join(Environment.NewLine, Enumerable.Range(1, 20).Select(i =>
            $"        await Page.Locator(\"#element{i}\").ClickAsync();"));

        var testCase = new TestCase
        {
            Id = 6,
            Title = "Large test script",
            Description = "Test with many steps",
            Steps = "Test steps",
            ExpectedResults = "Expected results",
            AutomationScript = $@"public async Task LargeTest()
{{
{largeScript}
}}",
            ProjectId = 1
        };

        var execution = new TestExecution
        {
            Id = 6,
            TestCaseId = 6,
            ProjectId = 1,
            Status = "Failed",
            ErrorMessage = @"Locator(""#element10"") not found",
            StackTrace = null,
            ExecutedAt = DateTime.UtcNow
        };

        _context.TestCases.Add(testCase);
        _context.TestExecutions.Add(execution);
        await _context.SaveChangesAsync();

        // Return complete healed script
        var healedResponse = testCase.AutomationScript.Replace(
            "await Page.Locator(\"#element10\").ClickAsync();",
            "await Page.Locator(\"[data-testid='element10']\").ClickAsync();");

        SetupMockOpenAIResponse(healedResponse);

        _mockSyntaxValidationService
            .Setup(s => s.ValidateSyntaxWithDetails(It.IsAny<string>()))
            .Returns((true, string.Empty));

        _mockComparisonService
            .Setup(s => s.ValidateHealedScript(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(true);

        // Act
        var healedScript = await _service.HealTestScriptAsync(
            testCaseId: 6,
            executionId: 6,
            apiKey: "test-key",
            model: "gpt-4");

        // Assert
        Assert.NotNull(healedScript);
        
        // Verify the healing fixed the specific failing element
        Assert.Contains("[data-testid='element10']", healedScript);
        
        // Verify other elements were preserved
        Assert.Contains("#element1", healedScript);
        Assert.Contains("#element20", healedScript);
    }

    private void SetupMockOpenAIResponse(string responseContent)
    {
        var mockApiResponse = new
        {
            choices = new[]
            {
                new
                {
                    message = new
                    {
                        content = responseContent
                    }
                }
            }
        };

        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(mockApiResponse))
            });
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
