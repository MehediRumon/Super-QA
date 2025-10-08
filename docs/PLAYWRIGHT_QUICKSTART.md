# Quick Start: Playwright Test Generator

Generate executable Playwright C# test scripts using AI in just a few steps!

## Prerequisites

- Super-QA application running (see [QUICKSTART.md](../QUICKSTART.md))
- OpenAI API key (get one at [platform.openai.com/api-keys](https://platform.openai.com/api-keys))

## Step-by-Step Guide

### 1. Get Your OpenAI API Key

1. Go to [OpenAI Platform](https://platform.openai.com/api-keys)
2. Sign in or create an account
3. Click "Create new secret key"
4. Copy the key (it starts with `sk-...`)
5. **Important**: Keep this key secure and never share it publicly

### 2. Navigate to the Generator

1. Open Super-QA at `https://localhost:5001`
2. Click **"Playwright Generator"** in the navigation menu

### 3. Fill in the Configuration

#### OpenAI API Key
Paste your API key from step 1

#### AI Model
Choose based on your needs:
- **GPT-4** (Recommended) - Best quality, most accurate
- **GPT-4 Turbo** - Faster, good balance
- **GPT-3.5 Turbo** - Fastest, economical

#### Application URL
Enter the URL you want to test, e.g., `https://example.com`

#### FRS (Functional Requirements)
Describe what you want to test. Be specific!

**Example:**
```
User Login Flow:
1. Navigate to the login page at /login
2. Find the email input field
3. Enter "user@example.com" 
4. Find the password input field
5. Enter "securePassword123"
6. Click the "Sign In" button
7. Verify successful login by checking for dashboard URL
8. Verify welcome message "Welcome back!" is displayed
```

### 4. Generate the Test

1. Click **"🤖 Generate Test Script"**
2. Wait 10-30 seconds for AI to generate the script
3. Review the generated C# code in the right panel

### 5. (Optional) Execute the Test

1. Click **"▶️ Execute Test"**
2. Wait for the test to complete
3. Review the results:
   - ✅ **Pass**: Test executed successfully
   - ❌ **Fail**: Test failed (check error message)
   - **Logs**: Detailed execution steps

### 6. Use the Generated Script

Click **"📋 Copy"** to copy the script, then:

1. Create a new test project:
```bash
dotnet new nunit -n MyTests
cd MyTests
dotnet add package Microsoft.Playwright.NUnit
```

2. Install Playwright:
```bash
playwright install chromium
```

3. Paste the generated code into a `.cs` file

4. Run the test:
```bash
dotnet test
```

## Example: Testing a Search Feature

### Input (FRS):
```
Search Functionality Test:
1. Go to https://www.google.com
2. Locate the search input box
3. Type "Playwright testing" in the search box
4. Press Enter to submit the search
5. Wait for search results to load
6. Verify that results page contains "Playwright"
7. Verify at least 5 search results are displayed
```

### Expected Output:
A complete C# Playwright test with:
- Proper using statements
- Test class with NUnit attributes
- Setup/teardown methods
- Test method with all steps
- Element locators
- Actions (navigate, type, click)
- Assertions (URL check, text verification, count validation)

## Tips for Best Results

### Writing Good FRS

✅ **DO:**
- Be specific about URLs and paths
- Mention exact element names/labels
- Describe expected outcomes clearly
- Use numbered steps
- Include both positive and negative scenarios

❌ **DON'T:**
- Be vague ("test the login")
- Skip expected results
- Use unclear references ("the button")
- Omit important context

### Model Selection Tips

- **Complex app with many edge cases?** → Use GPT-4
- **Standard web application?** → Use GPT-4 Turbo
- **Simple form validation?** → Use GPT-3.5 Turbo
- **First time/experimenting?** → Start with GPT-3.5 Turbo

### Cost Optimization

- Start with GPT-3.5 Turbo to test your FRS
- Refine the FRS based on results
- Use GPT-4 for final production-quality tests
- Monitor your OpenAI API usage dashboard

## Troubleshooting

### "Error: Invalid API Key"
→ Double-check your API key, ensure it starts with `sk-`

### "Generation taking too long"
→ This is normal for complex FRS. GPT-4 can take 20-30 seconds

### "Generated script has errors"
→ Try making your FRS more specific, or use GPT-4 instead of GPT-3.5

### "Test execution failed"
→ Check the logs for specific errors. The generated script might need minor adjustments for your specific application

## Security Notes

- ✅ Your API key is sent securely over HTTPS
- ✅ API key is NOT stored in the database
- ✅ API key is only used for the current request
- ⚠️ Set spending limits in your OpenAI dashboard
- ⚠️ Monitor your API usage regularly

## What's Next?

1. **Customize the Script**: Edit locators or assertions as needed
2. **Add to Your Project**: Integrate into your existing test suite
3. **Expand Coverage**: Generate tests for other features
4. **Automate**: Run generated tests in your CI/CD pipeline

## Need Help?

- 📖 Full documentation: [docs/PLAYWRIGHT_GENERATOR.md](PLAYWRIGHT_GENERATOR.md)
- 🐛 Issues: [GitHub Issues](https://github.com/MehediRumon/Super-QA/issues)
- 💡 Examples: See [docs/PLAYWRIGHT_GENERATOR.md](PLAYWRIGHT_GENERATOR.md#example-workflow)

---

**Happy Testing! 🎭✨**
