namespace SuperQA.Core.Interfaces;

/// <summary>
/// Service for validating locators to prevent mismatched element selection during healing
/// </summary>
public interface ILocatorValidationService
{
    /// <summary>
    /// Validates that a new locator is targeting the same type of element as the old one
    /// </summary>
    /// <param name="oldLocator">The original locator that failed</param>
    /// <param name="newLocator">The new healed locator</param>
    /// <param name="htmlContext">Optional HTML context to validate against</param>
    /// <returns>True if the locator is valid, false if it likely targets a different element</returns>
    bool IsLocatorValid(string oldLocator, string newLocator, string? htmlContext = null);

    /// <summary>
    /// Checks if a locator contains patterns that suggest it might target unintended elements
    /// </summary>
    /// <param name="locator">The locator to check</param>
    /// <param name="errorMessage">The error message from the failed test</param>
    /// <returns>True if mismatch patterns are detected</returns>
    bool HasMismatchPatterns(string locator, string errorMessage);
}
