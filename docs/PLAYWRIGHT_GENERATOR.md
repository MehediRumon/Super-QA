# Playwright Test Generator Guide

## Overview

The Playwright Test Generator is an AI-powered feature that enables users to generate executable Playwright test scripts in C# directly from Functional Requirement Specifications (FRS). This feature leverages OpenAI's GPT models to automatically create test scripts with locators, actions, and assertions.

## Features

- **OpenAI Integration**: Uses OpenAI's GPT models (GPT-4, GPT-4 Turbo, or GPT-3.5 Turbo) to generate intelligent test scripts
- **Secure API Key Handling**: API keys are not stored and only used for the current request
- **FRS to Test Script**: Converts functional requirements into executable Playwright C# test scripts
- **Automatic Locator Detection**: Generated scripts include element locators from the target application
- **Action Implementation**: Generates code for actions like clicks, inputs, and navigation
- **Assertion Generation**: Creates assertions based on expected behavior from the FRS
- **Test Execution**: Ability to execute the generated test scripts and view results
- **Result Reporting**: Displays pass/fail status, logs, and error messages

## How to Use

### Step 1: Navigate to Playwright Generator

1. Open the Super-QA application
2. Click on **"Playwright Generator"** in the navigation menu
3. You'll see a two-panel interface:
   - Left panel: Configuration & Requirements
   - Right panel: Generated Test Script and Results

### Step 2: Configure Settings

Fill in the required fields in the Configuration panel:

