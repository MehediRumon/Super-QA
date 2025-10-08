# Playwright Test Generator Guide

## Overview

The Playwright Test Generator is an AI-powered feature that enables users to generate executable Playwright test scripts in C# directly from Functional Requirement Specifications (FRS). This feature leverages OpenAI's GPT models to automatically create test scripts with locators, actions, and assertions.

## Features

- **OpenAI Integration**: Uses OpenAI's GPT models (GPT-4, GPT-4 Turbo, or GPT-3.5 Turbo) to generate intelligent test scripts
- **Secure API Key Handling**: API keys are not stored and only used for the current request
- **FRS to Test Script**: Converts functional requirements into executable Playwright C# test scripts
- **Automatic Page Inspection**: Automatically inspects the target application URL to detect actual page elements and their selectors
- **Real Element Selectors**: Uses actual element IDs, names, types, and classes from the live page instead of placeholder selectors
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
- **GPT-4o Mini (Recommended)**: Best balance of quality, speed and cost
- **GPT-4o**: Latest and most capable model
- **GPT-4 Turbo**: Fast and cost-effective
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
2. The system will automatically:
   - Navigate to the application URL using Playwright
   - Inspect the page to identify actual elements (inputs, buttons, links, etc.)
   - Extract real element selectors (IDs, names, types, classes)
   - Send this information to the AI along with your FRS
3. Wait for the AI to generate the test script (this may take 10-30 seconds)
4. The generated C# Playwright test script will appear in the right panel

**Note**: If the page inspection fails (e.g., URL is inaccessible), the AI will still generate a test script with best-effort selectors based on the FRS.

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
  "model": "gpt-4o-mini"
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

### How It Works

The Playwright Test Generator uses a two-step process to create accurate test scripts:

1. **Page Inspection**: 
   - When you click "Generate Test Script", the system first launches a Playwright browser
   - The browser can run in headless mode (invisible) or headed mode (visible) based on configuration
   - It navigates to your application URL and waits for the page to load
   - It extracts information about all interactive elements on the page:
     - Input fields (with their name, ID, type, placeholder, etc.)
     - Buttons (with their text, ID, name, etc.)
     - Links (with their text and href)
     - Textareas and select dropdowns
   - This information is structured as JSON with actual selectors

2. **AI Script Generation**:
   - The extracted page structure is sent to OpenAI along with your FRS
   - The AI receives **CRITICAL** instructions to use ONLY the actual selectors from the 'selector' field
   - Concrete examples in the prompt show exactly how to use these selectors
   - The AI is explicitly forbidden from inventing/guessing selectors or using generic ones
   - Instead of generic selectors like `input[name='username']`, it uses the exact selectors found on your page
   - The result is a test script with real, working selectors that match your application

**Benefits**:
- ✅ No need to manually inspect the page for element selectors
- ✅ Generated tests use actual selectors that exist on the page
- ✅ Reduces errors from placeholder or generic selectors
- ✅ Tests are more likely to work on first run
- ✅ 25% lower token costs with optimized prompt
- ✅ More deterministic, focused output with lower temperature setting

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

- **GPT-4o Mini (Recommended)**: Best default choice - excellent balance of speed, quality and cost. Optimized prompt uses ~25% fewer tokens.
- **GPT-4o**: Use for most complex applications or when highest accuracy is critical
- **GPT-4 Turbo**: Alternative high-quality option
- **GPT-3.5 Turbo**: Use for simple test scenarios or when cost is a primary concern

**Token Cost Optimization:**
The system has been optimized to reduce OpenAI API costs:
- Maximum response tokens reduced from 2000 to **1500** (25% reduction)
- Temperature lowered from 0.7 to **0.3** for more focused, deterministic output
- Streamlined prompt structure while maintaining clarity and effectiveness
- More explicit instructions prevent the AI from generating verbose unnecessary code

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
- Check browser configuration (headless/headed mode) in appsettings.json

### Browser Configuration

The Playwright browser can run in two modes:
- **Headless mode** (default in production): Browser runs invisibly in the background
- **Headed mode** (default in development): Browser window opens visually so you can watch tests execute

To configure browser visibility, update the `appsettings.json` or `appsettings.Development.json` file:

```json
{
  "Playwright": {
    "Headless": false  // false = browser visible, true = browser invisible
  }
}
```

**Default settings:**
- Development (`appsettings.Development.json`): `Headless: false` (browser visible)
- Production (`appsettings.json`): `Headless: true` (browser invisible)

**When to use headed mode:**
- Debugging test scripts
- Watching test execution in real-time
- Developing new test scenarios
- Demonstrating test automation to stakeholders

**When to use headless mode:**
- Production environments
- CI/CD pipelines
- Automated test runs
- Better performance and resource usage

### Script doesn't work as expected
- Review and refine your FRS to be more specific
- Try using GPT-4o or GPT-4 Turbo for better results
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
