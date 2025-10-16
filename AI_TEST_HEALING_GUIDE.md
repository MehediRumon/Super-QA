# AI Test Healing Feature

## Overview

The AI Test Healing feature provides intelligent, automated test script repair when tests fail. Using advanced AI analysis, it examines test failures, error messages, stack traces, and screenshots to generate improved, more resilient test scripts.

## Features

- **Automatic Failure Analysis**: AI analyzes error messages, stack traces, and failure context
- **Smart Selector Improvements**: Generates more robust element selectors (role+name, data-testid, IDs)
- **Timing Issue Resolution**: Adds proper wait strategies and element visibility checks
- **Error Handling**: Improves error handling and retry mechanisms
- **Self-Healing Scripts**: Creates test scripts that are more resilient to UI changes

## How to Use

### 1. Navigate to Test Executions

After running tests, navigate to the Test Executions page for your project.

### 2. Identify Failed Tests

Failed tests are marked with a red "Failed" badge in the status column.

### 3. Click "AI Heal" Button

For any failed test, click the **"AI Heal"** button (appears next to the Details button for failed tests).

### 4. Provide OpenAI API Key

In the healing dialog:
- Enter your OpenAI API Key (get one from [OpenAI Platform](https://platform.openai.com/api-keys))
- Select an AI model (GPT-4 recommended, GPT-4 Turbo, or GPT-3.5 Turbo)
- Click **"Heal Test"**

### 5. Review and Apply Healed Script

The AI will analyze the failure and generate an improved test script. You have two options:
- **Apply Healed Script** (Recommended): Click the "Apply Healed Script" button to automatically update the test case with the healed version
- **Copy Script**: Manually copy the improved script and update your test case

After applying the healed script, re-run the test to verify the fix works.

## What the AI Analyzes

The healing service examines:

1. **Original Test Information**
   - Test case title and description
   - Test steps and expected results
   - Original automation script (if exists)

2. **Failure Context**
   - Error message
   - Stack trace
   - Screenshot (if captured)

3. **Common Issues**
   - Selector problems (element not found, changed selectors)
   - Timing issues (elements not ready, async operations)
   - Navigation issues (page not loaded, redirects)
   - Data issues (incorrect test data, validation failures)

## Healing Improvements

The AI generates scripts with:

- **Robust Selectors**: Prefers role+name, data-testid, and IDs over fragile CSS selectors
- **Explicit Waits**: Adds proper wait strategies for elements to be ready
- **Error Handling**: Improves exception handling and validation
- **Retry Logic**: Adds retry mechanisms where appropriate
- **Best Practices**: Follows Playwright and modern test automation standards

## API Endpoints

### Heal Test Execution

**POST** `/api/testexecutions/heal`

**Request Body:**
```json
{
  "testCaseId": 1,
  "executionId": 2,
  "apiKey": "sk-...",
  "model": "gpt-4"
}
```

**Response:**
```json
{
  "healedScript": "// Improved test script...",
  "message": "Test script healed successfully. Review and update your test case with the improved script."
}
```

**Error Responses:**
- `404 Not Found`: Test case or execution not found
- `400 Bad Request`: Cannot heal non-failed executions
- `502 Bad Gateway`: OpenAI API error
- `500 Internal Server Error`: Other errors

### Apply Healed Script

**POST** `/api/testexecutions/apply-healed-script`

**Request Body:**
```json
{
  "testCaseId": 1,
  "healedScript": "// Healed test script..."
}
```

**Response:**
```json
{
  "success": true,
  "message": "Healed script applied successfully to the test case."
}
```

**Error Responses:**
- `404 Not Found`: Test case not found
- `500 Internal Server Error`: Error applying healed script

## Security & Privacy

- API keys are **never stored** - they are used only for the healing request
- All healing requests are processed server-side
- OpenAI API calls use HTTPS encryption
- Healing is performed on-demand, not automatically

## Supported Models

- **GPT-4** (Recommended): Best quality healing with deep analysis
- **GPT-4 Turbo**: Fast healing with good quality
- **GPT-3.5 Turbo**: Quick healing for simple issues

## Example Use Case

**Before Healing:**
```csharp
// Test fails with "Element not found: #submit-button"
await Page.ClickAsync("#submit-button");
```

**After AI Healing:**
```csharp
// Healed script with robust selector and wait
await Page.GetByRole(AriaRole.Button, new() { Name = "Submit" }).ClickAsync();
```

## Limitations

- Healing is a **suggestion** - always review the healed script before using it
- Complex test logic may require manual intervention
- AI cannot fix infrastructure issues (network, database, etc.)
- Requires valid OpenAI API key and sufficient credits

## Best Practices

1. **Review Before Applying**: Always review healed scripts before updating test cases
2. **Use GPT-4**: For best results, use GPT-4 model (though more expensive)
3. **Iterative Healing**: If first healing doesn't work, try again with additional context
4. **Combine with Manual Review**: Use AI healing as a starting point, refine manually
5. **Keep API Keys Secure**: Never commit API keys to source control

## Troubleshooting

### "Invalid API Key" Error
- Verify your OpenAI API key is correct
- Check if the key has the necessary permissions
- Ensure the key hasn't expired

### "Rate Limit Exceeded" Error
- Wait a few minutes before trying again
- Check your OpenAI usage quota
- Consider upgrading your OpenAI plan

### "OpenAI Service Error" or "Service Unavailable"
- OpenAI may be experiencing temporary issues
- Try again in a few minutes
- Check [OpenAI Status](https://status.openai.com/)

### Healed Script Still Fails
- Review the healed script for logical errors
- The AI may need more context - add detailed test descriptions
- Some issues may require manual debugging
- Try healing again with a different model

## Related Features

- [Test Execution](../PHASE2_QUICKSTART.md) - Run and monitor test executions
- [Playwright Generator](../docs/PLAYWRIGHT_GENERATOR.md) - Generate test scripts from requirements
- [Saved Test Scripts](../SAVED_TEST_SCRIPTS_FEATURE.md) - Manage AI-generated test scripts

## Feedback

If you encounter issues or have suggestions for improving the AI healing feature, please open an issue on GitHub.
