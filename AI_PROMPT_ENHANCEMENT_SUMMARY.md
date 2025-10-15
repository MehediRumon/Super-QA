# AI Prompt Enhancement and Intelligent Duplicate Filtering

## Overview
This implementation addresses the requirements from the problem statement to improve the quality of generated test scripts and intelligent duplicate handling in Gherkin steps.

## Changes Made

### 1. Enhanced AI Prompt (OpenAIService.cs)

#### Improvements to Prompt Quality
The AI prompt in `OpenAIService.cs` has been significantly enhanced with the following critical requirements:

**Before:**
```
REQUIREMENTS:
1) Use Microsoft.Playwright and Microsoft.Playwright.NUnit with NUnit; class inherits from PageTest
2) Follow the CRITICAL SELECTOR POLICY strictly
3) Implement actions and assertions based on FRS
4) Use async/await properly
5) Return ONLY executable C# code, no markdown fences
```

**After:**
```
CRITICAL REQUIREMENTS:
1) Generate COMPLETE, RUNNABLE C# code with NO syntax errors
2) Use Microsoft.Playwright and Microsoft.Playwright.NUnit with NUnit; class inherits from PageTest
3) Follow the CRITICAL SELECTOR POLICY strictly
4) Implement ALL actions and assertions based on FRS
5) If a step has a locator but NO test data (empty/missing value), you MUST generate appropriate test data
   - For email fields: use "test@example.com"
   - For username fields: use "testuser"
   - For password fields: use "Test@123"
   - For search/text fields: use descriptive test data based on field name
   - For numeric fields: use appropriate numbers
6) Use async/await properly with correct syntax
7) Return ONLY executable C# code with proper structure, no markdown fences
8) Include proper using statements: using Microsoft.Playwright; using Microsoft.Playwright.NUnit; using NUnit.Framework;
9) Class must be named with valid C# identifier (only letters, digits, underscore)
10) Test method must be named with valid C# identifier and have [Test] attribute
```

#### Key Enhancements:
1. **Explicit requirement for runnable code** - "COMPLETE, RUNNABLE C# code with NO syntax errors"
2. **Test data generation guidance** - Specific instructions for filling missing test data
3. **Syntax correctness** - Explicit requirements for proper structure and identifiers
4. **Increased max_tokens** - From 1700 to 2000 to allow for more complete code generation

#### System Message Enhancement
**Before:**
```
"You are a Playwright test automation engineer. Use Page.GetByRole when role+name exists; otherwise use provided selectors. Never invent selectors."
```

**After:**
```
"You are an expert Playwright test automation engineer. Generate COMPLETE, RUNNABLE C# code with NO syntax errors. Use Page.GetByRole when role+name exists; otherwise use provided selectors. Never invent selectors. Always provide test data for input fields - never leave them empty. The code must compile and run without errors."
```

### 2. Intelligent FRS Generation (PlaywrightController.cs)

The `GenerateFrsFromSteps` method now includes intelligent handling for steps with missing test data:

**Enhancement:**
```csharp
if (!string.IsNullOrWhiteSpace(step.Value))
{
    frs += $"   Value: {step.Value}\n";
}
else if (step.Action?.ToLower() == "fill" || step.Action?.ToLower() == "type")
{
    // Indicate that AI should generate test data for fill/type actions without values
    frs += $"   Value: [AI: Generate appropriate test data based on field name/type]\n";
}
```

This ensures the AI understands when it needs to generate test data for input fields.

### 3. Intelligent Duplicate Filtering (view.js)

The `filterDuplicateStrings` function has been completely rewritten to intelligently detect and remove duplicates:

**Before:**
```javascript
function filterDuplicateStrings(arr) {
    return Array.from(new Set(arr));
}
```

