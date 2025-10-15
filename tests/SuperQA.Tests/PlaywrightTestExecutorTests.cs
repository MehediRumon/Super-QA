using Microsoft.Extensions.Configuration;
using SuperQA.Infrastructure.Services;
using Xunit;

namespace SuperQA.Tests;

public class PlaywrightTestExecutorTests
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
    public async Task ExecuteTestScriptAsync_ShouldNotThrow_WhenPwshNotAvailable()
    {
        // Arrange
        var executor = new PlaywrightTestExecutor(CreateConfiguration());
        var testScript = @"
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace PlaywrightTests;

[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class Tests : PageTest
{
    [Test]
    public async Task BasicTest()
    {
        await Page.GotoAsync(""https://www.example.com"");
        await Expect(Page).ToHaveTitleAsync(new System.Text.RegularExpressions.Regex(""Example""));
    }
}";

        // Act & Assert
        // This should not throw an error about pwsh not being found
        var result = await executor.ExecuteTestScriptAsync(testScript, "https://www.example.com");
        
        // The result may fail for other reasons (network, browser not installed, etc.)
        // but it should NOT fail with "cannot find pwsh" error
        Assert.NotNull(result);
        Assert.NotNull(result.Logs);
        
        // Check that logs don't contain the specific pwsh error that was occurring before
        var allLogs = string.Join("\n", result.Logs);
        Assert.DoesNotContain("cannot find the file specified", allLogs, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("The system cannot find the file specified", allLogs, StringComparison.OrdinalIgnoreCase);
        
        // Verify that browser installation was attempted
        Assert.Contains("Installing Playwright browsers", allLogs);
    }

    [Fact]
    public async Task ExecuteTestScriptAsync_ShouldReturnBuildError_WhenScriptHasSyntaxErrors()
    {
        // Arrange
        var executor = new PlaywrightTestExecutor(CreateConfiguration());
        var testScriptWithSyntaxError = @"
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace PlaywrightTests;

[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class Tests : PageTest
{
    [Test]
    public async Task BasicTest()
    {
        await Page.GotoAsync(""https://www.example.com"");
        await Expect(Page).ToHaveTitleAsync(new System.Text.RegularExpressions.Regex(""Example""));
    // Missing closing brace here
}";

        // Act
        var result = await executor.ExecuteTestScriptAsync(testScriptWithSyntaxError, "https://www.example.com");
        
        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Equal("Build Error", result.Status);
        Assert.Contains("syntax errors", result.ErrorMessage);
        Assert.Contains("failed to compile", result.ErrorMessage);
        
        // Verify that build output is in the response
        Assert.NotNull(result.Output);
        Assert.Contains("error CS", result.Output);
        
        // Verify that browser installation and test execution were NOT attempted
        var allLogs = string.Join("\n", result.Logs);
        Assert.DoesNotContain("Executing tests", allLogs);
    }
}
