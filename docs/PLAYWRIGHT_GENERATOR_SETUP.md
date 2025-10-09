# AI-Powered Playwright Test Generator - Complete Setup Guide

## Overview

This guide explains how to set up and verify the AI-Powered Playwright Test Generator feature, which automatically generates executable C# Playwright test scripts with **actual element selectors** from your web application.

## How It Works

When you provide:
1. **FRS (Functional Requirements Specification)** - describing what to test
2. **Application URL** - the web page to test
3. **OpenAI API Key** - for AI generation

The system will:
1. ✅ Launch a headless browser and navigate to your URL
2. ✅ Inspect the page and extract all interactive elements (inputs, buttons, links, etc.)
3. ✅ Send the actual page structure + your FRS to OpenAI
4. ✅ Generate a C# Playwright test script using **real selectors from your page**

## Prerequisites

### 1. .NET 9.0 SDK
```bash
dotnet --version  # Should be 9.0.x or higher
```

### 2. Playwright Browsers (CRITICAL!)

**Without Playwright browsers:**
- ❌ Page inspection will fail
- ❌ AI will generate **generic/placeholder selectors**
- ❌ Tests will likely need manual adjustment

**With Playwright browsers:**
- ✅ Page inspection succeeds
- ✅ AI uses **actual selectors** from your page
- ✅ Tests work immediately without modification

## Installation Steps

### Step 1: Build the Project

```bash
cd /path/to/Super-QA
dotnet build
```

### Step 2: Install Playwright Browsers

**Option A: Using Installation Scripts (Recommended)**

Linux/macOS:
```bash
./scripts/install-playwright-browsers.sh
```

Windows:
```powershell
.\scripts\install-playwright-browsers.ps1
```

**Option B: Manual Installation**

```bash
# Install Playwright CLI
dotnet tool install --global Microsoft.Playwright.CLI

# Add to PATH (Linux/macOS)
export PATH="$PATH:$HOME/.dotnet/tools"

# Add to PATH (Windows PowerShell)
$env:PATH += ";$env:USERPROFILE\.dotnet\tools"

# Install Chromium browser
playwright install chromium
```

### Step 3: Verify Installation

**Critical: Always verify that browsers were actually installed!**

Linux/macOS:
```bash
playwright --version
ls -la ~/.cache/ms-playwright/
```

Windows:
```powershell
playwright --version
dir $env:USERPROFILE\.cache\ms-playwright\
```

**Expected output:**
- Playwright CLI version (e.g., `Version 1.50.0`)
- Directories like `chromium-1155/` containing browser binaries

**If the cache directory is empty or doesn't exist:**
- Installation failed (even if no error was shown)
- See [Troubleshooting Guide](TROUBLESHOOTING_PLAYWRIGHT.md)

### Step 4: Start the Application

```bash
# Terminal 1: Start API
cd src/SuperQA.Api
dotnet run

# Terminal 2: Start Frontend
cd src/SuperQA.Client
dotnet run
```

## Using the Feature

### 1. Navigate to Playwright Generator

Open your browser to the Blazor frontend (typically `https://localhost:5001`) and navigate to the Playwright Test Generator page.

### 2. Fill in the Form

**FRS (Functional Requirements):**
```
Test login functionality:
1. Navigate to the login page
2. Enter username "testuser"
3. Enter password "password123"
4. Click the login button
5. Verify the user is redirected to the dashboard
```

**Application URL:**
```
https://your-app.com/login
```

**OpenAI API Key:**
```
sk-...your-key...
```

**Model:** `gpt-4` (recommended) or `gpt-3.5-turbo`

### 3. Generate Test Script

Click "Generate Test Script" and wait for the response.

### 4. Check the Result

**✅ Success Indicators:**
```csharp
// Generated code uses ACTUAL selectors from your page:
await Page.FillAsync("#loginUsername", "testuser");      // Real ID
await Page.FillAsync("#loginPassword", "password123");   // Real ID  
await Page.ClickAsync("#submitLoginButton");             // Real ID
```

**❌ Failure Indicators (Browsers Not Installed):**
```csharp
// Generated code uses GENERIC selectors:
await Page.FillAsync("input[name='username']", "testuser");  // Generic
await Page.FillAsync("input[type='password']", "password123"); // Generic
await Page.ClickAsync("button[type='submit']");              // Generic
```

