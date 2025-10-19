using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SuperQA.Core.Interfaces;
using SuperQA.Infrastructure.Data;

namespace SuperQA.Infrastructure.Services;

/// <summary>
/// AI-powered test healing service that analyzes failures and generates fixed scripts
/// </summary>
public class AITestHealingService : IAITestHealingService
{
    private readonly SuperQADbContext _context;
    private readonly HttpClient _httpClient;
    private const string OpenAIEndpoint = "https://api.openai.com/v1/chat/completions";

    public AITestHealingService(SuperQADbContext context, HttpClient httpClient)
    {
        _context = context;
        _httpClient = httpClient;
    }

    public async Task<string> HealTestScriptAsync(int testCaseId, int executionId, string apiKey, string model = "gpt-4")
    {
        // Get the test case and failed execution details
        var testCase = await _context.TestCases
            .FirstOrDefaultAsync(tc => tc.Id == testCaseId);

        if (testCase == null)
        {
            throw new ArgumentException($"Test case with ID {testCaseId} not found.");
        }

        var execution = await _context.TestExecutions
            .FirstOrDefaultAsync(e => e.Id == executionId);

        if (execution == null)
        {
            throw new ArgumentException($"Test execution with ID {executionId} not found.");
        }

        if (execution.Status != "Failed")
        {
            throw new InvalidOperationException("Can only heal failed test executions.");
        }

        // Get healing history to avoid overwriting previously corrected locators
        var healingHistory = await _context.HealingHistories
            .Where(h => h.TestCaseId == testCaseId && h.WasSuccessful)
            .OrderByDescending(h => h.HealedAt)
            .Take(10)
            .ToListAsync();

        // Build a comprehensive prompt for AI healing
        var prompt = BuildHealingPrompt(testCase, execution, healingHistory);

        // Call OpenAI to generate the healed script
        var healedScript = await CallOpenAIForHealingAsync(prompt, apiKey, model);

        // Store healing history
        var history = new Core.Entities.HealingHistory
        {
            TestCaseId = testCaseId,
            TestExecutionId = executionId,
            HealingType = "AI-Healing",
            OldScript = testCase.AutomationScript,
            NewScript = healedScript,
            WasSuccessful = true,
            HealedAt = DateTime.UtcNow
        };
        _context.HealingHistories.Add(history);
        await _context.SaveChangesAsync();

        return healedScript;
    }

