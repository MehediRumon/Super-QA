using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SuperQA.Core.Interfaces;
using SuperQA.Infrastructure.Data;

namespace SuperQA.Infrastructure.Services;

/// <summary>
/// Service for automatic self-healing of test locators when elements are not found
/// </summary>
public class SelfHealingService : ISelfHealingService
{
    private readonly SuperQADbContext _context;
    private readonly HttpClient _httpClient;
    private const string OpenAIEndpoint = "https://api.openai.com/v1/chat/completions";

    public SelfHealingService(SuperQADbContext context, HttpClient httpClient)
    {
        _context = context;
        _httpClient = httpClient;
    }

    public async Task<string> SuggestUpdatedLocatorAsync(string failedLocator, string htmlStructure)
    {
        // First, try to extract alternative locators from the HTML structure
        if (!string.IsNullOrWhiteSpace(htmlStructure))
        {
            var alternatives = ExtractAlternativeLocators(failedLocator, htmlStructure);
            if (alternatives.Any())
            {
                // Return the most stable alternative (prefer id > data-testid > class > tag)
                return alternatives.First();
            }
        }

        // If no HTML structure provided or no alternatives found, return a fallback locator
        return ConvertToFallbackLocator(failedLocator);
    }

    public async Task<bool> UpdateLocatorAsync(int testCaseId, string oldLocator, string newLocator)
    {
        var testCase = await _context.TestCases.FindAsync(testCaseId);
        if (testCase == null)
        {
            return false;
        }

        // Store old values for history tracking
        var oldSteps = testCase.Steps;
        var oldScript = testCase.AutomationScript;

        // Update the test steps with the new locator using precise matching
        if (!string.IsNullOrWhiteSpace(testCase.Steps))
        {
            testCase.Steps = ReplaceLocatorPrecisely(testCase.Steps, oldLocator, newLocator);
        }

        // Update the automation script with the new locator using precise matching
        if (!string.IsNullOrWhiteSpace(testCase.AutomationScript))
        {
            testCase.AutomationScript = ReplaceLocatorPrecisely(testCase.AutomationScript, oldLocator, newLocator);
        }

        testCase.UpdatedAt = DateTime.UtcNow;

        // Track healing history
        var healingHistory = new Core.Entities.HealingHistory
        {
            TestCaseId = testCaseId,
            HealingType = "Self-Healing",
            OldLocator = oldLocator,
            NewLocator = newLocator,
            OldScript = oldScript,
            NewScript = testCase.AutomationScript,
            WasSuccessful = true,
            HealedAt = DateTime.UtcNow
        };
        _context.HealingHistories.Add(healingHistory);

        await _context.SaveChangesAsync();

        return true;
    }

    private string ReplaceLocatorPrecisely(string text, string oldLocator, string newLocator)
    {
        // Use regex to replace only exact matches of the locator
        // This ensures we don't replace partial matches like #btn in #btn-submit
        
        // Escape special regex characters in the locator
        string escapedOldLocator = System.Text.RegularExpressions.Regex.Escape(oldLocator);
        
        // Match the locator when it's:
        // - At the start/end of the string
        // - Followed/preceded by whitespace, quotes, parentheses, or other non-alphanumeric characters
        // This prevents partial replacements like #user -> #username
        
        string pattern = $@"(?<=[^\w-]|^){escapedOldLocator}(?=[^\w-]|$)";
        
        string result = System.Text.RegularExpressions.Regex.Replace(
            text, 
            pattern, 
            newLocator,
            System.Text.RegularExpressions.RegexOptions.None
        );
        
        return result;
    }

    private string ConvertToFallbackLocator(string failedLocator)
    {
        // Convert specific selectors to more generic but stable fallback locators
        
        if (failedLocator.StartsWith("#"))
        {
            // If ID selector failed, try data-testid as a more stable alternative
            var elementId = failedLocator.Substring(1);
            return $"[data-testid='{elementId}']";
        }
        else if (failedLocator.StartsWith("."))
        {
            // If class selector failed, try partial match to handle dynamic classes
            var className = failedLocator.Substring(1);
            return $"[class*='{className}']";
        }
        else if (failedLocator.Contains("[data-testid="))
        {
            // If data-testid failed, try to find by ID
            var testIdMatch = System.Text.RegularExpressions.Regex.Match(
                failedLocator, 
                @"data-testid='([^']+)'");
            if (testIdMatch.Success)
            {
                var testId = testIdMatch.Groups[1].Value;
                return $"#{testId}";
            }
        }
        
        // Default: return the original locator
        return failedLocator;
    }

    private List<string> ExtractAlternativeLocators(string failedLocator, string htmlStructure)
    {
        var alternatives = new List<string>();
        
        // Extract element name/id from the failed locator
        string elementIdentifier = "";
        if (failedLocator.StartsWith("#"))
        {
            elementIdentifier = failedLocator.Substring(1);
        }
        else if (failedLocator.StartsWith("."))
        {
            elementIdentifier = failedLocator.Substring(1);
        }
        else if (failedLocator.Contains("["))
        {
            // Extract value from attribute selector
            var match = System.Text.RegularExpressions.Regex.Match(
                failedLocator, 
                @"='([^']+)'");
            if (match.Success)
            {
                elementIdentifier = match.Groups[1].Value;
            }
        }

        if (string.IsNullOrWhiteSpace(elementIdentifier))
        {
            return alternatives;
        }

        // Priority order: id > data-testid > name > class > aria-label
        
        // Try to find ID in HTML
        if (htmlStructure.Contains($"id=\"{elementIdentifier}\"") || 
            htmlStructure.Contains($"id='{elementIdentifier}'"))
        {
            alternatives.Add($"#{elementIdentifier}");
        }

        // Try to find data-testid in HTML
        if (htmlStructure.Contains($"data-testid=\"{elementIdentifier}\"") || 
            htmlStructure.Contains($"data-testid='{elementIdentifier}'"))
        {
            alternatives.Add($"[data-testid='{elementIdentifier}']");
        }

        // Try to find name attribute in HTML
        if (htmlStructure.Contains($"name=\"{elementIdentifier}\"") || 
            htmlStructure.Contains($"name='{elementIdentifier}'"))
        {
            alternatives.Add($"[name='{elementIdentifier}']");
        }

        // Try to find by class
        if (htmlStructure.Contains($"class=\"{elementIdentifier}\"") || 
            htmlStructure.Contains($"class='{elementIdentifier}'") ||
            htmlStructure.Contains($"class=\"{elementIdentifier} ") ||
            htmlStructure.Contains($"class='{elementIdentifier} ") ||
            htmlStructure.Contains($" {elementIdentifier}\"") ||
            htmlStructure.Contains($" {elementIdentifier}'"))
        {
            alternatives.Add($".{elementIdentifier}");
        }

        // Try to find by aria-label
        if (htmlStructure.Contains($"aria-label=\"{elementIdentifier}\"") || 
            htmlStructure.Contains($"aria-label='{elementIdentifier}'"))
        {
            alternatives.Add($"[aria-label='{elementIdentifier}']");
        }
        
        return alternatives;
    }
}
