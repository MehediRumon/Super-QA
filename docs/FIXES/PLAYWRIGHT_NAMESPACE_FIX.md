# Fix: Playwright Namespace Issue

## Problem

When generating Playwright test scripts using the AI-powered generator, the OpenAI model was sometimes generating code using the deprecated `PlaywrightSharp` namespace instead of the correct `Microsoft.Playwright` namespace. This caused compilation errors:

```
error CS0246: The type or namespace name 'PlaywrightSharp' could not be found
error CS0246: The type or namespace name 'IBrowser' could not be found  
error CS0246: The type or namespace name 'IPage' could not be found
```

## Root Cause

The AI prompt in `OpenAIService.cs` was not explicit enough about:
1. Which exact namespaces to use
2. The correct class structure (inheriting from `PageTest`)
3. Prohibition against using deprecated libraries like PlaywrightSharp

## Solution

Updated the AI prompt in `src/SuperQA.Infrastructure/Services/OpenAIService.cs` to:

1. **Explicitly specify required namespaces**:
   ```
   - using Microsoft.Playwright;
   - using Microsoft.Playwright.NUnit;
   - using NUnit.Framework;
   ```

2. **Provide a complete code template** showing the correct structure:
   ```csharp
   public class Tests : PageTest
   {
       [Test]
       public async Task YourTestName()
       {
           await Page.GotoAsync("URL_HERE");
           // Test steps
       }
   }
   ```

3. **Add explicit prohibitions** in the system message:
   - "CRITICAL: Use ONLY Microsoft.Playwright namespace (NOT PlaywrightSharp)"
   - "Test class MUST inherit from PageTest"

## Changes Made

### File: `src/SuperQA.Infrastructure/Services/OpenAIService.cs`

1. Enhanced the prompt with CRITICAL REQUIREMENTS section
2. Added a REQUIRED CODE STRUCTURE template
3. Updated system message to explicitly prohibit PlaywrightSharp
4. Specified that the test class must inherit from PageTest

### File: `tests/SuperQA.Tests/OpenAIServiceTests.cs`

Added a new test `GeneratePlaywrightTestScriptAsync_SuccessfulResponse_DoesNotContainPlaywrightSharp()` that validates:
- Generated code contains `Microsoft.Playwright` namespace
- Generated code contains `Microsoft.Playwright.NUnit` namespace
- Generated code contains `PageTest` base class
- Generated code does NOT contain `PlaywrightSharp`
- Generated code does NOT contain deprecated interfaces like `IBrowser` or `IPage`

## Testing

All 13 tests pass, including:
- Existing OpenAI service tests
- New test validating correct namespaces
- PlaywrightTestExecutor tests
- Integration tests

## Impact

- **Minimal code changes**: Only modified the AI prompt and added one test
- **No breaking changes**: Existing functionality remains intact
- **Improved reliability**: Generated test scripts now consistently use correct namespaces
- **Better error messages**: Users will no longer encounter confusing namespace errors

## Prevention

The enhanced prompt with explicit requirements and code template ensures:
1. Consistent namespace usage across all generated tests
2. Proper class structure (inheriting from PageTest)
3. Use of modern Playwright API
4. Executable code that builds without namespace errors
