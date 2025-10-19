using SuperQA.Core.Interfaces;

namespace SuperQA.Infrastructure.Services;

/// <summary>
/// Service for validating that healed locators target the correct elements
/// </summary>
public class LocatorValidationService : ILocatorValidationService
{
    /// <summary>
    /// Validates that a new locator is targeting the same type of element as the old one
    /// by analyzing the locator patterns and element types
    /// </summary>
    public bool IsLocatorValid(string oldLocator, string newLocator, string? htmlContext = null)
    {
        if (string.IsNullOrWhiteSpace(oldLocator) || string.IsNullOrWhiteSpace(newLocator))
        {
            return false;
        }

        // If locators are identical, validation passes
        if (oldLocator == newLocator)
        {
            return true;
        }

        // Extract element type hints from locators
        var oldElementType = ExtractElementTypeHint(oldLocator);
        var newElementType = ExtractElementTypeHint(newLocator);

        // If both locators suggest different element types, they likely target different elements
        if (!string.IsNullOrEmpty(oldElementType) && 
            !string.IsNullOrEmpty(newElementType) && 
            oldElementType != newElementType &&
            !AreCompatibleElementTypes(oldElementType, newElementType))
        {
            return false;
        }

        // If HTML context is provided, validate that both locators could target similar elements
        if (!string.IsNullOrWhiteSpace(htmlContext))
        {
            return ValidateAgainstHtml(oldLocator, newLocator, htmlContext);
        }

        // If no conflicts detected, consider it valid
        return true;
    }

    /// <summary>
    /// Checks if a locator contains patterns that suggest it might target unintended elements
    /// </summary>
    public bool HasMismatchPatterns(string locator, string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(locator) || string.IsNullOrWhiteSpace(errorMessage))
        {
            return false;
        }

        // Check for common mismatch patterns
        
        // Pattern 1: Locator is too generic (e.g., just "button" or "div")
        if (IsGenericLocator(locator))
        {
            return true;
        }

        // Pattern 2: Error message mentions a specific element but locator doesn't match
        // e.g., error says "button" but locator targets "input"
        var errorElementType = ExtractElementTypeFromError(errorMessage);
        var locatorElementType = ExtractElementTypeHint(locator);

        if (!string.IsNullOrEmpty(errorElementType) && 
            !string.IsNullOrEmpty(locatorElementType) &&
            errorElementType != locatorElementType &&
            !AreCompatibleElementTypes(errorElementType, locatorElementType))
        {
            return true;
        }

