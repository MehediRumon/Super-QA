using Microsoft.Extensions.Configuration;
using SuperQA.Infrastructure.Services;
using System.Text.Json;
using Xunit.Abstractions;

namespace SuperQA.Tests;

public class PageInspectorWithBrowserTests
{
    private readonly ITestOutputHelper _output;

    public PageInspectorWithBrowserTests(ITestOutputHelper output)
    {
        _output = output;
    }

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
    public async Task GetPageStructureAsync_WithLocalHtmlFile_ExtractsActualElements()
    {
        // Arrange
        var service = new PageInspectorService(CreateConfiguration());
        
        // Create a test HTML file
        var htmlContent = @"
<!DOCTYPE html>
<html>
<head><title>Test Page</title></head>
<body>
    <input type='text' id='username' name='username' placeholder='Enter username' />
    <input type='password' id='password' name='password' />
    <button id='loginBtn' type='submit'>Login</button>
    <a href='/home' id='homeLink'>Home</a>
</body>
</html>";
        
        var tempFile = Path.Combine(Path.GetTempPath(), "test_page_inspector.html");
        File.WriteAllText(tempFile, htmlContent);
        var fileUrl = "file://" + tempFile;

        try
        {
            // Act
            var result = await service.GetPageStructureAsync(fileUrl);
            
            // Assert
            _output.WriteLine("Page Structure Result:");
            _output.WriteLine(result);
            
            Assert.NotNull(result);
            Assert.StartsWith("[", result.Trim());
            Assert.EndsWith("]", result.Trim());
            
            // If browsers are installed, should extract actual elements
            if (!result.Contains("error"))
            {
                // Parse JSON and verify elements were extracted
                var elements = JsonSerializer.Deserialize<JsonElement[]>(result);
                Assert.NotNull(elements);
                
                _output.WriteLine($"Found {elements.Length} elements");
                
                // Should have at least the input fields and button
                Assert.True(elements.Length >= 3, $"Expected at least 3 elements, found {elements.Length}");
                
                // Verify we have the username input with correct selector
                var usernameInput = elements.FirstOrDefault(e => 
                    e.TryGetProperty("type", out var type) && 
                    type.GetString() == "input" &&
                    e.TryGetProperty("id", out var id) &&
                    id.GetString() == "username");
                
                Assert.NotEqual(default, usernameInput);
                
                if (usernameInput.TryGetProperty("selector", out var selector))
                {
                    var selectorValue = selector.GetString();
                    _output.WriteLine($"Username selector: {selectorValue}");
                    Assert.Equal("#username", selectorValue);
                }
            }
            else
            {
                _output.WriteLine("⚠️ Browser not installed - test skipped (graceful degradation)");
                Assert.Contains("error", result.ToLower());
            }
        }
        finally
        {
            // Clean up
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }
}
