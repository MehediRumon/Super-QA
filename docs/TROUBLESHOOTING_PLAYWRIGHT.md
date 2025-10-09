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
**Playwright browsers are NOT installed or not found.** The page inspector service requires Playwright browsers to navigate to your application URL and extract actual element information.

### Solution

#### ⚠️ IMPORTANT: Do NOT rely on automatic installation

Previous versions attempted automatic browser installation, but this is **unreliable** due to:
- JavaScript errors in Playwright's download progress display
- Network/CDN issues causing download failures
- Process communication problems when calling from within applications

**You MUST manually install browsers before using this feature.**

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

**Note**: The script may show errors during download but browsers may still install successfully. Always verify installation (see below).

#### Option 2: Manual Installation

```bash
# 1. Install Playwright CLI globally
dotnet tool install --global Microsoft.Playwright.CLI

# 2. Add to PATH
export PATH="$PATH:$HOME/.dotnet/tools"  # Linux/macOS
# OR
$env:PATH += ";$env:USERPROFILE\.dotnet\tools"  # Windows PowerShell

# 3. Install Chromium browser
playwright install chromium

# 4. Verify installation
playwright --version
```

### Verification

**Always verify** that browsers were actually installed:

**Linux/macOS:**
```bash
ls -la ~/.cache/ms-playwright/
```

**Windows:**
```powershell
dir $env:USERPROFILE\.cache\ms-playwright\
```

You should see directories like `chromium-*` containing the browser binaries.

**If the directory is empty or doesn't exist:**
1. Installation failed despite any success messages
2. Try running the installation command again
3. Check your network connection
4. See "Common Installation Issues" below

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

#### Common Installation Issues

**1. JavaScript RangeError: Invalid count value: Infinity**

This is a known bug in Playwright's download progress display. The error is **cosmetic** and may not prevent installation.

**Solution:**
- Ignore the error and check if browsers were installed anyway
- Verify using: `ls ~/.cache/ms-playwright/` (Linux/macOS) or `dir $env:USERPROFILE\.cache\ms-playwright\` (Windows)
- If browsers are there, installation succeeded despite the error

**2. Download failed: size mismatch**

The Playwright downloader is having issues retrieving the correct file size from the CDN.

**Solutions:**
- Wait a few minutes and try again (CDN may be temporarily unavailable)
- Try a different download mirror:
  ```bash
  export PLAYWRIGHT_DOWNLOAD_HOST=https://playwright.azureedge.net
  playwright install chromium
  ```
- Install with dependencies (may help):
  ```bash
  playwright install chromium --with-deps
  ```

**3. "playwright: command not found" (Linux/macOS)**

The Playwright CLI is not in your PATH.

**Solution:**
```bash
export PATH="$PATH:$HOME/.dotnet/tools"
# Add to ~/.bashrc or ~/.zshrc to make permanent
echo 'export PATH="$PATH:$HOME/.dotnet/tools"' >> ~/.bashrc
```

**4. "playwright: command not found" (Windows)**

The Playwright CLI is not in your PATH.

**Solution (PowerShell):**
```powershell
$env:PATH += ";$env:USERPROFILE\.dotnet\tools"
# Add permanently via System Properties > Environment Variables
```

**5. Installation completes but browsers still not found**

The browsers may have been installed for a different Playwright version.

**Solution:**
1. Check which versions you have:
   ```bash
   playwright --version  # CLI version
   dotnet list package --include-transitive | grep Playwright  # Package version
   ```
2. Ensure they match or are compatible
3. Re-run installation after ensuring versions align

#### Browsers Installed But Still Getting Generic Selectors

1. **Check browser location:**
   ```bash
   ls -la ~/.cache/ms-playwright/
   ```
   
   Should show directories like `chromium-1155/` with browser binaries inside

2. **Check application logs** when generating a test:
   - Look for "Page inspection failed" messages
   - Check the error details for specific issues

3. **Verify URL accessibility:**
   - Make sure your application is running
   - Ensure the URL is reachable from the server running Super-QA
   - Check for any authentication requirements that might block access

4. **Review server console logs:**
   ```bash
   cd src/SuperQA.Api
   dotnet run
   # Watch for "Page inspection failed:" messages in console
   ```

5. **Test with a simple public page first:**
   - Try generating a test for a simple public webpage (e.g., `https://example.com`)
   - If that works, the issue is with accessing your application URL
   - If that fails too, browsers aren't properly installed

#### Network/Firewall Issues

If your application is behind a firewall or requires authentication:

1. The page inspector might not be able to access it
2. Consider temporarily using a public test page to verify the feature works
3. Or whitelist the server's IP address
4. For authenticated pages, consider using a test environment without authentication

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