**After:**
```javascript
function filterDuplicateStrings(arr) {
    // Intelligent duplicate removal
    // Two steps are considered duplicates if they have the same action and locator
    const seen = new Map();
    const result = [];
    
    for (const step of arr) {
        // Extract the key parts: action description and locator (if present)
        // Format: "Action on Element (xpath=...)" or "Action on Element"
        const locatorMatch = step.match(/\((xpath=.+?|css=.+?|id=.+?)\)$/);
        const locator = locatorMatch ? locatorMatch[1] : '';
        
        // Get the description part (everything before the locator)
        const description = locator ? step.substring(0, step.lastIndexOf('(')).trim() : step.trim();
        
        // Create a unique key combining description and locator
        const key = locator ? `${description}|${locator}` : description;
        
        // Only add if we haven't seen this combination before
        if (!seen.has(key)) {
            seen.set(key, true);
            result.push(step);
        }
    }
    
    return result;
}
```

**Key Features:**
- Parses step format to extract description and locator separately
- Considers steps duplicate only if both description AND locator match
- Preserves order of first occurrence
- Handles steps with and without locators

**Example:**
```javascript
// Input:
[
  'Click on Login (xpath=//button[@id="login"])',
  'Click on Login (xpath=//button[@id="login"])',  // Duplicate - same description and locator
  'Click on Login (xpath=//button[@id="submit"])'  // Not duplicate - different locator
]

// Output:
[
  'Click on Login (xpath=//button[@id="login"])',
  'Click on Login (xpath=//button[@id="submit"])'
]
```

## Testing

### C# Unit Tests (38 tests passing)

#### OpenAIServiceTests.cs
New tests added:
- `GeneratePlaywrightTestScriptAsync_WithPageStructure_SendsEnhancedPrompt` - Verifies enhanced prompt contains critical requirements
- `GeneratePlaywrightTestScriptAsync_IncreasedMaxTokens_AllowsLongerResponses` - Verifies max_tokens increased to 2000

#### PlaywrightControllerTests.cs (New file)
Tests for FRS generation with missing values:
- `GenerateFromExtension_WithFillActionAndNoValue_IndicatesAIShouldFillData` - Verifies AI instruction is added for empty values
- `GenerateFromExtension_WithFillActionAndValue_PreservesValue` - Verifies provided values are preserved
- `GenerateFromExtension_WithClickAction_DoesNotIndicateAIShouldFill` - Verifies non-input actions don't get AI instruction
- `GenerateFromExtension_WithTypeActionAndNoValue_IndicatesAIShouldFillData` - Verifies type action also triggers AI instruction

### JavaScript Unit Tests (7 tests passing)

Created `test-duplicate-filtering.js` with comprehensive tests:
- Should remove exact duplicate strings
- Should keep steps with same description but different locators
- Should remove duplicates with same description and locator
- Should handle steps without locators
- Should handle empty array
- Should preserve order of first occurrence
- Should handle mixed steps with and without locators

Also created `test-duplicate-filtering.html` for browser-based testing.

## Impact

### 1. Improved Test Script Quality
- AI now generates more complete and runnable code
- Reduced syntax errors in generated scripts
- Better handling of missing test data

### 2. Intelligent Duplicate Removal
- More accurate detection of duplicate steps
- Preserves steps with different locators even if description is similar
- Better user experience with cleaner step lists

### 3. Better Test Data Generation
- AI automatically fills in appropriate test data when values are missing
- Uses sensible defaults (test@example.com for emails, testuser for usernames, etc.)
- Reduces manual editing needed after generation

## Files Modified

1. `src/SuperQA.Infrastructure/Services/OpenAIService.cs` - Enhanced AI prompt
2. `src/SuperQA.Api/Controllers/PlaywrightController.cs` - Intelligent FRS generation
3. `Test-Case-and-Selector-Generator-Extension/view.js` - Intelligent duplicate filtering
4. `tests/SuperQA.Tests/OpenAIServiceTests.cs` - Added tests for enhanced prompt
5. `tests/SuperQA.Tests/PlaywrightControllerTests.cs` - New test file for controller logic
6. `tests/SuperQA.Tests/SuperQA.Tests.csproj` - Added project references

## Files Created

1. `Test-Case-and-Selector-Generator-Extension/test-duplicate-filtering.js` - JavaScript unit tests
2. `Test-Case-and-Selector-Generator-Extension/test-duplicate-filtering.html` - Browser-based tests

## Verification

All changes have been verified:
- ✅ All 38 C# unit tests passing
- ✅ All 7 JavaScript unit tests passing
- ✅ Build successful with no errors
- ✅ Code follows existing patterns and conventions
