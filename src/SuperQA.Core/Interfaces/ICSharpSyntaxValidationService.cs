namespace SuperQA.Core.Interfaces;

/// <summary>
/// Service for validating C# syntax without compiling the code
/// </summary>
public interface ICSharpSyntaxValidationService
{
    /// <summary>
    /// Validates C# code for syntax errors
    /// </summary>
    /// <param name="code">The C# code to validate</param>
    /// <returns>Tuple of (isValid, errorMessages)</returns>
    (bool IsValid, List<string> Errors) ValidateSyntax(string code);

    /// <summary>
    /// Validates C# code and provides a detailed error message suitable for AI re-generation
    /// </summary>
    /// <param name="code">The C# code to validate</param>
    /// <returns>Tuple of (isValid, detailedErrorMessage)</returns>
    (bool IsValid, string DetailedErrorMessage) ValidateSyntaxWithDetails(string code);
}
