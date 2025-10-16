using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Playwright;
using SuperQA.Core.Entities;
using SuperQA.Core.Interfaces;
using SuperQA.Infrastructure.Data;

namespace SuperQA.Infrastructure.Services;

public class TestExecutionService : ITestExecutionService
{
    private readonly SuperQADbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ISelfHealingService? _selfHealingService;

    public TestExecutionService(
        SuperQADbContext context, 
        IConfiguration configuration,
        ISelfHealingService? selfHealingService = null)
    {
        _context = context;
        _configuration = configuration;
        _selfHealingService = selfHealingService;
    }

    public async Task<int> ExecuteTestAsync(int testCaseId, string? baseUrl = null)
    {
        var testCase = await _context.TestCases
            .Include(tc => tc.Project)
            .FirstOrDefaultAsync(tc => tc.Id == testCaseId);

        if (testCase == null)
        {
            throw new ArgumentException($"Test case with ID {testCaseId} not found.");
        }

        var execution = new TestExecution
        {
            TestCaseId = testCaseId,
            ProjectId = testCase.ProjectId,
            ExecutedAt = DateTime.UtcNow,
            Status = "Running"
        };

        _context.TestExecutions.Add(execution);
        await _context.SaveChangesAsync();

        var startTime = DateTime.UtcNow;

        try
        {
            // Execute test with Playwright
            await ExecuteWithPlaywrightAsync(testCase, execution, baseUrl);

            execution.Status = "Passed";
        }
        catch (Exception ex)
        {
            execution.Status = "Failed";
            execution.ErrorMessage = ex.Message;
            execution.StackTrace = ex.StackTrace;

            // Capture screenshot on failure
            await CaptureScreenshotAsync(execution);
        }
        finally
        {
            var endTime = DateTime.UtcNow;
            execution.DurationMs = (int)(endTime - startTime).TotalMilliseconds;
            await _context.SaveChangesAsync();
        }

        return execution.Id;
    }

