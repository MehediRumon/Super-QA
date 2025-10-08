# Phase 2: Test Automation Quick Start Guide

This guide will help you get started with the automated test execution features introduced in Phase 2.

## 🚀 Prerequisites

Before you can execute automated tests, you need to install Playwright browsers:

### Windows (PowerShell)
```powershell
cd src/SuperQA.Infrastructure/bin/Debug/net9.0
.\playwright.ps1 install chromium
```

### Linux/macOS
```bash
cd src/SuperQA.Infrastructure/bin/Debug/net9.0
pwsh bin/Debug/net9.0/playwright.ps1 install chromium
# Or use the Playwright CLI
dotnet tool install --global Microsoft.Playwright.CLI
playwright install chromium
```

## 📝 Creating Executable Test Cases

For test cases to be executed automatically, they need to follow a specific format in the **Steps** field:

### Supported Step Commands

1. **Navigation**
   ```
   Navigate to https://example.com
   ```

2. **Click Actions**
   ```
   Click on "#submit-button"
   Click on "button.login"
   ```

3. **Form Input**
   ```
   Type "john@example.com" in "#email"
   Enter "password123" in "#password"
   ```

4. **Assertions/Verification**
   ```
   Verify "Welcome" appears on page
   Assert "Success" is visible
   ```

### Example Test Case

**Title:** Login Test

**Preconditions:** User account exists

**Steps:**
```
Navigate to https://example.com/login
Type "john@example.com" in "#email"
Type "password123" in "#password"
Click on "#login-button"
```

**Expected Results:**
```
Welcome Dashboard
```

## 🎮 Using the Test Execution Features

### 1. View Test Executions

1. Navigate to a project in the application
2. Click the **"Test Executions"** button (top-right)
3. You'll see the Test Executions page with:
   - Execution summary (passed/failed/running counts)
   - Execution history table
   - "Run All Tests" button

### 2. Run a Single Test

**Via API:**
```bash
curl -X POST https://localhost:7001/api/testexecutions/execute \
  -H "Content-Type: application/json" \
  -d '{
    "testCaseId": 1,
    "baseUrl": "https://example.com"
  }'
```

**Response:**
```json
5  # Execution ID
```

### 3. Run All Tests for a Project

**Via UI:**
1. Go to Test Executions page
2. Click **"Run All Tests"** button
3. Tests will run in background
4. Status updates automatically every 2 seconds
5. Execution results appear in the table

**Via API:**
```bash
curl -X POST https://localhost:7001/api/testexecutions/project/1/run-all
```

**Response:**
```json
{
  "message": "Test execution started in background"
}
```

### 4. Check Test Run Status

**Via API:**
```bash
curl https://localhost:7001/api/testexecutions/project/1/status
```

**Response:**
```json
{
  "status": "Running"  // or "Completed", "Failed", "Not Started"
}
```

### 5. View Execution Results

**Via UI:**
1. Click **"Details"** button on any execution row
2. Modal displays:
   - Execution status
   - Duration
   - Error messages (if failed)
   - Stack trace (if failed)
   - Screenshot (if failed)

**Via API:**
```bash
# Get all executions for a project
curl https://localhost:7001/api/testexecutions/project/1

# Get specific execution details
curl https://localhost:7001/api/testexecutions/5
```

## 📸 Screenshot Capture

When a test fails, a screenshot is automatically captured and stored as a Base64 string. You can view it in the execution details modal.

**Screenshot Storage:**
- Stored in the `TestExecution.Screenshot` field (Base64 encoded PNG)
- Displayed in UI using data URI: `data:image/png;base64,...`
- Can be decoded and saved if needed

## 🔧 Troubleshooting

### Tests Not Running

1. **Check Playwright Installation:**
   ```bash
   # Verify chromium is installed
   playwright install --dry-run chromium
   ```

2. **Check Test Case Format:**
   - Ensure steps use supported command syntax
   - Verify selectors are valid CSS selectors
   - Check that URLs are accessible

3. **Check Logs:**
   - API logs: Check console output from `SuperQA.Api`
   - Database: Check `TestExecutions` table for error messages

### Browser Installation Issues

If Playwright can't install browsers automatically, you can manually install:

```bash
# Install Playwright CLI globally
dotnet tool install --global Microsoft.Playwright.CLI

# Install browsers
playwright install chromium
```

### Execution Timing Out

If tests are timing out:
- Check that the target website is accessible
- Verify network connectivity
- Consider increasing timeout in `TestExecutionService.cs`

## 🎯 Best Practices

1. **Keep Test Steps Simple**
   - One action per step
   - Use clear, descriptive commands
   - Avoid complex logic

2. **Use Reliable Selectors**
   - Prefer IDs over classes: `#login-button` vs `.btn-login`
   - Use stable selectors that won't change frequently
   - Test selectors in browser DevTools first

3. **Add Meaningful Assertions**
   - Verify expected results
   - Check for success messages
   - Validate page transitions

4. **Handle Test Data**
   - Use test accounts with known credentials
   - Reset data between test runs if needed
   - Consider using a test database

5. **Monitor Execution Times**
   - Review duration metrics
   - Optimize slow tests
   - Set realistic expectations

## 📊 Understanding Results

### Test Statuses

- **Passed** (Green): Test completed successfully
- **Failed** (Red): Test encountered an error
- **Running** (Yellow): Test is currently executing

### Execution Metrics

- **Duration**: Time in milliseconds
- **Executed At**: Timestamp of execution
- **Error Message**: High-level error description
- **Stack Trace**: Detailed error information

## 🔄 Next Steps

With Phase 2 complete, you can:
1. Create comprehensive test suites
2. Run tests regularly (manually or on schedule)
3. Track test history and trends
4. Identify failing tests quickly with screenshots

**Coming in Phase 3:** AI-powered log analysis, automated bug reporting, and root cause analysis!

## 💡 Tips

- Start with simple navigation tests
- Gradually add more complex interactions
- Review screenshots to debug failures
- Keep test data consistent
- Document special setup requirements

## 📞 Need Help?

If you encounter issues:
1. Check this guide first
2. Review execution error messages
3. Verify Playwright installation
4. Open a GitHub issue with details
