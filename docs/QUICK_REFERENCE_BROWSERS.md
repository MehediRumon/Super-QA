# Quick Reference: Playwright Browser Installation

## 🚨 Do I need to install Playwright browsers?

**YES**, if you want the AI to generate test scripts with **actual element selectors** from your application.

## ✅ Quick Setup

### Linux / macOS
```bash
cd /path/to/Super-QA
./scripts/install-playwright-browsers.sh
```

### Windows
```powershell
cd C:\path\to\Super-QA
.\scripts\install-playwright-browsers.ps1
```

### Manual Installation
```bash
dotnet tool install --global Microsoft.Playwright.CLI
playwright install chromium
```

## 📊 Before vs After

### ❌ Without Browsers (Generic Selectors)
```csharp
// AI has to guess what selectors might exist
await Page.FillAsync("input[name='username']", "testuser");
await Page.FillAsync("input[type='password']", "password123");
await Page.ClickAsync("button[type='submit']");
```

### ✅ With Browsers (Actual Selectors)
```csharp
// AI uses your actual page elements
await Page.FillAsync("#loginUsername", "testuser");
await Page.FillAsync("#loginPassword", "password123");
await Page.ClickAsync("#submitLoginBtn");
```

## 🔍 How to Tell if Browsers are Installed

### Check Installation
```bash
playwright --version
ls ~/.cache/ms-playwright/
```

### Expected Output
```
chromium_headless_shell-1148/
└── chrome-linux/
    ├── chrome
    ├── headless_shell
    └── ...
```

### Test in Application
When generating a test script, if you see this warning in the API response:

> ⚠️ Page inspection failed. The AI will generate test scripts with generic selectors. For best results, ensure Playwright browsers are installed (run 'playwright install chromium').

This means browsers are **not installed** or **not accessible**.

## 🎯 What Happens When Browsers are Missing?

1. The PageInspectorService fails to launch a browser
2. Returns an error JSON instead of page elements
3. The AI doesn't receive actual element information
4. Generated scripts use generic/guessed selectors
5. You see a warning in the API response

**The system still works**, but tests may need manual adjustment.

## 💡 Troubleshooting

### Installation fails with download error
```bash
# Try alternative download host
export PLAYWRIGHT_DOWNLOAD_HOST=https://playwright.azureedge.net
playwright install chromium --with-deps
```

### "playwright: command not found"
```bash
# Add to PATH
export PATH="$PATH:$HOME/.dotnet/tools"

# Then try again
playwright install chromium
```

### Browsers installed but still getting generic selectors
1. Check server logs for "Page inspection failed" messages
2. Verify application URL is accessible from the server
3. Ensure no firewall blocking page access
4. Check browser permissions

### CI/CD Environment
See [CI_CD_SETUP.md](CI_CD_SETUP.md) for GitHub Actions, Azure DevOps, GitLab CI, and Docker configurations.

## 📖 More Information

- [Troubleshooting Guide](TROUBLESHOOTING_PLAYWRIGHT.md) - Detailed troubleshooting steps
- [Playwright Generator Guide](PLAYWRIGHT_GENERATOR.md) - How the feature works
- [CI/CD Setup](CI_CD_SETUP.md) - Configuration for automation pipelines
- [Phase 2 Quickstart](../PHASE2_QUICKSTART.md) - Complete automation guide

## 🎓 Understanding the Feature

When you click "Generate Test Script":

1. **Page Inspector launches** → Playwright browser opens (headless)
2. **Navigates to your URL** → Loads your application  
3. **Extracts elements** → Finds all inputs, buttons, links, etc.
4. **Captures selectors** → Gets IDs, names, classes, placeholders
5. **Sends to AI** → OpenAI receives actual page structure
6. **AI generates code** → Uses your real selectors

Without browsers: Steps 1-4 fail, AI gets no page info, generates generic code.

## ⚡ Quick Commands

```bash
# Install browsers
dotnet tool install --global Microsoft.Playwright.CLI
playwright install chromium

# Verify installation  
playwright --version
ls ~/.cache/ms-playwright/

# Run using installation script
./scripts/install-playwright-browsers.sh

# Check if your app can use them
cd src/SuperQA.Api
dotnet run
# Try generating a test script - should not show warnings
```

## 🆘 Still Need Help?

1. Review [TROUBLESHOOTING_PLAYWRIGHT.md](TROUBLESHOOTING_PLAYWRIGHT.md)
2. Check GitHub Issues
3. Open a new issue with:
   - OS and version
   - .NET version (`dotnet --version`)
   - Playwright version (`playwright --version`)
   - Error messages from logs
   - Steps to reproduce
