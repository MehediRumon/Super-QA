using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SuperQA.Core.Interfaces;

namespace SuperQA.Infrastructure.Services;

public class ScriptComparisonService : IScriptComparisonService
{
    public List<string> ExtractLocators(string script)
    {
        if (string.IsNullOrWhiteSpace(script))
            return new List<string>();

        var locators = new HashSet<string>(); // Use HashSet to avoid duplicates
        
        // Pattern to match Playwright locator methods
        // Need to handle both single and double quotes, and escaped quotes
        var patterns = new[]
        {
            @"Page\.Locator\(""[^""]*""\)",  // Double quotes
            @"Page\.Locator\('[^']*'\)",      // Single quotes
            @"Page\.GetByRole\([^)]+\)",
            @"Page\.GetByTestId\(""[^""]*""\)",
            @"Page\.GetByTestId\('[^']*'\)",
            @"Page\.GetById\(""[^""]*""\)",
            @"Page\.GetById\('[^']*'\)",
            @"Page\.GetByText\(""[^""]*""\)",
            @"Page\.GetByText\('[^']*'\)",
            @"Page\.GetByPlaceholder\(""[^""]*""\)",
            @"Page\.GetByPlaceholder\('[^']*'\)",
            @"Page\.GetByLabel\(""[^""]*""\)",
            @"Page\.GetByLabel\('[^']*'\)",
            @"Page\.GetByTitle\(""[^""]*""\)",
            @"Page\.GetByTitle\('[^']*'\)"
        };

        foreach (var pattern in patterns)
        {
            var matches = Regex.Matches(script, pattern, RegexOptions.Multiline);
            foreach (Match match in matches)
            {
                locators.Add(match.Value);
            }
        }

        return locators.ToList();
    }

    public List<(string Original, string Healed)> GetChangedLocators(string originalScript, string healedScript, string errorMessage)
    {
        var originalLocators = ExtractLocators(originalScript);
        var healedLocators = ExtractLocators(healedScript);
        var changes = new List<(string Original, string Healed)>();

        // Find locators that exist in original but not in healed (changed or removed)
        foreach (var originalLocator in originalLocators)
        {
            if (!healedLocators.Contains(originalLocator))
            {
                // This locator was changed or removed
                // Try to find what it was changed to by position/context
                var index = originalLocators.IndexOf(originalLocator);
                if (index < healedLocators.Count)
                {
                    var healedLocator = healedLocators[index];
                    if (healedLocator != originalLocator)
                    {
                        changes.Add((originalLocator, healedLocator));
                    }
                }
                else
                {
                    // Locator was removed
                    changes.Add((originalLocator, "REMOVED"));
                }
            }
        }

        return changes;
    }

    public bool ValidateHealedScript(string originalScript, string healedScript, string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(originalScript) || string.IsNullOrWhiteSpace(healedScript))
            return true; // Can't validate empty scripts

        if (string.IsNullOrWhiteSpace(errorMessage))
            return true; // Can't validate without error context

        var originalLocators = ExtractLocators(originalScript);
        var healedLocators = ExtractLocators(healedScript);

        // Find the failing locator from the error message
        var failingLocator = ExtractFailingLocatorFromError(errorMessage);
        
        if (string.IsNullOrWhiteSpace(failingLocator))
            return true; // Can't determine failing locator, allow the healing

        // Check if any original locators that were NOT the failing one are missing in the healed script
        foreach (var originalLocator in originalLocators)
        {
            // Skip if this is the locator that failed
            if (IsLocatorRelatedToError(originalLocator, failingLocator))
                continue;

            // This is a working locator - it should still be present in the healed script
            if (!healedLocators.Contains(originalLocator))
            {
                // A working locator was changed - this is invalid!
                return false;
            }
        }

        return true; // All working locators are preserved
    }

    private string ExtractFailingLocatorFromError(string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(errorMessage))
            return string.Empty;

        // Common patterns in error messages that contain the failing locator
        var patterns = new[]
        {
            @"Locator\([""']([^""']+)[""']\)",
            @"element not found:\s*([^\s\n]+)",
            @"waiting for\s+Locator\([""']([^""']+)[""']\)",
            @"selector\s+[""']([^""']+)[""']",
            @"//[^\s\n""']+", // XPath
            @"#[\w-]+", // ID selector
            @"\[data-testid=[""']?[\w-]+[""']?\]" // data-testid
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(errorMessage, pattern);
            if (match.Success)
            {
                return match.Groups[match.Groups.Count > 1 ? 1 : 0].Value;
            }
        }

        return string.Empty;
    }

    private bool IsLocatorRelatedToError(string locator, string failingLocator)
    {
        if (string.IsNullOrWhiteSpace(locator) || string.IsNullOrWhiteSpace(failingLocator))
            return false;

        // Check if the locator contains the failing locator or vice versa
        // This handles cases where the error shows "//div[@id='SpecialElement']" 
        // and the locator is "Page.Locator("//div[@id='SpecialElement']")"
        
        // Normalize both strings for comparison
        var normalizedLocator = locator.Replace("\"", "'").Replace(" ", "");
        var normalizedFailing = failingLocator.Replace("\"", "'").Replace(" ", "");

        return normalizedLocator.Contains(normalizedFailing) || 
               normalizedFailing.Contains(normalizedLocator);
    }
}
