# C# Syntax Validation - Quick Reference

## What It Does
Automatically validates C# syntax in AI-generated test scripts to ensure they compile successfully. If errors are found, the system attempts to fix them automatically (up to 2 retries).

## How It Works

### Automatic Validation Flow
```
AI generates code
    ↓
Syntax validation (instant)
    ↓
Valid? → Yes → Return code ✓
    ↓
    No
    ↓
Retry with error feedback (Attempt 1)
    ↓
Valid? → Yes → Return code ✓
    ↓
    No
    ↓
Retry with error feedback (Attempt 2)
    ↓
Valid? → Yes → Return code ✓
    ↓
    No
    ↓
Throw exception with detailed errors ✗
```

## Using the Service Directly

### Basic Syntax Validation
```csharp
var validationService = new CSharpSyntaxValidationService();
var code = "your C# code here";

var (isValid, errors) = validationService.ValidateSyntax(code);

if (!isValid)
{
    foreach (var error in errors)
    {
        Console.WriteLine(error);
    }
}
```

### Detailed Validation with Context
```csharp
var (isValid, detailedMessage) = validationService.ValidateSyntaxWithDetails(code);

if (!isValid)
{
    Console.WriteLine(detailedMessage);
    // Contains:
    // - All syntax errors with line numbers
    // - Suggestions for fixing common issues
    // - Best practices reminders
}
```

### Get Structured Error Details
```csharp
var errors = validationService.GetDetailedErrors(code);

foreach (var error in errors)
{
    Console.WriteLine($"Line {error.LineNumber}, Column {error.ColumnNumber}");
    Console.WriteLine($"Error: {error.ErrorMessage}");
    Console.WriteLine($"Code: {error.LineContent}");
}
```

## Common Syntax Errors Detected

### 1. Missing Semicolon
```csharp
// ✗ Invalid
Console.WriteLine("Hello")

// ✓ Valid
Console.WriteLine("Hello");
```

### 2. Missing Braces
```csharp
// ✗ Invalid
public void MyMethod()
{
    Console.WriteLine("Hello");

// ✓ Valid
public void MyMethod()
{
    Console.WriteLine("Hello");
}
```

### 3. Unclosed String Literals
```csharp
// ✗ Invalid
var text = "Hello;

// ✓ Valid
var text = "Hello";
```

### 4. Missing Async/Await
```csharp
// ✗ Invalid
public Task MyMethod()
{
    Page.GotoAsync("url");
}

// ✓ Valid
public async Task MyMethod()
{
    await Page.GotoAsync("url");
}
```

## When Validation Runs

### Automatic Validation
The validation runs automatically in:
- **OpenAIService.GeneratePlaywrightTestScriptAsync()** - When generating new tests
- **OpenAIService.HealTestScriptAsync()** - When healing/fixing failed tests
- **AITestHealingService.HealTestScriptAsync()** - When AI heals test cases

### No Action Required
You don't need to call validation manually. It's integrated into the AI generation pipeline.

## Error Messages

### Validation Success
```
Code validates successfully ✓
```

### Validation Failure (After Retries)
```
Failed to generate valid C# code after 2 attempts.

The generated C# code contains syntax errors:

Line 10: ; expected
Line 15: } expected

Please fix these syntax errors and regenerate the code. Ensure that:
1. All statements end with semicolons
2. All braces are properly matched
3. All string literals are properly closed
4. All method calls use correct syntax
5. All async methods use 'await' correctly
6. Variable and method names follow C# naming conventions
```

## Configuration

### No Configuration Required
The service is automatically registered in dependency injection:
```csharp
// Already configured in Program.cs
builder.Services.AddScoped<ICSharpSyntaxValidationService, CSharpSyntaxValidationService>();
```

### Customizing Retry Count
Currently hardcoded to 2 retries. To change:
```csharp
// In OpenAIService.cs or AITestHealingService.cs
private const int MaxRetries = 2; // Change this value
```

## Testing

### Run Syntax Validation Tests
```bash
dotnet test --filter "FullyQualifiedName~CSharpSyntaxValidationServiceTests"
```

### Run All Tests
```bash
dotnet test
```

## Troubleshooting

### Issue: Validation Takes Too Long
**Solution**: Validation is instant (Roslyn syntax-only parsing). If slow, check network latency to OpenAI API during retries.

### Issue: Still Getting Build Errors
**Possible Causes**:
1. Semantic errors (e.g., undefined types) - validation only checks syntax
2. Missing using statements - add them to AI prompts
3. Package version mismatches - ensure all packages are up to date

### Issue: Too Many Retries Failing
**Solutions**:
1. Improve AI prompt with more specific instructions
2. Check if generated code is too complex
3. Consider manual review of failed attempts

## Performance Impact

### Validation Speed
- **Syntax validation**: < 10ms (very fast)
- **Retry overhead**: ~2-5 seconds per retry (OpenAI API call)

### Resource Usage
- Minimal CPU usage (Roslyn is efficient)
- No disk I/O (all in-memory)
- Small memory footprint (~10MB for Roslyn assemblies)

## Best Practices

### 1. Clear AI Prompts
Ensure AI prompts specify:
- Required using statements
- Correct namespace structure
- Proper async/await usage
- Complete class definitions

### 2. Monitor Retry Rates
Track how often retries occur:
- High retry rate → Improve prompts
- Low retry rate → System working well

### 3. Handle Exceptions Gracefully
```csharp
try
{
    var code = await openAIService.GeneratePlaywrightTestScriptAsync(...);
    // Use code
}
catch (InvalidOperationException ex)
{
    // Handle validation failure
    logger.LogError($"Code generation failed: {ex.Message}");
    // Show error to user or try alternative approach
}
```

## Examples

### Example 1: Valid Playwright Test
```csharp
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace PlaywrightTests;

[TestFixture]
public class Tests : PageTest
{
    [Test]
    public async Task LoginTest()
    {
        await Page.GotoAsync("https://example.com");
        await Page.Locator("#username").FillAsync("testuser");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Login" }).ClickAsync();
        await Expect(Page).ToHaveTitleAsync("Dashboard");
    }
}
```
✓ Validates successfully

### Example 2: Invalid Playwright Test
```csharp
using Microsoft.Playwright;

namespace PlaywrightTests;

public class Tests
{
    public async Task LoginTest()  // Missing [Test] attribute
    {
        await Page.GotoAsync("https://example.com")  // Missing semicolon
        await Page.Locator("#username").FillAsync("testuser");
    // Missing closing brace
```
✗ Multiple syntax errors:
- Line 9: ; expected
- Line 10: } expected

After retry with error feedback → AI fixes errors → Validates successfully ✓

## Summary

✅ **Automatic** - No manual intervention needed
✅ **Fast** - Instant syntax validation
✅ **Self-healing** - Auto-retries with feedback
✅ **Comprehensive** - Catches all syntax errors
✅ **Developer-friendly** - Clear error messages

For detailed implementation information, see: [SYNTAX_VALIDATION_IMPLEMENTATION.md](SYNTAX_VALIDATION_IMPLEMENTATION.md)
