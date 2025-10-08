using SuperQA.Infrastructure.Services;

namespace SuperQA.Tests;

public class PageInspectorServiceTests
{
    [Fact]
    public async Task GetPageStructureAsync_WithInvalidUrl_ReturnsErrorStructure()
    {
        // Arrange
        var service = new PageInspectorService();

        // Act
        var result = await service.GetPageStructureAsync("http://invalid-url-that-does-not-exist-12345.com");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("error", result.ToLower());
    }

    [Fact]
    public async Task GetPageStructureAsync_WithValidUrl_ReturnsJsonStructure()
    {
        // Arrange
        var service = new PageInspectorService();

        // Act - Using example.com which is always available
        var result = await service.GetPageStructureAsync("http://example.com");

        // Assert
        Assert.NotNull(result);
        // Should return JSON array format
        Assert.StartsWith("[", result.Trim());
        Assert.EndsWith("]", result.Trim());
    }
}
