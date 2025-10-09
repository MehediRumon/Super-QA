using SuperQA.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace SuperQA.Tests;

public class PageInspectorErrorHandlingTests
{
    private IConfiguration CreateConfiguration()
    {
        var configData = new Dictionary<string, string>
        {
            { "Playwright:Headless", "true" }
        };
        
        return new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();
    }

    [Fact]
    public async Task GetPageStructureAsync_WhenBrowsersNotInstalled_ReturnsHelpfulErrorMessage()
    {
        // Arrange
        var service = new PageInspectorService(CreateConfiguration());
        
        // Act - Use a valid URL but with missing browsers scenario
        var result = await service.GetPageStructureAsync("http://example.com");
        
        // Assert
        Assert.NotNull(result);
        Assert.Contains("error", result.ToLower());
        
        // Verify the error message is helpful
        if (result.Contains("Executable doesn't exist") || result.Contains("Browser is not installed"))
        {
            Assert.Contains("install", result.ToLower());
            Assert.Contains("playwright", result.ToLower());
        }
    }

    [Fact]
    public async Task GetPageStructureAsync_WithInvalidUrl_ReturnsErrorStructure()
    {
        // Arrange  
        var service = new PageInspectorService(CreateConfiguration());
        
        // Act
        var result = await service.GetPageStructureAsync("http://invalid-url-that-definitely-does-not-exist-12345.com");
        
        // Assert
        Assert.NotNull(result);
        Assert.Contains("error", result.ToLower());
        Assert.StartsWith("[", result.Trim());
        Assert.EndsWith("]", result.Trim());
    }
}
