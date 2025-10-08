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
        var service = new OpenAIService(httpClient);

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
        var service = new OpenAIService(httpClient);

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
        var service = new OpenAIService(httpClient);

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
        var service = new OpenAIService(httpClient);

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
        var service = new OpenAIService(httpClient);

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
}
