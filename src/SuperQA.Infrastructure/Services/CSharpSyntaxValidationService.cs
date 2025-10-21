using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SuperQA.Core.Interfaces;

namespace SuperQA.Infrastructure.Services;

/// <summary>
/// Service for validating C# syntax without compiling the code
/// </summary>
public class CSharpSyntaxValidationService : ICSharpSyntaxValidationService
{
    /// <summary>
    /// Validates C# code for syntax errors
    /// </summary>
    /// <param name="code">The C# code to validate</param>
    /// <returns>Tuple of (isValid, errorMessages)</returns>
    public (bool IsValid, List<string> Errors) ValidateSyntax(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return (false, new List<string> { "Code is empty or whitespace" });
        }

        try
        {
            // Parse the C# code into a syntax tree
            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            
            // Get diagnostics (errors and warnings)
            var diagnostics = syntaxTree.GetDiagnostics();
            
            // Filter to only get errors (not warnings)
            var errors = diagnostics
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .ToList();

            if (errors.Any())
            {
                var errorMessages = errors
                    .Select(e => $"Line {e.Location.GetLineSpan().StartLinePosition.Line + 1}: {e.GetMessage()}")
                    .ToList();
                
                return (false, errorMessages);
            }

            return (true, new List<string>());
        }
        catch (Exception ex)
        {
            return (false, new List<string> { $"Failed to parse code: {ex.Message}" });
        }
    }

    /// <summary>
    /// Validates C# code and provides a detailed error message suitable for AI re-generation
    /// </summary>
    /// <param name="code">The C# code to validate</param>
    /// <returns>Tuple of (isValid, detailedErrorMessage)</returns>
    public (bool IsValid, string DetailedErrorMessage) ValidateSyntaxWithDetails(string code)
    {
        var (isValid, errors) = ValidateSyntax(code);
        
        if (isValid)
        {
            return (true, string.Empty);
        }

        var errorMessage = "The generated C# code contains syntax errors:\n\n" +
                          string.Join("\n", errors) +
                          "\n\nPlease fix these syntax errors and regenerate the code. " +
                          "Ensure that:\n" +
                          "1. All statements end with semicolons\n" +
                          "2. All braces are properly matched\n" +
                          "3. All string literals are properly closed\n" +
                          "4. All method calls use correct syntax\n" +
                          "5. All async methods use 'await' correctly\n" +
                          "6. Variable and method names follow C# naming conventions";

        return (false, errorMessage);
    }

    /// <summary>
    /// Extracts specific syntax error locations and context for targeted fixing
    /// </summary>
    public List<SyntaxErrorDetail> GetDetailedErrors(string code)
    {
        var result = new List<SyntaxErrorDetail>();

        if (string.IsNullOrWhiteSpace(code))
        {
            return result;
        }

        try
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            var diagnostics = syntaxTree.GetDiagnostics()
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .ToList();

            var lines = code.Split('\n');

            foreach (var diagnostic in diagnostics)
            {
                var lineSpan = diagnostic.Location.GetLineSpan();
                var lineNumber = lineSpan.StartLinePosition.Line;
                var columnNumber = lineSpan.StartLinePosition.Character;
                
                var errorDetail = new SyntaxErrorDetail
                {
                    LineNumber = lineNumber + 1,
                    ColumnNumber = columnNumber + 1,
                    ErrorCode = diagnostic.Id,
                    ErrorMessage = diagnostic.GetMessage(),
                    LineContent = lineNumber < lines.Length ? lines[lineNumber].Trim() : string.Empty,
                    Severity = diagnostic.Severity.ToString()
                };

                result.Add(errorDetail);
            }
        }
        catch
        {
            // If parsing fails completely, return empty list
        }

        return result;
    }
}

/// <summary>
/// Detailed information about a syntax error
/// </summary>
public class SyntaxErrorDetail
{
    public int LineNumber { get; set; }
    public int ColumnNumber { get; set; }
    public string ErrorCode { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public string LineContent { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;

    public override string ToString()
    {
        return $"Line {LineNumber}, Column {ColumnNumber} [{ErrorCode}]: {ErrorMessage}\n  → {LineContent}";
    }
}
