using SuperQA.Infrastructure.Services;
using Xunit;

namespace SuperQA.Tests;

public class PlaywrightTestExecutorTests
{
    [Fact]
    public async Task ExecuteTestScriptAsync_ShouldNotThrow_WhenPwshNotAvailable()
    {
        // Arrange
        var executor = new PlaywrightTestExecutor();
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
}