    private string BuildHealingPrompt(Core.Entities.TestCase testCase, Core.Entities.TestExecution execution, List<Core.Entities.HealingHistory> healingHistory)
    {
        var prompt = new StringBuilder();
        
        prompt.AppendLine("You are an expert test automation engineer specializing in self-healing test scripts.");
        prompt.AppendLine("A test has failed and needs to be fixed. Analyze the failure and generate an improved, healed test script.");
        prompt.AppendLine();
        prompt.AppendLine("CRITICAL: This test has been healed before. You MUST preserve previously corrected locators and code.");
        prompt.AppendLine();
        
        // Include healing history to prevent overwriting previous fixes
        if (healingHistory.Any())
        {
            prompt.AppendLine("HEALING HISTORY (PREVIOUSLY CORRECTED - DO NOT OVERWRITE):");
            foreach (var history in healingHistory)
            {
                if (!string.IsNullOrWhiteSpace(history.OldLocator) && !string.IsNullOrWhiteSpace(history.NewLocator))
                {
                    prompt.AppendLine($"✓ {history.OldLocator} → {history.NewLocator} (Corrected on {history.HealedAt:yyyy-MM-dd})");
                }
            }
            prompt.AppendLine();
            prompt.AppendLine("IMPORTANT: Keep all these previously corrected locators in the healed script. Only modify code related to the current failure.");
            prompt.AppendLine();
        }
        
        prompt.AppendLine("ORIGINAL TEST CASE:");
        prompt.AppendLine($"Title: {testCase.Title}");
        prompt.AppendLine($"Description: {testCase.Description}");
        prompt.AppendLine();
        prompt.AppendLine("ORIGINAL TEST STEPS:");
        prompt.AppendLine(testCase.Steps);
        prompt.AppendLine();
        prompt.AppendLine("EXPECTED RESULTS:");
        prompt.AppendLine(testCase.ExpectedResults);
        prompt.AppendLine();
        
        if (!string.IsNullOrWhiteSpace(testCase.AutomationScript))
        {
            prompt.AppendLine("CURRENT AUTOMATION SCRIPT (may include previous fixes):");
            prompt.AppendLine(testCase.AutomationScript);
            prompt.AppendLine();
        }

        prompt.AppendLine("FAILURE INFORMATION:");
        prompt.AppendLine($"Error Message: {execution.ErrorMessage}");
        prompt.AppendLine();
        
        if (!string.IsNullOrWhiteSpace(execution.StackTrace))
        {
            prompt.AppendLine("Stack Trace:");
            prompt.AppendLine(execution.StackTrace);
            prompt.AppendLine();
        }

        prompt.AppendLine("HEALING REQUIREMENTS:");
        prompt.AppendLine("1. Analyze the failure reason carefully");
        prompt.AppendLine("2. Identify potential issues:");
        prompt.AppendLine("   - Selector problems (element not found, changed selectors)");
        prompt.AppendLine("   - Timing issues (elements not ready, async operations)");
        prompt.AppendLine("   - Navigation issues (page not loaded, redirects)");
        prompt.AppendLine("   - Data issues (incorrect test data, validation failures)");
        prompt.AppendLine("3. Generate improved test steps with:");
        prompt.AppendLine("   - More robust selectors (prefer role+name, data-testid, IDs)");
        prompt.AppendLine("   - Proper wait strategies (explicit waits, element visibility checks)");
        prompt.AppendLine("   - Better error handling");
        prompt.AppendLine("   - Retry mechanisms where appropriate");
        prompt.AppendLine("4. PRESERVE all previously corrected locators from healing history");
        prompt.AppendLine("5. Make INCREMENTAL changes - only fix what's actually broken");
        prompt.AppendLine("6. If an automation script exists, generate a COMPLETE, RUNNABLE healed Playwright C# script");
        prompt.AppendLine("7. If no automation script exists, provide healed test steps in clear, actionable format");
        prompt.AppendLine();
        prompt.AppendLine("HEALING OUTPUT FORMAT:");
        prompt.AppendLine("Provide ONLY the healed test steps or script, no explanations or markdown fences.");
        prompt.AppendLine("Make the test more resilient and likely to pass.");
        prompt.AppendLine("CRITICAL: Do not change or overwrite previously corrected locators unless they are directly causing this specific failure.");

        return prompt.ToString();
    }

    private async Task<string> CallOpenAIForHealingAsync(string prompt, string apiKey, string model)
    {
        var requestBody = new
        {
            model = model,
            messages = new[]
            {
                new 
                { 
                    role = "system", 
                    content = "You are an expert test automation engineer with deep knowledge of self-healing test strategies. You analyze test failures and generate improved, resilient test scripts that are more likely to succeed. You understand Playwright, Selenium, and modern test automation best practices. CRITICAL: You ALWAYS preserve previously corrected locators and code - you only make incremental changes to fix the specific failure. You never overwrite working code or previously healed locators unless they are directly causing the current failure."
                },
                new { role = "user", content = prompt }
            },
            temperature = 0.3, // Lower temperature for more focused, deterministic healing
            max_tokens = 3000
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var response = await _httpClient.PostAsync(OpenAIEndpoint, content);
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            var errorMessage = response.StatusCode switch
            {
                System.Net.HttpStatusCode.TooManyRequests =>
                    "Rate limit exceeded. Please wait and try again.",
                System.Net.HttpStatusCode.Unauthorized =>
                    "Invalid API key. Please check your OpenAI API key.",
                System.Net.HttpStatusCode.PaymentRequired =>
                    "Payment required. Your OpenAI account has insufficient credits.",
                System.Net.HttpStatusCode.InternalServerError =>
                    "OpenAI service error. Please try again later.",
                System.Net.HttpStatusCode.ServiceUnavailable =>
                    "OpenAI service unavailable. Please try again later.",
                _ =>
                    $"OpenAI API error ({response.StatusCode}): {errorContent}"
            };
            throw new HttpRequestException(errorMessage);
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        var jsonResponse = JsonDocument.Parse(responseContent);

        var healedScript = jsonResponse.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;

        // Clean up any markdown formatting
        healedScript = healedScript.Trim();
        if (healedScript.StartsWith("```csharp"))
            healedScript = healedScript.Substring("```csharp".Length);
        else if (healedScript.StartsWith("```"))
            healedScript = healedScript.Substring("```".Length);
        if (healedScript.EndsWith("```"))
            healedScript = healedScript[..^3];

        return healedScript.Trim();
    }
}
