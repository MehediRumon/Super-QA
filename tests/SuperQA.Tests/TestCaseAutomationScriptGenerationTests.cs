using SuperQA.Core.Entities;
using SuperQA.Core.Interfaces;
using SuperQA.Infrastructure.Services;
using Xunit;
using Moq;

namespace SuperQA.Tests;

public class TestCaseAutomationScriptGenerationTests
{
    private readonly Mock<IMCPService> _mockMcpService;
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly HttpClient _httpClient;
    private readonly AITestGeneratorService _service;

    public TestCaseAutomationScriptGenerationTests()
    {
        _mockMcpService = new Mock<IMCPService>();
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
        _service = new AITestGeneratorService(_mockMcpService.Object, _httpClient);
    }

    [Fact]
    public async Task GenerateAutomationScriptAsync_WithPageStructure_IncludesPageElements()
    {
        // Arrange
        var testCase = new TestCase
        {
            Id = 1,
            Title = "Login Test",
            Description = "Test user login functionality",
            Preconditions = "User is on login page",
            Steps = "1. Enter username\n2. Enter password\n3. Click login button",
            ExpectedResults = "User is logged in successfully",
            ProjectId = 1
        };

        var pageStructure = @"[
  {
    ""type"": ""input"",
    ""selector"": ""#username"",
    ""inputType"": ""text"",
    ""name"": ""username"",
    ""id"": ""username""
  },
  {
    ""type"": ""input"",
    ""selector"": ""#password"",
    ""inputType"": ""password"",
    ""name"": ""password"",
    ""id"": ""password""
  },
  {
    ""type"": ""button"",
    ""selector"": ""#loginBtn"",
    ""text"": ""Login"",
    ""id"": ""loginBtn""
  }
]";

