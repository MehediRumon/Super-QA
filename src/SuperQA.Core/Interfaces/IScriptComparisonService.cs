using System.Collections.Generic;

namespace SuperQA.Core.Interfaces;

public interface IScriptComparisonService
{
    /// <summary>
    /// Extracts locators from a Playwright test script
    /// </summary>
    List<string> ExtractLocators(string script);
    
    /// <summary>
    /// Compares two scripts and identifies locators that were changed
    /// Returns a list of (original locator, new locator) pairs for changed locators
    /// </summary>
    List<(string Original, string Healed)> GetChangedLocators(string originalScript, string healedScript, string errorMessage);
    
    /// <summary>
    /// Validates that a healed script only changes the failing locator mentioned in the error
    /// Returns true if valid, false if the healing changed working locators
    /// </summary>
    bool ValidateHealedScript(string originalScript, string healedScript, string errorMessage);
}
