# Browser Extension Connection Error Fix

## Overview
This document describes the fixes for connection errors that users encountered when trying to send test data from the browser extension to SuperQA.

## Problem Statements

### Issue 1: API Server Not Running
Users received an unhelpful error message when the SuperQA API server was not running:

```
Error: Failed to fetch
```

Console error:
```
POST http://localhost:7000/api/playwright/store-extension-data net::ERR_CONNECTION_REFUSED
```

### Issue 2: CORS/Extension Security Restrictions
Even when the API server was running, users still received connection refused errors. This was caused by browser extension security restrictions when trying to make cross-origin requests from extension pages.

## Root Causes

### Cause 1: Server Not Running
The browser extension's error handling in `view.js` was catching the connection error but only displaying the generic error message without providing context or guidance.

### Cause 2: Extension CORS Restrictions
When `view.html` is loaded via the `chrome-extension://` protocol and tries to make a direct fetch request to `http://localhost:7000`, it triggers CORS (Cross-Origin Resource Sharing) restrictions. Even though the API server has CORS configured to allow all origins (`AllowAnyOrigin()`), browser extensions have additional security layers that prevent direct cross-origin fetch requests from extension pages.

## Solutions

### Solution 1: Improved Error Messages
Enhanced error handling to:
1. Detect connection failures specifically (`Failed to fetch` or `TypeError`)
2. Display a clear, user-friendly error message
3. Provide actionable instructions on how to resolve the issue

Updated error message:
```
❌ Cannot connect to SuperQA API server.
Please ensure the API is running on port 7000.
Run: cd src/SuperQA.Api && dotnet run
```

### Solution 2: Background Service Worker for API Calls
Modified the extension architecture to route API calls through the background service worker:

1. **view.js**: Detects if running in extension context and uses `chrome.runtime.sendMessage()` to communicate with the background script instead of making direct fetch calls
2. **background.js**: Added message handler for `storeExtensionData` action that performs the fetch request from the background service worker context, which has different CORS permissions
3. **Fallback**: Maintains direct fetch capability for non-extension contexts (useful for testing)

#### Why Background Service Worker?
Background service workers in Chrome extensions have broader permissions and aren't subject to the same CORS restrictions as pages loaded from `chrome-extension://` URLs. By routing the fetch request through the service worker, we bypass the CORS issue entirely.

#### Data Flow
1. User clicks "Send to SuperQA" in view.html
2. view.js detects extension context (`chrome.runtime.id` exists)
3. Sends message to background.js via `chrome.runtime.sendMessage()`
4. background.js receives message and makes fetch to `http://localhost:7000`
5. API response is sent back to view.js via `sendResponse()`
6. view.js opens SuperQA UI with the data ID

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

### Error: Extension communication error
**Cause**: The extension's background service worker failed to communicate with the API

**Solution**:
1. Reload the extension: Chrome Extensions → Developer mode → Reload
2. Ensure the API server is running on port 7000
3. Check browser console for detailed error messages

### Error: Port 7000 is already in use
**Cause**: Another application is using port 7000

**Solution**:
1. Find and stop the process using port 7000, or
2. Modify `launchSettings.json` to use a different port (also update view.js and background.js)

### Error: Failed to store data
**Cause**: The API server is running but returned an error

**Solution**:
1. Check the API server logs for details
2. Ensure the database is accessible
3. Verify the request payload is valid

## Technical Details

### Code Changes

#### view.js
```javascript
// Before (direct fetch - causes CORS issues)
const response = await fetch('http://localhost:7000/api/playwright/store-extension-data', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload)
});

// After (routes through background service worker)
const isExtension = typeof chrome !== 'undefined' && chrome.runtime && chrome.runtime.id;

if (isExtension) {
    const result = await new Promise((resolve, reject) => {
        chrome.runtime.sendMessage({
            action: 'storeExtensionData',
            payload: payload
        }, (response) => {
            if (chrome.runtime.lastError) {
                reject(new Error(chrome.runtime.lastError.message));
            } else if (response && response.error) {
                reject(new Error(response.error));
            } else {
                resolve(response);
            }
        });
    });
}
```

#### background.js
```javascript
// New message handler
chrome.runtime.onMessage.addListener((message, sender, sendResponse) => {
    if (message.action === 'storeExtensionData') {
        const { payload } = message;
        
        fetch('http://localhost:7000/api/playwright/store-extension-data', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        })
        .then(response => response.json())
        .then(data => sendResponse({ dataId: data.dataId }))
        .catch(error => sendResponse({ error: error.message }));
        
        return true; // Keep message channel open for async response
    }
});
```

### Architecture
- **Extension**: Runs in the browser, collects user actions
- **API Server**: Runs on `http://localhost:7000`, stores test data
- **UI Server**: Runs on `http://localhost:5000`, displays SuperQA interface
- **Background Service Worker**: Handles API communication without CORS restrictions

### Security Considerations
- Background service worker has `host_permissions: ["<all_urls>"]` in manifest.json
- API server has CORS configured with `AllowAnyOrigin()` policy
- Extension can only make requests to localhost (hardcoded in code)
- No sensitive data is transmitted (only test steps and metadata)

## Related Documentation
- [Extension Quick Start Guide](./QUICK_START_GUIDE.md)
- [Extension Integration Guide](../EXTENSION_INTEGRATION_GUIDE.md)
- [Main README](../README.md)

## Support
If you continue to experience issues:
1. Check the browser console for detailed error messages (F12 → Console)
2. Check the extension's background service worker logs (chrome://extensions → Extension details → Service worker → Inspect)
3. Check the API server logs
4. Verify both servers are running on the correct ports
5. Open an issue on GitHub with error details and logs

