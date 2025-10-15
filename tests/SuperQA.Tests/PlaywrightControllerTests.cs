using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using SuperQA.Api.Controllers;
using SuperQA.Core.Interfaces;
using SuperQA.Infrastructure.Data;
using SuperQA.Shared.DTOs;

namespace SuperQA.Tests;

public class PlaywrightControllerTests
{
    private readonly Mock<IOpenAIService> _mockOpenAIService;
    private readonly Mock<IPlaywrightTestExecutor> _mockPlaywrightTestExecutor;
    private readonly Mock<IUserSettingsService> _mockUserSettingsService;
    private readonly SuperQADbContext _context;
    private readonly Mock<IMemoryCache> _mockMemoryCache;
    private readonly PlaywrightController _controller;

    public PlaywrightControllerTests()
    {
        _mockOpenAIService = new Mock<IOpenAIService>();
        _mockPlaywrightTestExecutor = new Mock<IPlaywrightTestExecutor>();
        _mockUserSettingsService = new Mock<IUserSettingsService>();
        _mockMemoryCache = new Mock<IMemoryCache>();

        var options = new DbContextOptionsBuilder<SuperQADbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new SuperQADbContext(options);

        _controller = new PlaywrightController(
            _mockOpenAIService.Object,
            _mockPlaywrightTestExecutor.Object,
            _mockUserSettingsService.Object,
            _context,
            _mockMemoryCache.Object);
    }

    [Fact]
    public async Task GenerateFromExtension_WithFillActionAndNoValue_IndicatesAIShouldFillData()
    {
        // Arrange
        var request = new GenerateFromExtensionRequest
        {
            TestName = "Login Test",
            ApplicationUrl = "https://example.com",
            OpenAIApiKey = "test-key",
            Model = "gpt-4o-mini",
            Steps = new List<BrowserExtensionStep>
            {
                new BrowserExtensionStep
                {
                    Action = "fill",
                    Description = "Fill username field",
                    Locator = "#username",
                    Value = "" // Empty value - AI should fill this
                },
                new BrowserExtensionStep
                {
                    Action = "fill",
                    Description = "Fill password field",
                    Locator = "#password",
                    Value = null! // Null value - AI should fill this
                }
            }
        };

        string capturedFrsText = string.Empty;
        _mockOpenAIService
            .Setup(x => x.GeneratePlaywrightTestScriptAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Callback<string, string, string, string, string>((frs, url, key, model, structure) =>
            {
                capturedFrsText = frs;
            })
            .ReturnsAsync("generated script");

        // Act
        var result = await _controller.GenerateFromExtension(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<PlaywrightTestGenerationResponse>(okResult.Value);
        Assert.True(response.Success);

        // Verify FRS contains instruction for AI to generate test data
        Assert.Contains("[AI: Generate appropriate test data based on field name/type]", capturedFrsText);
    }

    [Fact]
    public async Task GenerateFromExtension_WithFillActionAndValue_PreservesValue()
    {
        // Arrange
        var request = new GenerateFromExtensionRequest
        {
            TestName = "Login Test",
            ApplicationUrl = "https://example.com",
            OpenAIApiKey = "test-key",
            Model = "gpt-4o-mini",
            Steps = new List<BrowserExtensionStep>
            {
                new BrowserExtensionStep
                {
                    Action = "fill",
                    Description = "Fill username field",
                    Locator = "#username",
                    Value = "admin" // Has value - should be preserved
                }
            }
        };

        string capturedFrsText = string.Empty;
        _mockOpenAIService
            .Setup(x => x.GeneratePlaywrightTestScriptAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Callback<string, string, string, string, string>((frs, url, key, model, structure) =>
            {
                capturedFrsText = frs;
            })
            .ReturnsAsync("generated script");

        // Act
        var result = await _controller.GenerateFromExtension(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<PlaywrightTestGenerationResponse>(okResult.Value);
        Assert.True(response.Success);

        // Verify FRS contains the actual value
        Assert.Contains("Value: admin", capturedFrsText);
        // Should NOT contain AI instruction since value is provided
        Assert.DoesNotContain("[AI: Generate appropriate test data", capturedFrsText);
    }

    [Fact]
    public async Task GenerateFromExtension_WithClickAction_DoesNotIndicateAIShouldFill()
    {
        // Arrange
        var request = new GenerateFromExtensionRequest
        {
            TestName = "Login Test",
            ApplicationUrl = "https://example.com",
            OpenAIApiKey = "test-key",
            Model = "gpt-4o-mini",
            Steps = new List<BrowserExtensionStep>
            {
                new BrowserExtensionStep
                {
                    Action = "click",
                    Description = "Click login button",
                    Locator = "#login-btn",
                    Value = "" // Empty value but click doesn't need a value
                }
            }
        };

        string capturedFrsText = string.Empty;
        _mockOpenAIService
            .Setup(x => x.GeneratePlaywrightTestScriptAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Callback<string, string, string, string, string>((frs, url, key, model, structure) =>
            {
                capturedFrsText = frs;
            })
            .ReturnsAsync("generated script");

        // Act
        var result = await _controller.GenerateFromExtension(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<PlaywrightTestGenerationResponse>(okResult.Value);
        Assert.True(response.Success);

        // Click actions should NOT have AI instruction for test data
        Assert.DoesNotContain("[AI: Generate appropriate test data", capturedFrsText);
    }

    [Fact]
    public async Task GenerateFromExtension_WithTypeActionAndNoValue_IndicatesAIShouldFillData()
    {
        // Arrange
        var request = new GenerateFromExtensionRequest
        {
            TestName = "Search Test",
            ApplicationUrl = "https://example.com",
            OpenAIApiKey = "test-key",
            Model = "gpt-4o-mini",
            Steps = new List<BrowserExtensionStep>
            {
                new BrowserExtensionStep
                {
                    Action = "type",
                    Description = "Type in search box",
                    Locator = "#search",
                    Value = "" // Empty value - AI should fill this
                }
            }
        };

        string capturedFrsText = string.Empty;
        _mockOpenAIService
            .Setup(x => x.GeneratePlaywrightTestScriptAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Callback<string, string, string, string, string>((frs, url, key, model, structure) =>
            {
                capturedFrsText = frs;
            })
            .ReturnsAsync("generated script");

        // Act
        var result = await _controller.GenerateFromExtension(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<PlaywrightTestGenerationResponse>(okResult.Value);
        Assert.True(response.Success);

        // Verify FRS contains instruction for AI to generate test data
        Assert.Contains("[AI: Generate appropriate test data based on field name/type]", capturedFrsText);
    }
}
