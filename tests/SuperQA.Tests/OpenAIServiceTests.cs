using System.Net;
using Moq;
using Moq.Protected;
using SuperQA.Infrastructure.Services;

namespace SuperQA.Tests;

public class OpenAIServiceTests
{
    [Fact]
    public async Task GeneratePlaywrightTestScriptAsync_RateLimitExceeded_ThrowsWithFriendlyMessage()
    {
        // Arrange
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
        var service = new OpenAIService(httpClient, new CSharpSyntaxValidationService());

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => service.GeneratePlaywrightTestScriptAsync(
                "test frs",
                "https://example.com",
                "test-api-key",
                "gpt-4o-mini"));

        Assert.Contains("Rate limit exceeded", exception.Message);
        Assert.Contains("too many requests", exception.Message.ToLower());
    }

    [Fact]
    public async Task GeneratePlaywrightTestScriptAsync_UnauthorizedError_ThrowsWithFriendlyMessage()
    {
        // Arrange
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
        var service = new OpenAIService(httpClient, new CSharpSyntaxValidationService());

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => service.GeneratePlaywrightTestScriptAsync(
                "test frs",
                "https://example.com",
                "test-api-key",
                "gpt-4o-mini"));

        Assert.Contains("Invalid API key", exception.Message);
    }

    [Fact]
    public async Task GeneratePlaywrightTestScriptAsync_PaymentRequired_ThrowsWithFriendlyMessage()
    {
        // Arrange
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.PaymentRequired,
                Content = new StringContent("{\"error\": {\"message\": \"Quota exceeded\"}}")
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var service = new OpenAIService(httpClient, new CSharpSyntaxValidationService());

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => service.GeneratePlaywrightTestScriptAsync(
                "test frs",
                "https://example.com",
                "test-api-key",
                "gpt-4o-mini"));

        Assert.Contains("Payment required", exception.Message);
        Assert.Contains("insufficient credits", exception.Message.ToLower());
    }

    [Fact]
    public async Task GeneratePlaywrightTestScriptAsync_SuccessfulResponse_ReturnsGeneratedScript()
    {
        // Arrange
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        var responseContent = @"{
            ""choices"": [
                {
                    ""message"": {
                        ""content"": ""```csharp\nusing Microsoft.Playwright;\n// Test script\n```""
                    }
                }
            ]
        }";

        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseContent)
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var service = new OpenAIService(httpClient, new CSharpSyntaxValidationService());

        // Act
        var result = await service.GeneratePlaywrightTestScriptAsync(
            "test frs",
            "https://example.com",
            "test-api-key",
            "gpt-4o-mini");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("using Microsoft.Playwright", result);
    }

    [Fact]
    public async Task GeneratePlaywrightTestScriptAsync_SuccessfulResponse_DoesNotContainPlaywrightSharp()
    {
        // Arrange
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        var responseContent = @"{
            ""choices"": [
                {
                    ""message"": {
                        ""content"": ""```csharp\nusing Microsoft.Playwright;\nusing Microsoft.Playwright.NUnit;\nusing NUnit.Framework;\n\nnamespace PlaywrightTests;\n\n[TestFixture]\npublic class Tests : PageTest\n{\n    [Test]\n    public async Task BasicTest()\n    {\n        await Page.GotoAsync(\""https://example.com\"");\n    }\n}\n```""
                    }
                }
            ]
        }";

        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseContent)
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var service = new OpenAIService(httpClient, new CSharpSyntaxValidationService());

        // Act
        var result = await service.GeneratePlaywrightTestScriptAsync(
            "test frs",
            "https://example.com",
            "test-api-key",
            "gpt-4o-mini");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("using Microsoft.Playwright", result);
        Assert.Contains("using Microsoft.Playwright.NUnit", result);
        Assert.Contains("PageTest", result);
        // Ensure it does NOT contain the deprecated PlaywrightSharp namespace
        Assert.DoesNotContain("PlaywrightSharp", result);
        Assert.DoesNotContain("IBrowser", result); // IBrowser is from the old API
        Assert.DoesNotContain("IPage", result); // IPage is from the old API
    }

    [Fact]
    public async Task GeneratePlaywrightTestScriptAsync_WithPageStructure_SendsEnhancedPrompt()
    {
        // Arrange
        var capturedRequest = string.Empty;
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        var responseContent = @"{
            ""choices"": [
                {
                    ""message"": {
                        ""content"": ""using Microsoft.Playwright;\nusing NUnit.Framework;\n\npublic class Test : PageTest\n{\n    [Test]\n    public async Task TestMethod()\n    {\n        await Page.GotoAsync(\""https://example.com\"");\n        await Page.Locator(\""#username\"").FillAsync(\""testuser\"");\n    }\n}""
                    }
                }
            ]
        }";

        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage req, CancellationToken ct) =>
            {
                capturedRequest = req.Content!.ReadAsStringAsync().Result;
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(responseContent)
                };
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var service = new OpenAIService(httpClient, new CSharpSyntaxValidationService());

        // Act
        var result = await service.GeneratePlaywrightTestScriptAsync(
            "Fill username field",
            "https://example.com",
            "test-api-key",
            "gpt-4o-mini",
            "[{\"type\":\"fill\",\"selector\":\"#username\"}]");

        // Assert - verify the enhanced prompt is used
        Assert.Contains("COMPLETE, RUNNABLE C# code", capturedRequest);
        Assert.Contains("NO syntax errors", capturedRequest);
        Assert.Contains("generate appropriate test data", capturedRequest);
        Assert.Contains("test@example.com", capturedRequest);
        Assert.Contains("testuser", capturedRequest);
    }

    [Fact]
    public async Task GeneratePlaywrightTestScriptAsync_IncreasedMaxTokens_AllowsLongerResponses()
    {
        // Arrange
        var capturedRequest = string.Empty;
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        var validCSharpCode = @"using Microsoft.Playwright;
using NUnit.Framework;

namespace Tests
{
    public class MyTest
    {
        [Test]
        public async Task TestMethod()
        {
            // Test code
        }
    }
}";
        var responseContent = @"{
            ""choices"": [
                {
                    ""message"": {
                        ""content"": """ + validCSharpCode.Replace("\"", "\\\"").Replace("\n", "\\n") + @"""
                    }
                }
            ]
        }";

        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage req, CancellationToken ct) =>
            {
                capturedRequest = req.Content!.ReadAsStringAsync().Result;
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(responseContent)
                };
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var service = new OpenAIService(httpClient, new CSharpSyntaxValidationService());

        // Act
        await service.GeneratePlaywrightTestScriptAsync(
            "test frs",
            "https://example.com",
            "test-api-key",
            "gpt-4o-mini");

        // Assert - verify max_tokens is increased to 2000
        Assert.Contains("\"max_tokens\":2000", capturedRequest.Replace(" ", ""));
    }
}
