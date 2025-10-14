# Browser Extension Integration Guide

## Overview

This guide explains how to use the Browser Extension to send test cases to SuperQA and execute them with AI-generated Playwright scripts.

## Prerequisites

1. **SuperQA API Running**: The API must be running on `http://localhost:7001` or `https://localhost:7001`
   ```bash
   cd src/SuperQA.Api
   dotnet run
   ```

2. **SuperQA Client Running** (optional, for viewing tests): 
   ```bash
   cd src/SuperQA.Client
   dotnet run
   ```
   Access at: `http://localhost:5000` or `https://localhost:5001`

3. **OpenAI API Key Configured**: You need to configure your OpenAI API key in SuperQA Settings
   - Navigate to Settings in SuperQA
   - Enter your OpenAI API key
   - Select your preferred AI model (default: gpt-4o-mini)

## How to Send Tests from Extension to SuperQA

### Step 1: Record Test Steps in Extension

1. Install and open the Test Case and Selector Generator Extension
2. Record your test steps by clicking on elements on the web page
3. The extension will collect Gherkin-style steps with selectors

### Step 2: Send to SuperQA

1. Enter a test name in the "Test Name" field
2. Click the "🚀 Send to SuperQA" button
3. The extension will:
   - Send the test steps to SuperQA API endpoint: `/api/playwright/generate-from-extension`
   - SuperQA will automatically generate a Playwright test script using AI
   - Create a test case in the "Generated Tests" project
   - Open SuperQA in a new tab to view the test

### Troubleshooting "ERR_EMPTY_RESPONSE"

If you get `ERR_EMPTY_RESPONSE` error when sending to SuperQA:

1. **Make sure the API is running**:
   ```bash
   cd src/SuperQA.Api
   dotnet run
   ```
   You should see: `Now listening on: https://localhost:7001`

2. **Check if the API is accessible**:
   ```bash
   curl -k https://localhost:7001/api/playwright/generate-from-extension
   ```
   
3. **Verify CORS is configured**: The API should have CORS enabled for all origins (already configured)

4. **Check browser console**: Look for any additional error messages that might help diagnose the issue

## Using Generated Tests in SuperQA

### View Generated Tests

1. Open SuperQA at `http://localhost:5000` (or `https://localhost:5001`)
2. Navigate to Projects
3. Click on the "Generated Tests" project
4. Switch to "Test Cases" tab
5. You'll see all tests imported from the browser extension

### Test Case Actions

Each test case has different actions depending on whether it has an automation script:

#### If Test Has No Automation Script

- **🤖 Generate AI Script** button: Click to generate a Playwright automation script
  - Uses AI to convert test steps into executable Playwright code
  - May show warnings if page inspection fails (requires Playwright browsers installed)
  - Once generated, the button changes to "Run Test"

#### If Test Has Automation Script

- **▶️ Run Test** button: Execute the Playwright test script
  - Runs the test against the application URL specified in preconditions
  - Shows test results (Pass/Fail) with output
  - Displays any errors or logs from test execution

- **👁️ View Script** button: Display the generated Playwright code
  - Click to expand/collapse the script view
  - View the C# Playwright test code that will be executed

### Test Execution Results

After running a test, you'll see:
- ✅ Success message if test passed
- ❌ Error message if test failed
- Test output and logs
- Any warnings about page inspection or browser installation

## API Endpoint Details

### POST `/api/playwright/generate-from-extension`

Receives test steps from browser extension and generates Playwright test script.

**Request Body:**
```json
{
  "applicationUrl": "https://example.com",
  "testName": "Login Test",
  "steps": [
    {
      "action": "click",
      "locator": "xpath=//button[@id='login']",
      "value": "",
      "description": "Click on Login Button"
    },
    {
      "action": "fill",
      "locator": "id=username",
      "value": "testuser",
      "description": "Enter username"
    }
  ]
}
```

**Response:**
```json
{
  "success": true,
  "generatedScript": "using Microsoft.Playwright;...",
  "testCaseId": 123,
  "projectId": 45,
  "warnings": ["⚠️ Page inspection failed..."]
}
```

## Project Structure

Tests from the browser extension are automatically organized in:
- **Project Name**: "Generated Tests"
- **Project Description**: "Tests imported from browser extension"
- Each test case includes:
  - Title: The test name you provided
  - Description: Auto-generated with application URL
  - Preconditions: Navigate to URL
  - Steps: Formatted test steps
  - AutomationScript: AI-generated Playwright C# code

## Best Practices

1. **Use descriptive test names**: Makes it easier to find tests later
2. **Verify selectors**: Make sure the extension captures stable selectors
3. **Install Playwright browsers**: For better page inspection and test execution
   ```bash
   playwright install chromium
   ```
4. **Configure OpenAI API key**: Required for generating automation scripts
5. **Review generated scripts**: Always review AI-generated code before running tests

## Workflow Example

1. Record a login test in the browser extension
2. Click "🚀 Send to SuperQA" 
3. SuperQA opens automatically - navigate to "Generated Tests" project
4. View your test case with auto-generated Playwright script
5. Click "▶️ Run Test" to execute the test
6. View results and verify test passed

## Notes

- The browser extension automatically generates automation scripts when sending tests
- Most tests from the extension will already have scripts and show "Run Test" button
- You can manually regenerate scripts if needed using "Generate AI Script"
- Tests can be executed multiple times with different configurations
