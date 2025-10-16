# Browser Extension Connection Error Fix

## Overview
This document describes the fix for the connection error that users encountered when trying to send test data from the browser extension to SuperQA while the API server was not running.

## Problem Statement
Users received an unhelpful error message when the SuperQA API server was not running:

```
Error: Failed to fetch
```

Console error:
```
POST http://localhost:7000/api/playwright/store-extension-data net::ERR_CONNECTION_REFUSED
```

## Root Cause
The browser extension's error handling in `view.js` was catching the connection error but only displaying the generic error message without providing context or guidance.

## Solution
Improved the error handling to:
1. Detect connection failures specifically (`Failed to fetch` or `TypeError`)
2. Display a clear, user-friendly error message
3. Provide actionable instructions on how to resolve the issue

## Updated Error Message
When the API server is not running, users now see:

```
❌ Cannot connect to SuperQA API server.
Please ensure the API is running on port 7000.
Run: cd src/SuperQA.Api && dotnet run
```

## How to Use the Extension

### Prerequisites
Before using the "Send to SuperQA" feature, ensure the API server is running:

1. Open a terminal
2. Navigate to the API directory:
   ```bash
   cd src/SuperQA.Api
   ```
3. Run the API server:
   ```bash
   dotnet run
   ```
4. Wait for the message: `Now listening on: http://localhost:7000`

### Using the Extension
1. Navigate to the web page you want to test
2. Interact with the page (clicks, form fills, etc.)
3. The extension automatically records your actions as Gherkin steps
4. Enter a test name in the extension popup
5. Click "📤 Send to SuperQA"
6. If successful, SuperQA will open in a new tab at `http://localhost:5000/extension-test-review`

## Troubleshooting

### Error: Cannot connect to SuperQA API server
**Cause**: The API server is not running on port 7000

**Solution**:
```bash
cd src/SuperQA.Api
dotnet run
```

### Error: Port 7000 is already in use
**Cause**: Another application is using port 7000

**Solution**:
1. Find and stop the process using port 7000, or
2. Modify `launchSettings.json` to use a different port

### Error: Failed to store data
**Cause**: The API server is running but returned an error

**Solution**:
1. Check the API server logs for details
2. Ensure the database is accessible
3. Verify the request payload is valid

## Technical Details

### Code Changes
File: `Test-Case-and-Selector-Generator-Extension/view.js`

```javascript
// Before
catch (error) {
    console.error('Error opening SuperQA:', error);
    sendStatus.textContent = `❌ Error: ${error.message}`;
    sendStatus.style.color = '#ffcccb';
}

// After
catch (error) {
    console.error('Error opening SuperQA:', error);
    
    // Check if the error is a connection refused error
    if (error.message === 'Failed to fetch' || error.name === 'TypeError') {
        sendStatus.innerHTML = '❌ Cannot connect to SuperQA API server.<br>' +
                              'Please ensure the API is running on port 7000.<br>' +
                              '<small>Run: <code>cd src/SuperQA.Api && dotnet run</code></small>';
    } else {
        sendStatus.textContent = `❌ Error: ${error.message}`;
    }
    sendStatus.style.color = '#ffcccb';
}
```

### Architecture
- **Extension**: Runs in the browser, collects user actions
- **API Server**: Runs on `http://localhost:7000`, stores test data
- **UI Server**: Runs on `http://localhost:5000`, displays SuperQA interface

### Data Flow
1. Extension collects Gherkin steps
2. User clicks "Send to SuperQA"
3. Extension POSTs to `http://localhost:7000/api/playwright/store-extension-data`
4. API stores data and returns a data ID
5. Extension opens `http://localhost:5000/extension-test-review?dataId={id}`
6. UI retrieves data from API and displays it

## Related Documentation
- [Extension Quick Start Guide](./QUICK_START_GUIDE.md)
- [Extension Integration Guide](../EXTENSION_INTEGRATION_GUIDE.md)
- [Main README](../README.md)

## Support
If you continue to experience issues:
1. Check the browser console for detailed error messages
2. Check the API server logs
3. Verify both servers are running on the correct ports
4. Open an issue on GitHub with error details
