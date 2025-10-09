# Test Case Automation Script Generation

## Overview

The Test Case Automation Script Generation feature allows you to generate Playwright test scripts from your existing test cases. The system automatically inspects your application's UI to extract actual element selectors, ensuring the generated automation scripts use real, working element locators.

## How It Works

When you generate an automation script for a test case, the system follows these steps:

1. **Test Case Information Extraction**: The system retrieves your test case details including:
   - Title
   - Description
   - Preconditions
   - Test Steps
   - Expected Results

2. **URL Detection**:
   - The system first attempts to extract the application URL from your test case (steps, preconditions, or description)
   - If no URL is found in the test case, it uses the `applicationUrl` from the request
   - This allows for automatic page inspection based on the test case content

3. **Page Inspection** (Automatic):
   - Launches a Playwright browser (Chromium in headless mode by default)
   - Navigates to the detected or provided application URL
   - Inspects the page to identify all interactive elements:
     - Input fields (with ID, name, placeholder, type)
     - Buttons (with ID, name, text)
     - Links (with ID, text, href)
     - Textareas and dropdowns
   - Extracts actual element selectors

4. **AI Script Generation**:
   - Sends the test case information along with actual page selectors to the AI
   - The AI generates a Playwright test script using the **exact selectors** from your page
   - The generated script follows the C# NUnit + Microsoft.Playwright pattern

5. **Script Storage**:
   - The generated automation script is automatically saved to the test case's `AutomationScript` field
   - You can execute or modify the script as needed

## API Usage

### Generate Automation Script for Test Case

**Endpoint**: `POST /api/TestCases/generate-automation-script`

**Request Body**:
```json
{
  "testCaseId": 123,
  "applicationUrl": "https://your-app.com",
  "framework": "Playwright"
}
```

**Parameters**:
- `testCaseId` (required): The ID of the test case to generate an automation script for
- `applicationUrl` (optional): The URL of your application to inspect. If not provided, the system will attempt to extract the URL from the test case steps, preconditions, or description
- `framework` (optional): The automation framework (currently only "Playwright" is supported, defaults to "Playwright")

**URL Auto-Detection**:
The system can automatically extract the application URL from your test case if it's mentioned in:
- Test Steps (e.g., "1. Navigate to https://example.com/login")
- Preconditions (e.g., "User is on https://example.com/home")
- Description (e.g., "Test login at https://example.com/login")

If a URL is found in the test case, it will be used for page inspection. Otherwise, the `applicationUrl` parameter is required.

**Response**:
```json
{
  "success": true,
  "automationScript": "using Microsoft.Playwright;\nusing Microsoft.Playwright.NUnit;\n...",
  "errorMessage": null,
  "warnings": null
}
```

**Response with Warning** (when page inspection fails):
```json
{
  "success": true,
  "automationScript": "// script with generic selectors",
  "errorMessage": null,
  "warnings": [
    "⚠️ Page inspection failed. The automation script will be generated with generic selectors. For best results, ensure Playwright browsers are installed (run 'playwright install chromium')."
  ]
}
```

## Example

### Test Case

**Title**: User Login Test

**Steps**:
1. Navigate to https://your-app.com/login
2. Enter username in the username field
3. Enter password in the password field
4. Click the login button

**Expected Results**: User is successfully logged in and redirected to the dashboard

**Note**: The URL `https://your-app.com/login` in step 1 will be automatically detected and used for page inspection, so you don't need to provide `applicationUrl` in the API request.

### Generated Script (with Page Inspection)

```csharp
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace PlaywrightTests;

[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class Tests : PageTest
{
    [Test]
    public async Task User_Login_Test()
    {
        await Page.GotoAsync("https://your-app.com/login");
        
        // Using actual selectors from the page
        await Page.FillAsync("#username", "testuser");
        await Page.FillAsync("#password", "password123");
        await Page.ClickAsync("#loginButton");
        
        // Verify successful login
        await Expect(Page).ToHaveURLAsync("https://your-app.com/dashboard");
    }
}
```

Note how the generated script uses actual selectors like `#username`, `#password`, and `#loginButton` instead of generic selectors.

## Benefits

✅ **Actual Element Selectors**: Uses real selectors from your application, not generic guesses  
✅ **Time-Saving**: Automatically generates automation scripts from test cases  
✅ **Executable Code**: Generated scripts are production-ready and follow best practices  
✅ **Error Prevention**: Reduces failures caused by incorrect selectors  
✅ **Consistent Structure**: All scripts follow the same C# + NUnit + Playwright pattern  
✅ **Graceful Degradation**: Falls back to generic selectors if page inspection fails  

## Requirements

### Playwright Browsers

For the page inspection feature to work properly, you need to have Playwright browsers installed on the server running the API.

**Installation**:

1. Install Playwright CLI globally:
   ```bash
   dotnet tool install --global Microsoft.Playwright.CLI
   ```

2. Install Chromium browser:
   ```bash
   playwright install chromium
   ```

3. Restart your application

### Application Accessibility

- Your application must be **accessible from the server** running Super-QA
- The URL must be reachable via HTTP/HTTPS
- If your application requires authentication, consider:
  - Creating a test environment without authentication
  - Using a publicly accessible staging/test instance
  - Configuring the application to allow anonymous access for the specific pages being tested

## Troubleshooting

### "Page inspection failed" Warning

**Problem**: The API returns a warning that page inspection failed.

**Solutions**:

1. **Check if Playwright browsers are installed**:
   ```bash
   playwright install chromium
   ```

2. **Verify the application URL is accessible**:
   - Open the URL in a browser from the server running Super-QA
   - Check for network connectivity issues
   - Ensure there are no authentication barriers

3. **Check server logs**:
   - Look for detailed error messages in the console output
   - Common issues:
     - "Executable doesn't exist" → Browsers not installed
     - "Navigation failed" → URL is not accessible
     - "Timeout" → Page takes too long to load

4. **Test with a simple public page first**:
   - Try generating a script for a simple page like `https://example.com`
   - If that works, the issue is with your application's accessibility

### Generic Selectors in Generated Script

If the generated script contains generic selectors like `input[type="text"]` instead of specific ones like `#username`, it means page inspection failed.

**Causes**:
- Playwright browsers are not installed
- Application URL is not accessible
- Page requires authentication
- Network timeout or connectivity issues

**Solution**: Follow the steps in "Page inspection failed" above.

## See Also

- [Playwright Generator Guide](PLAYWRIGHT_GENERATOR.md) - Standalone Playwright test generator
- [Troubleshooting Playwright](TROUBLESHOOTING_PLAYWRIGHT.md) - Common issues and solutions
- [Architecture](ARCHITECTURE.md) - System architecture overview
