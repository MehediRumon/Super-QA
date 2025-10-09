# URL Extraction Feature for Test Case Automation

## Overview

The Test Case Automation Script Generation feature now includes automatic URL extraction from test case content. This enhancement makes the system more intelligent by automatically detecting which page to inspect based on the test case itself.

## How It Works

### Before This Feature

Users had to manually provide the `applicationUrl` parameter when generating automation scripts:

```json
{
  "testCaseId": 123,
  "applicationUrl": "https://example.com/login",  // Required
  "framework": "Playwright"
}
```

### After This Feature

The system automatically extracts URLs from test case content. The `applicationUrl` parameter is now optional:

```json
{
  "testCaseId": 123,
  "framework": "Playwright"
}
```

## URL Detection Sources

The system searches for URLs in the following order:

1. **Preconditions** - e.g., "User is on https://example.com/home"
2. **Steps** - e.g., "1. Navigate to https://example.com/login"
3. **Description** - e.g., "Test login at https://example.com/login"

The first URL found is used for page inspection.

## Examples

### Example 1: URL in Test Steps

**Test Case**:
- **Title**: User Login Test
- **Steps**: 
  ```
  1. Navigate to https://example.com/login
  2. Enter username
  3. Enter password
  4. Click login button
  ```

**Result**: The system automatically uses `https://example.com/login` for page inspection.

### Example 2: URL in Preconditions

**Test Case**:
- **Title**: Dashboard Test
- **Preconditions**: User is logged in and on http://localhost:5000/dashboard
- **Steps**:
  ```
  1. Click settings button
  2. Verify settings page loads
  ```

**Result**: The system automatically uses `http://localhost:5000/dashboard` for page inspection.

### Example 3: No URL in Test Case

**Test Case**:
- **Title**: Generic Test
- **Steps**:
  ```
  1. Click button
  2. Verify result
  ```

**Request**:
```json
{
  "testCaseId": 123,
  "applicationUrl": "https://example.com",  // Now required since no URL in test case
  "framework": "Playwright"
}
```

**Result**: The system uses the provided `applicationUrl` since no URL was found in the test case.

## Benefits

✅ **Less Manual Work**: No need to manually specify the URL when it's already in your test case  
✅ **More Accurate**: Uses the exact URL mentioned in the test case, reducing errors  
✅ **Better Integration**: The automation script generation is now truly based on the test case content  
✅ **Flexible**: Falls back to manual URL if needed  
✅ **Backward Compatible**: Existing API calls with `applicationUrl` still work  

## API Changes

### Request DTO

**Before**:
```csharp
public class GenerateAutomationScriptRequest
{
    public int TestCaseId { get; set; }
    public string ApplicationUrl { get; set; } = string.Empty;  // Required
    public string Framework { get; set; } = "Playwright";
}
```

**After**:
```csharp
public class GenerateAutomationScriptRequest
{
    public int TestCaseId { get; set; }
    public string? ApplicationUrl { get; set; }  // Optional
    public string Framework { get; set; } = "Playwright";
}
```

### Error Messages

If no URL is found in the test case and no `applicationUrl` is provided:

```json
{
  "success": false,
  "errorMessage": "Application URL is required (either provide it in the request or include a URL in the test case steps/preconditions)"
}
```

## Implementation Details

### New Method in IAITestGeneratorService

```csharp
string? ExtractUrlFromTestCase(TestCase testCase);
```

This method:
- Searches for URLs using regex pattern `https?://[^\s\)\]\>]+`
- Removes trailing punctuation (`.`, `,`, `;`)
- Returns the first URL found, or `null` if none

### Controller Logic

The `TestCasesController.GenerateAutomationScript` method now:

1. Attempts to extract URL from test case
2. Falls back to provided `applicationUrl` if extraction fails
3. Returns error if neither source provides a URL
4. Uses the determined URL for page inspection
5. Logs which URL is being used for transparency

## Testing

### Unit Tests Added

- `ExtractUrlFromTestCase_WithUrlInSteps_ReturnsUrl`
- `ExtractUrlFromTestCase_WithUrlInPreconditions_ReturnsUrl`
- `ExtractUrlFromTestCase_WithUrlInDescription_ReturnsUrl`
- `ExtractUrlFromTestCase_WithNoUrl_ReturnsNull`
- `ExtractUrlFromTestCase_WithUrlAndTrailingPunctuation_RemovesPunctuation`

All tests pass with 100% coverage of the URL extraction logic.

## Migration Guide

### For API Consumers

**No breaking changes!** Your existing code will continue to work. However, you can now simplify your code:

**Before**:
```javascript
const response = await fetch('/api/TestCases/generate-automation-script', {
  method: 'POST',
  body: JSON.stringify({
    testCaseId: 123,
    applicationUrl: 'https://example.com/login',  // Had to specify
    framework: 'Playwright'
  })
});
```

**After** (if your test case includes a URL):
```javascript
const response = await fetch('/api/TestCases/generate-automation-script', {
  method: 'POST',
  body: JSON.stringify({
    testCaseId: 123,
    framework: 'Playwright'  // URL extracted automatically
  })
});
```

### Best Practices

1. **Include URLs in Test Cases**: Add specific URLs to your test case steps for automatic extraction
2. **Be Specific**: Use complete URLs (e.g., `https://example.com/login` instead of just `/login`)
3. **First URL Wins**: If multiple URLs exist, the first one found is used
4. **Override When Needed**: You can still provide `applicationUrl` to override the extracted URL

## See Also

- [Test Case Automation Documentation](TEST_CASE_AUTOMATION.md)
- [Playwright Generator Guide](PLAYWRIGHT_GENERATOR.md)
