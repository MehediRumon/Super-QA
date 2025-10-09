# Playwright Test Generator - Troubleshooting Guide

## Issue: AI Test Generator Not Using Actual Page Elements

### Problem
The AI-powered Playwright test generator is creating test scripts with **generic/placeholder selectors** instead of **actual element selectors** from your application.

**Example of the issue:**
```csharp
// Generic selectors (not specific to your page)
await Page.FillAsync("input[name='username']", "testuser"); 
await Page.ClickAsync("button[type='submit']"); 
```

**Expected behavior:**
```csharp
// Actual selectors from your page
await Page.FillAsync("#loginUsername", "testuser"); 
await Page.ClickAsync("#submitButton"); 
```

### Root Cause
**Playwright browsers are not installed.** The page inspector service requires Playwright browsers to navigate to your application URL and extract actual element information.

### Solution

#### Option 1: Quick Setup (Recommended)
Run the installation script:

**Linux/macOS:**
```bash
cd /path/to/Super-QA
./scripts/install-playwright-browsers.sh
```

**Windows:**
```powershell
cd C:\path\to\Super-QA
.\scripts\install-playwright-browsers.ps1
```

#### Option 2: Manual Installation
```bash
# Install Playwright CLI globally
dotnet tool install --global Microsoft.Playwright.CLI

# Install Chromium browser
playwright install chromium
```

#### Option 3: Project-Specific Installation
```bash
# Build the project first
dotnet build

# Navigate to the Infrastructure project output
cd src/SuperQA.Infrastructure/bin/Debug/net9.0

# Install browsers using the generated script
pwsh playwright.ps1 install chromium
```

### Verification

After installation, verify browsers are installed:

```bash
playwright --version
ls ~/.cache/ms-playwright/
```

You should see browser directories like `chromium_headless_shell-1148`.

### How It Works

When you generate a test script, the system:

1. **Launches a headless browser** (Chromium)
2. **Navigates** to your application URL
3. **Inspects the page** and extracts all interactive elements:
   - Input fields (with ID, name, placeholder, type)
   - Buttons (with ID, name, text)
   - Links (with ID, text, href)
   - Textareas and dropdowns
4. **Sends actual element selectors** to the AI
5. **AI generates test scripts** using these real selectors

### Troubleshooting

#### Browser Installation Fails
If you get download errors:

1. Check your internet connection
2. Try using a different mirror:
   ```bash
   export PLAYWRIGHT_DOWNLOAD_HOST=https://playwright.azureedge.net
   playwright install chromium
   ```
3. Install with dependencies:
   ```bash
   playwright install chromium --with-deps
   ```

#### Browsers Installed But Still Getting Generic Selectors

1. **Check browser location:**
   ```bash
   ls -la ~/.cache/ms-playwright/
   ```

2. **Check application logs** when generating a test:
   - Look for "Page inspection failed" messages
   - Check if the application URL is accessible

3. **Verify URL accessibility:**
   - Make sure your application is running
   - Ensure the URL is reachable from the server
   - Check for any authentication requirements

4. **Review server logs:**
   ```bash
   cd src/SuperQA.Api
   dotnet run
   # Watch for inspection warnings in console
   ```

#### Network/Firewall Issues

If your application is behind a firewall or requires authentication:

1. The page inspector might not be able to access it
2. Consider temporarily using a public test page to verify the feature works
3. Or whitelist the server's IP address

### CI/CD Integration

For GitHub Actions or other CI/CD pipelines, add this step:

```yaml
- name: Install Playwright Browsers
  run: |
    dotnet tool install --global Microsoft.Playwright.CLI
    playwright install chromium --with-deps
```

### Expected Warnings

You may see this warning in the API response if page inspection fails:

> ⚠️ Page inspection failed. The AI will generate test scripts with generic selectors. For best results, ensure Playwright browsers are installed (run 'playwright install chromium').

This warning indicates the feature is working in fallback mode. Install browsers to get actual element selectors.

### Additional Resources

- [Playwright for .NET Documentation](https://playwright.dev/dotnet/)
- [Browser Installation Guide](https://playwright.dev/dotnet/docs/browsers)
- [Super-QA Playwright Generator Guide](../PLAYWRIGHT_GENERATOR.md)
- [Phase 2 Quick Start](../PHASE2_QUICKSTART.md)

### Still Having Issues?

1. Check that your .NET version is 9.0 or higher:
   ```bash
   dotnet --version
   ```

2. Verify Playwright package version:
   ```bash
   dotnet list src/SuperQA.Infrastructure package | grep Playwright
   ```

3. Review the full error message in server logs

4. Open an issue on GitHub with:
   - Operating system and version
   - .NET version
   - Playwright version
   - Full error message
   - Steps to reproduce