        return false;
    }

    private string ExtractElementTypeHint(string locator)
    {
        // Extract element type from role-based locators (most specific)
        if (locator.Contains("AriaRole.Button") || locator.Contains("role='button'") || locator.Contains("role=\"button\""))
        {
            return "button";
        }
        if (locator.Contains("AriaRole.Textbox") || locator.Contains("role='textbox'") || locator.Contains("role=\"textbox\""))
        {
            return "input";
        }
        if (locator.Contains("AriaRole.Link") || locator.Contains("role='link'") || locator.Contains("role=\"link\""))
        {
            return "link";
        }
        if (locator.Contains("AriaRole.Checkbox") || locator.Contains("role='checkbox'") || locator.Contains("role=\"checkbox\""))
        {
            return "checkbox";
        }
        if (locator.Contains("AriaRole.Radio") || locator.Contains("role='radio'") || locator.Contains("role=\"radio\""))
        {
            return "radio";
        }
        if (locator.Contains("AriaRole.Combobox") || locator.Contains("role='combobox'") || locator.Contains("role=\"combobox\""))
        {
            return "select";
        }
        
        // Extract from Playwright GetBy methods
        if (locator.Contains("GetByRole") && locator.Contains("Button"))
        {
            return "button";
        }
        if (locator.Contains("GetByRole") && locator.Contains("Link"))
        {
            return "link";
        }
        if (locator.Contains("GetByRole") && locator.Contains("Textbox"))
        {
            return "input";
        }

        // Extract from common naming patterns (less specific, check last)
        var lowerLocator = locator.ToLower();
        
        // Check for specific element types first to avoid false positives
        if (lowerLocator.Contains("checkbox"))
        {
            return "checkbox";
        }
        if (lowerLocator.Contains("radio"))
        {
            return "radio";
        }
        if (lowerLocator.Contains("select") || lowerLocator.Contains("dropdown"))
        {
            return "select";
        }
        if (lowerLocator.Contains("textarea"))
        {
            return "textarea";
        }
        if (lowerLocator.Contains("button") || lowerLocator.Contains("btn") || lowerLocator.Contains("submit"))
        {
            return "button";
        }
        if (lowerLocator.Contains("input") || lowerLocator.Contains("field") || lowerLocator.Contains("textbox"))
        {
            return "input";
        }
        if (lowerLocator.Contains("link") || lowerLocator.Contains("anchor") || lowerLocator.Contains("href"))
        {
            return "link";
        }
        if (lowerLocator.Contains("image") || lowerLocator.Contains("img"))
        {
            return "image";
        }

        return string.Empty;
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

        return string.Empty;
    }

    private bool AreCompatibleElementTypes(string type1, string type2)
    {
        // Some element types are compatible for healing purposes
        var compatibleGroups = new List<HashSet<string>>
        {
            new HashSet<string> { "button", "submit", "btn" },
            new HashSet<string> { "input", "textbox", "field", "text" },
            new HashSet<string> { "link", "anchor", "a" },
            new HashSet<string> { "checkbox", "check" },
            new HashSet<string> { "radio", "radiobutton" },
            new HashSet<string> { "select", "dropdown", "combobox" },
            new HashSet<string> { "textarea", "multiline" },
            new HashSet<string> { "image", "img" }
        };

        foreach (var group in compatibleGroups)
        {
            if (group.Contains(type1.ToLower()) && group.Contains(type2.ToLower()))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsGenericLocator(string locator)
    {
        // Locators that are too generic and might match multiple unintended elements
        var genericPatterns = new[] { "button", "div", "span", "input", "a", "p", "body", "html" };
        
        var cleanedLocator = locator.Trim().ToLower();
        
        // Remove quotes and common selector prefixes for comparison
        cleanedLocator = cleanedLocator.Replace("\"", "").Replace("'", "");
        if (cleanedLocator.StartsWith("#") || cleanedLocator.StartsWith("."))
        {
            cleanedLocator = cleanedLocator.Substring(1);
        }

        return genericPatterns.Contains(cleanedLocator);
    }

    private bool ValidateAgainstHtml(string oldLocator, string newLocator, string htmlContext)
    {
        // Extract element identifiers from locators
        var oldIdentifier = ExtractIdentifierFromLocator(oldLocator);
        var newIdentifier = ExtractIdentifierFromLocator(newLocator);

        if (string.IsNullOrEmpty(oldIdentifier) || string.IsNullOrEmpty(newIdentifier))
        {
            return true; // Can't validate without identifiers
        }

        // Check if both identifiers appear in the HTML context near each other
        var oldIndex = htmlContext.IndexOf(oldIdentifier, StringComparison.OrdinalIgnoreCase);
        var newIndex = htmlContext.IndexOf(newIdentifier, StringComparison.OrdinalIgnoreCase);

        if (oldIndex == -1 && newIndex == -1)
        {
            return true; // Neither found, can't invalidate
        }

        if (oldIndex == -1 || newIndex == -1)
        {
            return true; // Only one found, assume it's the replacement
        }

        // If both are found, they should be in the same general area (within 500 characters)
        return Math.Abs(oldIndex - newIndex) < 500;
    }

    private string ExtractIdentifierFromLocator(string locator)
    {
        // Extract the main identifier from various locator formats
        
        // ID selector: #elementId -> elementId
        if (locator.StartsWith("#"))
        {
            return locator.Substring(1).Split(new[] { ' ', '.', '[' })[0];
        }

        // Class selector: .className -> className
        if (locator.StartsWith("."))
        {
            return locator.Substring(1).Split(new[] { ' ', '#', '[' })[0];
        }

        // Attribute selector: [data-testid='value'] -> value
        var attributeMatch = System.Text.RegularExpressions.Regex.Match(locator, @"\[.*?['""]([^'""]+)['""].*?\]");
        if (attributeMatch.Success)
        {
            return attributeMatch.Groups[1].Value;
        }

        // Role-based selector: GetByRole(..., Name = "Submit") -> Submit
        var roleMatch = System.Text.RegularExpressions.Regex.Match(locator, @"Name\s*=\s*['""]([^'""]+)['""]");
        if (roleMatch.Success)
        {
            return roleMatch.Groups[1].Value;
        }

        return string.Empty;
    }
}