    private async Task ExecuteWithPlaywrightAsync(TestCase testCase, TestExecution execution, string? baseUrl)
    {
        // Install Playwright browsers if not already installed
        // This is a simplified implementation - in production, browsers should be pre-installed
        var exitCode = Microsoft.Playwright.Program.Main(new[] { "install", "chromium" });
        if (exitCode != 0)
        {
            throw new Exception("Failed to install Playwright browsers");
        }

        using var playwright = await Playwright.CreateAsync();
        var headless = _configuration.GetValue<bool>("Playwright:Headless", true);
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = headless
        });

        var context = await browser.NewContextAsync();
        var page = await context.NewPageAsync();

        try
        {
            // Parse and execute test steps
            var steps = testCase.Steps.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var step in steps)
            {
                var trimmedStep = step.Trim();
                
                // Simple step parser - can be enhanced with more sophisticated parsing
                if (trimmedStep.StartsWith("Navigate to", StringComparison.OrdinalIgnoreCase))
                {
                    var url = baseUrl ?? ExtractUrl(trimmedStep);
                    await page.GotoAsync(url);
                }
                else if (trimmedStep.Contains("click", StringComparison.OrdinalIgnoreCase))
                {
                    var selector = ExtractSelector(trimmedStep);
                    await ClickWithRetryAsync(page, selector, testCase.Id);
                }
                else if (trimmedStep.Contains("type", StringComparison.OrdinalIgnoreCase) || 
                         trimmedStep.Contains("enter", StringComparison.OrdinalIgnoreCase))
                {
                    var selector = ExtractSelector(trimmedStep);
                    var text = ExtractText(trimmedStep);
                    await FillWithRetryAsync(page, selector, text, testCase.Id);
                }
                else if (trimmedStep.Contains("verify", StringComparison.OrdinalIgnoreCase) || 
                         trimmedStep.Contains("assert", StringComparison.OrdinalIgnoreCase))
                {
                    var expectedText = ExtractText(trimmedStep);
                    var content = await page.ContentAsync();
                    if (!content.Contains(expectedText, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new Exception($"Verification failed: Expected text '{expectedText}' not found");
                    }
                }
                
                // Wait a bit between steps
                await Task.Delay(500);
            }

            // Verify expected results if specified
            if (!string.IsNullOrWhiteSpace(testCase.ExpectedResults))
            {
                var content = await page.ContentAsync();
                if (!content.Contains(testCase.ExpectedResults, StringComparison.OrdinalIgnoreCase))
                {
                    throw new Exception($"Expected result not found: {testCase.ExpectedResults}");
                }
            }
        }
        catch
        {
            // Capture screenshot on any error
            var screenshotBytes = await page.ScreenshotAsync();
            execution.Screenshot = Convert.ToBase64String(screenshotBytes);
            throw;
        }
    }

    private async Task CaptureScreenshotAsync(TestExecution execution)
    {
        // Screenshot capture is handled in ExecuteWithPlaywrightAsync
        // This method can be used for additional screenshot logic if needed
        await Task.CompletedTask;
    }

    private string ExtractUrl(string step)
    {
        // Simple URL extraction - looks for http/https URLs
        var words = step.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var url = words.FirstOrDefault(w => w.StartsWith("http", StringComparison.OrdinalIgnoreCase));
        return url ?? "about:blank";
    }

    private string ExtractSelector(string step)
    {
        // Simple selector extraction - looks for quoted strings or common patterns
        var startQuote = step.IndexOf('"');
        if (startQuote >= 0)
        {
            var endQuote = step.IndexOf('"', startQuote + 1);
            if (endQuote > startQuote)
            {
                return step.Substring(startQuote + 1, endQuote - startQuote - 1);
            }
        }

        // Fallback: look for common selectors
        if (step.Contains("#"))
        {
            var parts = step.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            var selectorPart = parts.FirstOrDefault(p => p.StartsWith("#"));
            if (selectorPart != null) return selectorPart;
        }

        return "body"; // Default fallback
    }

    private string ExtractText(string step)
    {
        // Extract text from quotes
        var startQuote = step.IndexOf('"');
        if (startQuote >= 0)
        {
            var endQuote = step.IndexOf('"', startQuote + 1);
            if (endQuote > startQuote)
            {
                return step.Substring(startQuote + 1, endQuote - startQuote - 1);
            }
        }

        return string.Empty;
    }

    private async Task ClickWithRetryAsync(IPage page, string selector, int testCaseId)
    {
        try
        {
            await page.ClickAsync(selector, new() { Timeout = 5000 });
        }
        catch (PlaywrightException ex) when (ex.Message.Contains("not found") || ex.Message.Contains("not visible"))
        {
            // Try to heal the selector
            if (_selfHealingService != null)
            {
                var pageContent = await page.ContentAsync();
                var newSelector = await _selfHealingService.SuggestUpdatedLocatorAsync(selector, pageContent);
                
                if (newSelector != selector)
                {
                    // Try with the new selector
                    await page.ClickAsync(newSelector, new() { Timeout = 5000 });
                    
                    // Update the test case with the healed locator
                    await _selfHealingService.UpdateLocatorAsync(testCaseId, selector, newSelector);
                }
                else
                {
                    throw;
                }
            }
            else
            {
                throw;
            }
        }
    }

    private async Task FillWithRetryAsync(IPage page, string selector, string text, int testCaseId)
    {
        try
        {
            await page.FillAsync(selector, text, new() { Timeout = 5000 });
        }
        catch (PlaywrightException ex) when (ex.Message.Contains("not found") || ex.Message.Contains("not visible"))
        {
            // Try to heal the selector
            if (_selfHealingService != null)
            {
                var pageContent = await page.ContentAsync();
                var newSelector = await _selfHealingService.SuggestUpdatedLocatorAsync(selector, pageContent);
                
                if (newSelector != selector)
                {
                    // Try with the new selector
                    await page.FillAsync(newSelector, text, new() { Timeout = 5000 });
                    
                    // Update the test case with the healed locator
                    await _selfHealingService.UpdateLocatorAsync(testCaseId, selector, newSelector);
                }
                else
                {
                    throw;
                }
            }
            else
            {
                throw;
            }
        }
    }

    public async Task<IEnumerable<object>> GetTestExecutionsAsync(int projectId)
    {
        var executions = await _context.TestExecutions
            .Include(te => te.TestCase)
            .Where(te => te.ProjectId == projectId)
            .OrderByDescending(te => te.ExecutedAt)
            .Select(te => new
            {
                te.Id,
                te.TestCaseId,
                te.ProjectId,
                te.Status,
                te.ErrorMessage,
                te.StackTrace,
                te.Screenshot,
                te.ExecutedAt,
                te.DurationMs,
                TestCaseTitle = te.TestCase.Title
            })
            .ToListAsync();

        return executions;
    }

    public async Task<object?> GetTestExecutionAsync(int executionId)
    {
        var execution = await _context.TestExecutions
            .Include(te => te.TestCase)
            .Where(te => te.Id == executionId)
            .Select(te => new
            {
                te.Id,
                te.TestCaseId,
                te.ProjectId,
                te.Status,
                te.ErrorMessage,
                te.StackTrace,
                te.Screenshot,
                te.ExecutedAt,
                te.DurationMs,
                TestCaseTitle = te.TestCase.Title,
                TestCaseSteps = te.TestCase.Steps,
                TestCaseExpectedResults = te.TestCase.ExpectedResults
            })
            .FirstOrDefaultAsync();

        return execution;
    }

    public async Task<TestCase?> GetTestCaseAsync(int testCaseId)
    {
        return await _context.TestCases.FindAsync(testCaseId);
    }

    public async Task UpdateTestCaseAutomationScriptAsync(int testCaseId, string healedScript)
    {
        var testCase = await _context.TestCases.FindAsync(testCaseId);
        if (testCase == null)
        {
            throw new ArgumentException($"Test case with ID {testCaseId} not found.");
        }

        testCase.AutomationScript = healedScript;
        testCase.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }
}