        var expectedResponse = @"using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace PlaywrightTests;

[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class Tests : PageTest
{
    [Test]
    public async Task Login_Test()
    {
        await Page.GotoAsync(""https://example.com/login"");
        await Page.FillAsync(""#username"", ""testuser"");
        await Page.FillAsync(""#password"", ""password123"");
        await Page.ClickAsync(""#loginBtn"");
        await Expect(Page).ToHaveURLAsync(""https://example.com/dashboard"");
    }
}";

        _mockMcpService
            .Setup(x => x.SendPromptAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _service.GenerateAutomationScriptAsync(testCase, "Playwright", pageStructure);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("using Microsoft.Playwright", result);
        Assert.Contains("PageTest", result);
        Assert.DoesNotContain("```", result); // Should be cleaned up
        
        // Verify the prompt included the page structure
        _mockMcpService.Verify(
            x => x.SendPromptAsync(
                It.Is<string>(s => s.Contains("ACTUAL PAGE ELEMENTS") && s.Contains("#username")),
                It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task GenerateAutomationScriptAsync_WithoutPageStructure_GeneratesGenericScript()
    {
        // Arrange
        var testCase = new TestCase
        {
            Id = 1,
            Title = "Search Test",
            Description = "Test search functionality",
            Preconditions = "User is on home page",
            Steps = "1. Enter search term\n2. Click search button",
            ExpectedResults = "Search results are displayed",
            ProjectId = 1
        };

        var expectedResponse = @"// Generic automation script
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;";

        _mockMcpService
            .Setup(x => x.SendPromptAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _service.GenerateAutomationScriptAsync(testCase, "Playwright", null);

        // Assert
        Assert.NotNull(result);
        
        // Verify the prompt did NOT include page elements section
        _mockMcpService.Verify(
            x => x.SendPromptAsync(
                It.Is<string>(s => !s.Contains("ACTUAL PAGE ELEMENTS")),
                It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task GenerateAutomationScriptAsync_HandlesMarkdownCodeBlocks()
    {
        // Arrange
        var testCase = new TestCase
        {
            Id = 1,
            Title = "Test Case",
            Description = "Description",
            Steps = "Steps",
            ExpectedResults = "Results",
            ProjectId = 1
        };

        var responseWithMarkdown = @"```csharp
using Microsoft.Playwright;

public class Test {}
```";

        _mockMcpService
            .Setup(x => x.SendPromptAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(responseWithMarkdown);

        // Act
        var result = await _service.GenerateAutomationScriptAsync(testCase, "Playwright", null);

        // Assert
        Assert.DoesNotContain("```csharp", result);
        Assert.DoesNotContain("```", result);
        Assert.Contains("using Microsoft.Playwright", result);
    }

    [Fact]
    public async Task GenerateAutomationScriptAsync_SanitizesTestName()
    {
        // Arrange
        var testCase = new TestCase
        {
            Id = 1,
            Title = "Test Case With Special Characters!@#$%",
            Description = "Description",
            Steps = "Steps",
            ExpectedResults = "Results",
            ProjectId = 1
        };

        _mockMcpService
            .Setup(x => x.SendPromptAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("test script");

        // Act
        var result = await _service.GenerateAutomationScriptAsync(testCase, "Playwright", null);

        // Assert
        Assert.NotNull(result);
        
        // Verify the prompt contains sanitized test name
        _mockMcpService.Verify(
            x => x.SendPromptAsync(
                It.Is<string>(s => s.Contains("Test_Case_With_Special_Characters")),
                It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task GenerateAutomationScriptAsync_OnMcpFailure_ReturnsFallbackScript()
    {
        // Arrange
        var testCase = new TestCase
        {
            Id = 1,
            Title = "Fallback Test",
            Description = "Description",
            Steps = "Steps",
            ExpectedResults = "Results",
            ProjectId = 1
        };

        _mockMcpService
            .Setup(x => x.SendPromptAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("MCP service unavailable"));

        // Act
        var result = await _service.GenerateAutomationScriptAsync(testCase, "Playwright", null);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Fallback Test", result);
        Assert.Contains("Playwright", result);
        Assert.Contains("// Note: Add actual implementation", result);
    }

    [Fact]
    public void ExtractUrlFromTestCase_WithUrlInSteps_ReturnsUrl()
    {
        // Arrange
        var testCase = new TestCase
        {
            Id = 1,
            Title = "Login Test",
            Description = "Test login functionality",
            Preconditions = "User has valid credentials",
            Steps = "1. Navigate to https://example.com/login\n2. Enter username\n3. Click submit",
            ExpectedResults = "User is logged in",
            ProjectId = 1
        };

        // Act
        var url = _service.ExtractUrlFromTestCase(testCase);

        // Assert
        Assert.Equal("https://example.com/login", url);
    }

    [Fact]
    public void ExtractUrlFromTestCase_WithUrlInPreconditions_ReturnsUrl()
    {
        // Arrange
        var testCase = new TestCase
        {
            Id = 1,
            Title = "Test",
            Description = "Description",
            Preconditions = "User is on http://localhost:5000/home page",
            Steps = "1. Click button",
            ExpectedResults = "Action completed",
            ProjectId = 1
        };

        // Act
        var url = _service.ExtractUrlFromTestCase(testCase);

        // Assert
        Assert.Equal("http://localhost:5000/home", url);
    }

    [Fact]
    public void ExtractUrlFromTestCase_WithUrlInDescription_ReturnsUrl()
    {
        // Arrange
        var testCase = new TestCase
        {
            Id = 1,
            Title = "Test",
            Description = "Test the application at https://app.example.com/dashboard",
            Preconditions = "Prerequisites",
            Steps = "1. Perform action",
            ExpectedResults = "Expected outcome",
            ProjectId = 1
        };

        // Act
        var url = _service.ExtractUrlFromTestCase(testCase);

        // Assert
        Assert.Equal("https://app.example.com/dashboard", url);
    }

    [Fact]
    public void ExtractUrlFromTestCase_WithNoUrl_ReturnsNull()
    {
        // Arrange
        var testCase = new TestCase
        {
            Id = 1,
            Title = "Test",
            Description = "No URL here",
            Preconditions = "No URL",
            Steps = "1. Do something\n2. Verify result",
            ExpectedResults = "Test passes",
            ProjectId = 1
        };

        // Act
        var url = _service.ExtractUrlFromTestCase(testCase);

        // Assert
        Assert.Null(url);
    }

    [Fact]
    public void ExtractUrlFromTestCase_WithUrlAndTrailingPunctuation_RemovesPunctuation()
    {
        // Arrange
        var testCase = new TestCase
        {
            Id = 1,
            Title = "Test",
            Description = "Test description",
            Preconditions = "User is ready",
            Steps = "Navigate to https://example.com/login, then click the button at https://example.com/dashboard.",
            ExpectedResults = "Expected outcome",
            ProjectId = 1
        };

        // Act
        var url = _service.ExtractUrlFromTestCase(testCase);

        // Assert
        // Should get the first URL found and remove trailing punctuation
        Assert.Equal("https://example.com/login", url);
    }
}