#### OpenAI API Key *
- Enter your OpenAI API key (starts with `sk-...`)
- This key is **not stored** and is used only for this request
- Get your API key from [OpenAI Platform](https://platform.openai.com/api-keys)

#### AI Model
Select the OpenAI model to use:
- **GPT-4 (Recommended)**: Best quality, most accurate test generation
- **GPT-4 Turbo**: Faster and more cost-effective
- **GPT-3.5 Turbo**: Fastest and most economical option

#### Application URL *
- Enter the URL of the application you want to test
- Example: `https://example.com` or `http://localhost:3000`

#### Functional Requirement Specification (FRS) *
Enter your functional requirements. Example:

```
User Login Feature:
1. User should be able to navigate to the login page
2. User should see username and password fields
3. User should be able to enter credentials
4. Upon clicking Login, user should be authenticated
5. Successful login should redirect to dashboard
6. Failed login should display error message
```

### Step 3: Generate Test Script

1. Click the **"🤖 Generate Test Script"** button
2. Wait for the AI to generate the test script (this may take 10-30 seconds)
3. The generated C# Playwright test script will appear in the right panel

### Step 4: Review Generated Script

The generated script will include:
- **Using statements**: Required namespaces for Playwright and NUnit
- **Test class**: A complete test class with setup and teardown
- **Test methods**: One or more test methods based on your FRS
- **Locators**: Element selectors derived from the application
- **Actions**: Code to interact with the application (Navigate, Click, Fill, etc.)
- **Assertions**: Validation based on expected behavior

### Step 5: Execute Test (Optional)

1. Click the **"▶️ Execute Test"** button to run the generated test
2. Wait for execution to complete
3. View results in the "Test Execution Results" section:
   - **Status**: Pass/Fail indicator
   - **Execution Logs**: Detailed logs from the test execution process
   - **Test Output**: Console output from the test run
   - **Error Messages**: Any errors encountered during execution

### Step 6: Copy or Save Script

- Click the **"📋 Copy"** button to copy the script to clipboard
- You can then paste it into your test project

## Example Workflow

### Example FRS Input:

```
E-commerce Product Search:
1. Navigate to the homepage at https://example-store.com
2. Locate the search box in the header
3. Enter "laptop" in the search box
4. Click the search button
5. Verify that search results page loads
6. Verify that at least one product is displayed
7. Verify that product title contains "laptop"
```

### Example Generated Script:

```csharp
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace PlaywrightTests
{
    [TestFixture]
    public class ProductSearchTests : PageTest
    {
        [Test]
        public async Task TestProductSearch()
        {
            // Navigate to homepage
            await Page.GotoAsync("https://example-store.com");
            
            // Locate and fill search box
            var searchBox = Page.Locator("input[name='search']");
            await searchBox.FillAsync("laptop");
            
            // Click search button
            var searchButton = Page.Locator("button[type='submit']");
            await searchButton.ClickAsync();
            
            // Verify search results page loads
            await Expect(Page).ToHaveURLAsync(/.*search.*/);
            
            // Verify at least one product is displayed
            var products = Page.Locator(".product-item");
            await Expect(products).ToHaveCountAsync(1, new() { Timeout = 5000 });
            
            // Verify product title contains "laptop"
            var firstProduct = products.First();
            var productTitle = firstProduct.Locator(".product-title");
            await Expect(productTitle).ToContainTextAsync("laptop", 
                new() { IgnoreCase = true });
        }
    }
}
```

## API Endpoints

### Generate Test Script

**POST** `/api/Playwright/generate`

Request body:
```json
{
  "frsText": "User Login Feature: ...",
  "applicationUrl": "https://example.com",
  "openAIApiKey": "sk-...",
  "model": "gpt-4"
}
```

Response:
```json
{
  "success": true,
  "generatedScript": "using Microsoft.Playwright; ...",
  "errorMessage": null
}
```

### Execute Test Script

**POST** `/api/Playwright/execute`

Request body:
```json
{
  "testScript": "using Microsoft.Playwright; ...",
  "applicationUrl": "https://example.com"
}
```

Response:
```json
{
  "success": true,
  "status": "Pass",
  "output": "Test execution output...",
  "logs": ["Created temporary directory...", "Building test project..."],
  "errorMessage": null
}
```

## Best Practices

### Writing Good FRS

1. **Be Specific**: Clearly describe each step and expected outcome
2. **Include Context**: Mention specific URLs, element names, or text
3. **Expected Behavior**: Always state what should happen
4. **Logical Order**: Write steps in the order they should be executed
5. **Error Cases**: Include both positive and negative test scenarios

### Good FRS Example ✅
```
User Registration:
1. Navigate to https://app.example.com/register
2. Find the "Email" input field
3. Enter "test@example.com"
4. Find the "Password" input field
5. Enter a password with at least 8 characters
6. Click the "Register" button
7. Verify that a success message appears
8. Verify that user is redirected to /dashboard
```

### Poor FRS Example ❌
```
Test the registration page
Make sure it works
```

### Model Selection

- **GPT-4**: Use for complex applications or when accuracy is critical
- **GPT-4 Turbo**: Good balance of speed and quality for most use cases
- **GPT-3.5 Turbo**: Use for simple test scenarios or when cost is a concern

### Security Notes

- **API Key Security**: Your OpenAI API key is transmitted securely via HTTPS
- **Not Stored**: API keys are never stored in the database or logs
- **Session Only**: Keys are used only for the current request
- **Best Practice**: Use API keys with usage limits and monitor consumption

## Troubleshooting

### "Error generating test script"
- Verify your OpenAI API key is valid
- Check your OpenAI account has available credits
- Ensure you have internet connectivity

### "Test execution failed"
- Verify the generated script syntax is valid
- Check that the application URL is accessible
- Ensure Playwright is properly installed
- Review execution logs for specific errors

### Script doesn't work as expected
- Review and refine your FRS to be more specific
- Try using GPT-4 instead of GPT-3.5 for better results
- Manually adjust the generated script if needed

## Limitations

- **Internet Required**: Requires internet connection to call OpenAI API
- **OpenAI Account**: Requires a valid OpenAI account with API access
- **Cost**: Each generation consumes OpenAI API tokens (costs apply)
- **Execution Environment**: Test execution requires .NET SDK and Playwright
- **Best Effort**: Generated scripts may need manual refinement for complex scenarios

## Support

For issues or questions:
- Check the [OpenAPI documentation](https://localhost:7001/openapi/v1.json)
- Review the [Super-QA GitHub repository](https://github.com/MehediRumon/Super-QA)
- Open an issue on GitHub

## Future Enhancements

Planned improvements:
- Support for other testing frameworks (Selenium, Cypress)
- Test script templates and snippets
- Integration with version control
- Saved test suites
- Scheduled test execution
- Historical test results and analytics
