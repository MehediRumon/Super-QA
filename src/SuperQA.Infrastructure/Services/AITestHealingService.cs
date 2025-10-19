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
    private readonly ILocatorValidationService _validationService;
    private const string OpenAIEndpoint = "https://api.openai.com/v1/chat/completions";

    public AITestHealingService(
        SuperQADbContext context, 
        HttpClient httpClient,
        ILocatorValidationService validationService)
    {
        _context = context;
        _httpClient = httpClient;
        _validationService = validationService;
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

        // Validate the healed script to ensure it preserves previously corrected locators
        var validationResult = ValidateHealedScript(testCase, execution, healedScript, healingHistory);
        
        if (!validationResult.IsValid)
        {
            // Log failed healing attempt
            var failedHistory = new Core.Entities.HealingHistory
            {
                TestCaseId = testCaseId,
                TestExecutionId = executionId,
                HealingType = "AI-Healing",
                OldScript = testCase.AutomationScript,
                NewScript = healedScript,
                WasSuccessful = false,
                ErrorMessage = validationResult.ErrorMessage,
                HealedAt = DateTime.UtcNow
            };
            _context.HealingHistories.Add(failedHistory);
            await _context.SaveChangesAsync();
            
            throw new InvalidOperationException(
                $"AI healing validation failed: {validationResult.ErrorMessage}. " +
                "The healed script does not preserve previously corrected locators or contains invalid locators. " +
                "Please review the test manually or try healing again with a different model.");
        }

        // Store successful healing history
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
        prompt.AppendLine("⚠️  CRITICAL RULES - VIOLATION WILL CAUSE REJECTION:");
        prompt.AppendLine("1. PRESERVE ALL previously corrected locators (shown below) - DO NOT modify, remove, or replace them");
        prompt.AppendLine("2. Make ONLY INCREMENTAL changes - fix what's broken, keep what works");
        prompt.AppendLine("3. Use SPECIFIC locators - never use generic ones like 'button', 'div', 'input' alone");
        prompt.AppendLine("4. Ensure element TYPE compatibility - buttons to buttons, inputs to inputs");
        prompt.AppendLine();
        
        // Include healing history to prevent overwriting previous fixes
        if (healingHistory.Any())
        {
            prompt.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            prompt.AppendLine("🔒 PROTECTED LOCATORS - KEEP EXACTLY AS-IS:");
            prompt.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            
            var protectedLocators = new List<string>();
            foreach (var history in healingHistory)
            {
                if (!string.IsNullOrWhiteSpace(history.OldLocator) && !string.IsNullOrWhiteSpace(history.NewLocator))
                {
                    prompt.AppendLine($"✓ PROTECTED: {history.NewLocator}");
                    prompt.AppendLine($"  (Previously fixed: {history.OldLocator} → {history.NewLocator} on {history.HealedAt:yyyy-MM-dd})");
                    protectedLocators.Add(history.NewLocator);
                }
            }
            
            if (protectedLocators.Any())
            {
                prompt.AppendLine();
                prompt.AppendLine($"📌 You MUST keep these {protectedLocators.Count} locator(s) unchanged in your healed script:");
                foreach (var loc in protectedLocators)
                {
                    prompt.AppendLine($"   - {loc}");
                }
            }
            
            prompt.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
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

        prompt.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        prompt.AppendLine("❌ FAILURE INFORMATION:");
        prompt.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        prompt.AppendLine($"Error Message: {execution.ErrorMessage}");
        prompt.AppendLine();
        
        if (!string.IsNullOrWhiteSpace(execution.StackTrace))
        {
            prompt.AppendLine("Stack Trace:");
            prompt.AppendLine(execution.StackTrace);
            prompt.AppendLine();
        }

        prompt.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        prompt.AppendLine("🔧 HEALING REQUIREMENTS:");
        prompt.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        prompt.AppendLine("1. Analyze the failure reason carefully");
        prompt.AppendLine("2. Identify the SPECIFIC issue causing this failure:");
        prompt.AppendLine("   - Selector problems (element not found, changed selectors)");
        prompt.AppendLine("   - Timing issues (elements not ready, async operations)");
        prompt.AppendLine("   - Navigation issues (page not loaded, redirects)");
        prompt.AppendLine("   - Data issues (incorrect test data, validation failures)");
        prompt.AppendLine("   - Element state issues (readonly, disabled, hidden elements)");
        prompt.AppendLine("3. Fix ONLY the broken part with:");
        prompt.AppendLine("   - For FAILED locators: use more robust selectors (prefer role+name, data-testid, IDs)");
        prompt.AppendLine("   - For WORKING locators: leave them unchanged, even if they use XPath or old patterns");
        prompt.AppendLine("   - NEVER use bare tag names (button, div, input, span, a)");
        prompt.AppendLine("   - Proper wait strategies (explicit waits, element visibility checks)");
        prompt.AppendLine("   - Better error handling and retry mechanisms");
        prompt.AppendLine("   - For readonly/disabled elements: add WaitForAsync() before interaction");
        prompt.AppendLine("   - Add WaitForLoadStateAsync(LoadState.NetworkIdle) after navigation for stability");
        prompt.AppendLine("4. PRESERVE all previously corrected locators from healing history");
        prompt.AppendLine("5. Make INCREMENTAL changes - change as few lines as possible");
        prompt.AppendLine("6. Ensure element type compatibility (don't replace button locators with input locators)");
        prompt.AppendLine("7. If an automation script exists, generate a COMPLETE, RUNNABLE healed Playwright C# script");
        prompt.AppendLine("8. If no automation script exists, provide healed test steps in clear, actionable format");
        prompt.AppendLine();
        prompt.AppendLine("🎯 TARGETED HEALING APPROACH:");
        prompt.AppendLine("- Look at the error message and stack trace to identify the EXACT locator that failed");
        prompt.AppendLine("- Change ONLY that specific failing locator or add necessary wait/retry logic");
        prompt.AppendLine("- Keep ALL other locators and code exactly as-is, EVEN IF they use XPath or older patterns");
        prompt.AppendLine("- Do NOT rewrite working parts of the test");
        prompt.AppendLine("- Do NOT change locators that are not mentioned in the error");
        prompt.AppendLine("- Do NOT 'improve' or 'modernize' working locators - if it works, leave it alone");
        prompt.AppendLine("- Changing more than 1-2 locators is almost always wrong unless explicitly needed");
        prompt.AppendLine();
        prompt.AppendLine("🔒 SPECIAL CASE - READONLY/DISABLED ELEMENTS:");
        prompt.AppendLine("- If error mentions 'element is not editable', 'readonly', or 'disabled':");
        prompt.AppendLine("  1. Use Locator() to get the element reference");
        prompt.AppendLine("  2. Add WaitForAsync(new() { State = WaitForSelectorState.Visible }) before interaction");
        prompt.AppendLine("  3. The element might need JavaScript evaluation to enable it, or alternative interaction");
        prompt.AppendLine("  4. Consider using EvaluateAsync() to modify readonly/disabled attributes if needed");
        prompt.AppendLine("  5. For date inputs, use proper date format (yyyy-MM-dd) and ensure element is ready");
        prompt.AppendLine("- Example pattern for readonly inputs:");
        prompt.AppendLine("  var element = Page.Locator(\"#elementId\");");
        prompt.AppendLine("  await element.WaitForAsync(new() { State = WaitForSelectorState.Visible });");
        prompt.AppendLine("  await element.FillAsync(\"value\");");
        prompt.AppendLine();
        prompt.AppendLine("HEALING OUTPUT FORMAT:");
        prompt.AppendLine("Provide ONLY the healed test steps or script, no explanations or markdown fences.");
        prompt.AppendLine("Make the test more resilient and likely to pass.");
        prompt.AppendLine();
        prompt.AppendLine("⚠️  FINAL WARNING:");
        prompt.AppendLine("If you modify any protected locator or use generic selectors, your response will be REJECTED.");
        prompt.AppendLine("If you change more than necessary (over-heal), your response will be REJECTED.");
        prompt.AppendLine("If you change working locators that are not mentioned in the error, your response will be REJECTED.");
        prompt.AppendLine("Focus on fixing ONLY what's broken while keeping everything else intact.");

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
                    content = "You are an expert test automation engineer with deep knowledge of self-healing test strategies. You analyze test failures and generate improved, resilient test scripts. CRITICAL CONSTRAINTS: (1) You ALWAYS preserve previously corrected locators - you NEVER overwrite working code. (2) You make ONLY incremental changes to fix the specific failure - typically changing just 1-2 lines. (3) You NEVER use generic locators like 'button', 'div', 'input' alone - always use specific selectors. (4) You ensure element type compatibility - buttons remain buttons, inputs remain inputs. (5) If asked to preserve specific locators, you keep them EXACTLY as-is without any modifications. (6) You identify the EXACT failing locator from the error message and change ONLY that locator. (7) You do NOT rewrite or refactor working parts of the test. (8) You do NOT change locators that are not mentioned in the error message - even if they use XPath or old patterns, if they work, leave them alone. (9) For readonly or disabled elements, you add proper waits with WaitForAsync() and consider using JavaScript evaluation if needed. (10) You add WaitForLoadStateAsync(LoadState.NetworkIdle) after navigation for better stability. (11) You NEVER 'modernize' or 'improve' working locators that are not failing. Violating these rules will result in your response being rejected. You understand Playwright, Selenium, and modern test automation best practices. Remember: surgical precision is key - fix only what's broken, leave everything else untouched."
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

    /// <summary>
    /// Validates that the healed script preserves previously corrected locators and doesn't introduce mismatched elements
    /// </summary>
    private (bool IsValid, string ErrorMessage) ValidateHealedScript(
        Core.Entities.TestCase testCase,
        Core.Entities.TestExecution execution,
        string healedScript,
        List<Core.Entities.HealingHistory> healingHistory)
    {
        // Validation 1: Check that previously corrected locators are preserved
        if (healingHistory.Any())
        {
            foreach (var history in healingHistory.Where(h => !string.IsNullOrWhiteSpace(h.NewLocator)))
            {
                // The new locator from previous healing should be present in the healed script
                // (unless it was the locator that failed this time)
                if (!string.IsNullOrWhiteSpace(history.NewLocator) && 
                    !healedScript.Contains(history.NewLocator))
                {
                    // Check if this was the locator that failed (mentioned in error)
                    bool isFailedLocator = !string.IsNullOrWhiteSpace(execution.ErrorMessage) && 
                                          execution.ErrorMessage.Contains(history.NewLocator);
                    
                    if (!isFailedLocator)
                    {
                        return (false, 
                            $"Previously corrected locator '{history.NewLocator}' was removed from the script. " +
                            $"This locator was successfully healed on {history.HealedAt:yyyy-MM-dd} and should be preserved.");
                    }
                }
            }
        }

        // Validation 2: Check for overly generic locators that might match multiple elements
        var genericPatterns = new[] { 
            ("\"button\"", "button"), ("'button'", "button"),
            ("\"div\"", "div"), ("'div'", "div"),
            ("\"span\"", "span"), ("'span'", "span"),
            ("\"input\"", "input"), ("'input'", "input"),
            ("\"a\"", "a"), ("'a'", "a")
        };

        foreach (var (pattern, name) in genericPatterns)
        {
            if (healedScript.Contains($"ClickAsync({pattern})") ||
                healedScript.Contains($"FillAsync({pattern}") ||
                healedScript.Contains($"Locator({pattern})"))
            {
                return (false, 
                    $"Healed script contains overly generic locator {pattern} that may match multiple elements. " +
                    "Please use more specific locators like data-testid, IDs, or role+name combinations.");
            }
        }

        // Validation 3: Check that the script doesn't have mismatched element types
        // Extract error message element type and ensure healed locators are compatible
        if (!string.IsNullOrWhiteSpace(execution.ErrorMessage))
        {
            // Look for patterns like "Button element not found" or "Input field missing"
            var errorElementType = ExtractElementTypeFromError(execution.ErrorMessage);
            
            if (!string.IsNullOrEmpty(errorElementType))
            {
                // Extract potential new locators from the healed script
                var newLocators = ExtractLocatorsFromScript(healedScript);
                
                foreach (var locator in newLocators)
                {
                    // Check if this locator might target a different element type
                    if (_validationService.HasMismatchPatterns(locator, execution.ErrorMessage))
                    {
                        return (false,
                            $"Healed script contains locator '{locator}' that may target an incompatible element type. " +
                            $"The error indicates a '{errorElementType}' element, but the locator suggests a different type.");
                    }
                }
            }
        }
        
        // Validation 4: Compare old and new scripts to detect inappropriate wholesale changes
        if (!string.IsNullOrWhiteSpace(testCase.AutomationScript))
        {
            var oldLocators = ExtractLocatorsFromScript(testCase.AutomationScript);
            var newLocators = ExtractLocatorsFromScript(healedScript);
            
            // Check if too many locators were changed (possible sign of over-healing)
            var changedLocators = oldLocators.Except(newLocators).ToList();
            var protectedLocators = healingHistory
                .Where(h => !string.IsNullOrWhiteSpace(h.NewLocator))
                .Select(h => h.NewLocator!)
                .ToList();
            
            // Count how many unprotected locators were changed
            var unprotectedChanges = changedLocators
                .Where(loc => !protectedLocators.Contains(loc))
                .Count();
            
            // If more than 50% of unprotected locators changed, it's suspicious
            var totalUnprotectedLocators = oldLocators
                .Where(loc => !protectedLocators.Contains(loc))
                .Count();
            
            if (totalUnprotectedLocators > 0 && 
                unprotectedChanges > totalUnprotectedLocators * 0.5 &&
                unprotectedChanges > 2) // Allow small changes
            {
                return (false,
                    $"Healed script changed too many locators ({unprotectedChanges} out of {totalUnprotectedLocators}). " +
                    "AI healing should make incremental changes, not rewrite the entire test. " +
                    "This may indicate the AI is selecting wrong elements or making inappropriate changes.");
            }
        }

        // Validation 5: Ensure the script is not empty or malformed
        if (string.IsNullOrWhiteSpace(healedScript))
        {
            return (false, "Healed script is empty.");
        }

        if (healedScript.Length < 20) // Arbitrary minimum length for a valid script
        {
            return (false, "Healed script appears to be incomplete or malformed.");
        }

        // All validations passed
        return (true, string.Empty);
    }

    private string ExtractElementTypeFromError(string errorMessage)
    {
        var lowerError = errorMessage.ToLower();
        
        // Check in order of specificity to avoid false matches
        if (lowerError.Contains("checkbox"))
        {
            return "checkbox";
        }
        if (lowerError.Contains("radio"))
        {
            return "radio";
        }
        if (lowerError.Contains("textarea"))
        {
            return "textarea";
        }
        if (lowerError.Contains("select") || lowerError.Contains("dropdown"))
        {
            return "select";
        }
        if (lowerError.Contains("button") || lowerError.Contains("btn"))
        {
            return "button";
        }
        if (lowerError.Contains("input") || lowerError.Contains("textbox") || lowerError.Contains("field"))
        {
            return "input";
        }
        if (lowerError.Contains("link") || lowerError.Contains("anchor"))
        {
            return "link";
        }
        if (lowerError.Contains("image") || lowerError.Contains("img"))
        {
            return "image";
        }
        if (lowerError.Contains("heading") || lowerError.Contains("header"))
        {
            return "heading";
        }

        return string.Empty;
    }

    private List<string> ExtractLocatorsFromScript(string script)
    {
        var locators = new List<string>();
        
        // Extract CSS selectors and Playwright locators from common methods
        var patterns = new[]
        {
            // Basic selector methods
            @"ClickAsync\([""']([^""']+)[""']",
            @"FillAsync\([""']([^""']+)[""']",
            @"Locator\([""']([^""']+)[""']",
            @"WaitForSelectorAsync\([""']([^""']+)[""']",
            @"QuerySelectorAsync\([""']([^""']+)[""']",
            @"IsVisibleAsync\([""']([^""']+)[""']",
            @"IsEnabledAsync\([""']([^""']+)[""']",
            @"GetAttributeAsync\([""']([^""']+)[""']",
            
            // GetBy* methods - extract the search text/name
            @"GetByRole\([^,]+,\s*new\(\)\s*\{\s*Name\s*=\s*[""']([^""']+)[""']",
            @"GetByText\([""']([^""']+)[""']",
            @"GetByLabel\([""']([^""']+)[""']",
            @"GetByPlaceholder\([""']([^""']+)[""']",
            @"GetByAltText\([""']([^""']+)[""']",
            @"GetByTitle\([""']([^""']+)[""']",
            @"GetByTestId\([""']([^""']+)[""']",
        };

        foreach (var pattern in patterns)
        {
            var matches = System.Text.RegularExpressions.Regex.Matches(script, pattern);
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                if (match.Groups.Count > 1)
                {
                    locators.Add(match.Groups[1].Value);
                }
            }
        }

        return locators.Distinct().ToList();
    }
}
