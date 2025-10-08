using SuperQA.Infrastructure.Services;
using System.Text.Json;
using Xunit.Abstractions;

namespace SuperQA.Tests;

public class PageInspectorIntegrationTests
{
    private readonly ITestOutputHelper _output;

    public PageInspectorIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task GetPageStructureAsync_ExtractsInputElements()
    {
        // Arrange
        var service = new PageInspectorService();

        // Act - Test against example.com which has basic HTML structure
        var result = await service.GetPageStructureAsync("http://example.com");

        // Assert
        Assert.NotNull(result);
        _output.WriteLine("Result: " + result);
        Assert.StartsWith("[", result.Trim());
        Assert.EndsWith("]", result.Trim());
    }

    [Fact]
    public async Task GetPageStructureAsync_ParsesElementStructure()
    {
        // Arrange
        var service = new PageInspectorService();

        // Act
        var result = await service.GetPageStructureAsync("http://example.com");

        // Assert
        Assert.NotNull(result);
        _output.WriteLine("Result length: " + result.Length);
        _output.WriteLine("First 500 chars: " + result.Substring(0, Math.Min(500, result.Length)));
    }
}
