# Browser Extension Integration Fix

## Problem
When clicking "🚀 Send to SuperQA" in the browser extension, users encountered:
- `ERR_EMPTY_RESPONSE` error
- Tests were not being saved to the database
- No "Generated Tests" project/folder to organize extension-generated tests

## Solution

### 1. CORS Policy Update
**File**: `src/SuperQA.Api/Program.cs`

Added a new CORS policy to allow browser extensions to access the API:

```csharp
// Allow browser extensions to access the API
options.AddPolicy("AllowAll", policy =>
{
    policy.AllowAnyOrigin()
          .AllowAnyMethod()
          .AllowAnyHeader();
});
```

Changed the middleware to use the new policy:
```csharp
app.UseCors("AllowAll");
```

This allows requests from `chrome-extension://` origins which were previously blocked.

### 2. Test Case Persistence
**File**: `src/SuperQA.Api/Controllers/PlaywrightController.cs`

Updated the `generate-from-extension` endpoint to:

1. **Find or Create "Generated Tests" Project**:
   - Automatically creates a "Generated Tests" project if it doesn't exist
   - All extension-generated tests are saved to this project

2. **Save Test Cases to Database**:
   - Creates a `TestCase` entity with all the extension data
   - Stores the generated Playwright script in the `AutomationScript` field
   - Links the test to the "Generated Tests" project

3. **Return Test Case Information**:
   - Response now includes `TestCaseId` and `ProjectId`
   - Extension can use these IDs to navigate to the test in SuperQA

### 3. DTO Update
**File**: `src/SuperQA.Shared/DTOs/PlaywrightTestGenerationDto.cs`

Added new fields to `PlaywrightTestGenerationResponse`:
```csharp
public int? TestCaseId { get; set; }
public int? ProjectId { get; set; }
```

### 4. UI Enhancements for Test Execution
**Files**: 
- `src/SuperQA.Client/Pages/ProjectDetails.razor`
- `src/SuperQA.Client/Services/TestCaseService.cs`

Added new functionality to ProjectDetails page:

1. **Generate AI Script Button**: For test cases without automation scripts
   - Calls `/api/testcases/generate-automation-script` endpoint
   - Uses AI to convert test steps into Playwright code
   - Shows loading spinner and success/error messages

2. **Run Test Button**: For test cases with automation scripts
   - Calls `/api/playwright/execute` endpoint to run the test
   - Displays test execution results (Pass/Fail)
   - Shows test output and logs

3. **View Script Button**: To view the generated Playwright code
   - Toggles display of automation script in code block
   - Syntax highlighting for better readability

## How It Works Now

1. User records test steps in browser extension
2. User clicks "🚀 Send to SuperQA"
3. Extension sends steps to `/api/playwright/generate-from-extension`
4. API:
   - Validates the request
   - Gets OpenAI API key from settings (or request)
   - Inspects the page structure
   - Generates Playwright test script using AI
   - Creates/finds "Generated Tests" project
   - Saves test case with script to database
   - Returns success with test case ID
5. Extension opens SuperQA in new tab where user can:
   - View the test in the "Generated Tests" project
   - Run the test with one click
   - View the generated automation script
   - See test execution results

## Testing

### CORS Verification
```bash
curl -v -X POST https://localhost:7001/api/playwright/generate-from-extension \
  -H "Content-Type: application/json" \
  -H "Origin: chrome-extension://test-extension-id" \
  -d '{"applicationUrl":"http://example.com","testName":"Test","steps":[...]}'
```

You should see `Access-Control-Allow-Origin: *` in the response headers.

### Database Persistence Verification
After sending a test from the extension:
1. Navigate to SuperQA at `https://localhost:7001` (or `https://localhost:5001`)
2. Look for the "Generated Tests" project in the projects list
3. Click on the project to see the test case
4. The test should have the automation script ready to run

## Configuration

### OpenAI API Key
The endpoint requires an OpenAI API key, which can be provided in two ways:

1. **User Settings** (Recommended):
   - Configure via Settings page in SuperQA
   - Key is stored and reused for all extension requests

2. **Request Payload**:
   - Pass `openAIApiKey` field in the extension request
   - Useful for per-request customization

### Model Selection
Default model is `gpt-4o-mini`. Can be changed in:
- User Settings (applies to all requests)
- Request payload (per-request override)

## Troubleshooting ERR_EMPTY_RESPONSE

If you encounter `ERR_EMPTY_RESPONSE` error:

1. **Ensure API is running**:
   ```bash
   cd src/SuperQA.Api
   dotnet run
   ```
   Verify it's listening on `https://localhost:7001`

2. **Check API accessibility**:
   ```bash
   curl -k https://localhost:7001/api/playwright/generate-from-extension
   ```

3. **Verify OpenAI API key is configured** in Settings

4. **Check browser console** for detailed error messages

## Security Notes

The `AllowAll` CORS policy allows requests from any origin, including browser extensions. This is necessary for the extension to work but means the API is accessible from any web page.

**Future Enhancement**: Consider implementing API key authentication for the extension endpoint to prevent unauthorized access while still allowing CORS.