**Warning in API Response:**
```
⚠️ Page inspection failed. The AI will generate test scripts with generic selectors. 
For best results, ensure Playwright browsers are installed (run 'playwright install chromium').
```

## Verification Checklist

Use this checklist to verify everything is set up correctly:

- [ ] ✅ .NET 9.0 SDK installed
- [ ] ✅ Project builds successfully (`dotnet build`)
- [ ] ✅ Playwright CLI installed (`playwright --version` works)
- [ ] ✅ Browsers installed (`~/.cache/ms-playwright/` has chromium directories)
- [ ] ✅ API running (`dotnet run` in SuperQA.Api)
- [ ] ✅ Frontend running (`dotnet run` in SuperQA.Client)
- [ ] ✅ OpenAI API key obtained
- [ ] ✅ Test application is accessible from server

## Testing the Feature

### Test 1: Simple Public Page

To verify the feature works, test with a simple public page first:

**FRS:**
```
Test navigation:
1. Navigate to the example page
2. Verify the page title
3. Click the "More information" link
```

**Application URL:**
```
https://example.com
```

This should succeed even without your application running, proving that page inspection works.

### Test 2: Your Application

Once the public page test works, try your actual application.

## Expected Output Examples

### With Browsers Installed (Correct)

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
    public async Task LoginTest()
    {
        await Page.GotoAsync("https://your-app.com/login");
        
        // ACTUAL selectors from your page
        await Page.FillAsync("#loginUsername", "testuser");
        await Page.FillAsync("#loginPassword", "password123");
        await Page.ClickAsync("#submitLoginButton");
        
        await Expect(Page).ToHaveURLAsync("**/dashboard");
    }
}
```

### Without Browsers (Incorrect)

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
    public async Task LoginTest()
    {
        await Page.GotoAsync("https://your-app.com/login");
        
        // GENERIC selectors (likely won't work)
        await Page.FillAsync("input[name='username']", "testuser");
        await Page.FillAsync("input[type='password']", "password123");
        await Page.ClickAsync("button[type='submit']");
        
        await Expect(Page).ToHaveURLAsync("**/dashboard");
    }
}
```

## Common Issues and Solutions

### Issue: "Page inspection failed" Error

**Cause:** Playwright browsers not installed or not found

**Solution:**
1. Verify installation: `ls ~/.cache/ms-playwright/`
2. If empty, run installation again
3. Check [Troubleshooting Guide](TROUBLESHOOTING_PLAYWRIGHT.md)

### Issue: JavaScript Errors During Installation

**Cause:** Known bug in Playwright downloader progress display

**Solution:**
- Ignore the error messages
- Verify if browsers were installed anyway
- If not, try running installation command again

### Issue: Generic Selectors Despite Installation

**Possible Causes:**
1. Application URL is not accessible
2. Application requires authentication
3. Application is behind a firewall
4. Wrong Playwright version mismatch

**Solutions:**
1. Verify app is running and accessible
2. Test with a public URL first (e.g., `https://example.com`)
3. Check server console for specific error messages
4. Ensure Playwright package and CLI versions match

## CI/CD Setup

For automated environments (GitHub Actions, Azure DevOps, etc.):

```yaml
# GitHub Actions example
- name: Install Playwright Browsers
  run: |
    dotnet tool install --global Microsoft.Playwright.CLI
    playwright install chromium --with-deps
```

See [CI/CD Setup Guide](CI_CD_SETUP.md) for detailed examples.

## Support

If you encounter issues:

1. Check this setup guide carefully
2. Review the [Troubleshooting Guide](TROUBLESHOOTING_PLAYWRIGHT.md)
3. Verify each step in the verification checklist
4. Check server console logs for specific errors
5. Open a GitHub issue with:
   - Your OS and .NET version
   - Playwright version (`playwright --version`)
   - Browser installation status (`ls ~/.cache/ms-playwright/`)
   - Full error messages from server console

## Summary

The key to success:
- ✅ **MUST** install Playwright browsers before using the feature
- ✅ **MUST** verify installation succeeded
- ✅ **MUST** ensure application URL is accessible
- ✅ Generated tests with **actual selectors** = Success!
- ❌ Generic selectors = Browsers not installed or page inspection failed

Follow this guide step-by-step and you'll have working AI-generated test scripts with actual element selectors from your application!
