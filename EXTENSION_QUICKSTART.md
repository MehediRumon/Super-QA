# Quick Start Guide: Extension Test Review Feature

## How to Use the New Workflow

### For End Users

#### 1. Record Test Steps in Browser Extension
- Install the Super-QA browser extension
- Navigate to the web application you want to test
- Interact with the page (click buttons, fill forms, etc.)
- The extension automatically records each action as a Gherkin step with locator

Example recorded steps:
```
Enter Username "testuser" (xpath=//input[@id='username'])
Enter Password "testpass" (xpath=//input[@id='password'])
Click on Login Button (xpath=//button[@id='login-btn'])
```

#### 2. Send Steps to SuperQA
- Enter a meaningful **Test Name** (e.g., "User Login Test")
- Click the **"📤 Send to SuperQA"** button
- The extension will:
  - Store your recorded steps on the server
  - Open SuperQA in a new tab automatically

#### 3. Review and Edit on SuperQA
The SuperQA Review Page opens with:

**Test Name:** Pre-filled, but you can edit it
```
User Login Test
```

**Application URL:** Pre-filled with the page you were testing, but **you can edit it**
```
https://example.com/login
```

**Recorded Gherkin Steps:** Read-only display of your recorded steps
```
Enter Username "testuser" (xpath=//input[@id='username'])
Enter Password "testpass" (xpath=//input[@id='password'])
Click on Login Button (xpath=//button[@id='login-btn'])
```

#### 4. Generate Test Script
- Review the information
- Edit the Application URL if needed (e.g., change from dev to staging URL)
- Click **"Generate Test Script"** button
- Wait for AI to generate your Playwright test script

#### 5. Execute or Save
- Copy the generated test script
- Click **"Execute Test"** to run it immediately
- Or save it to your test suite

### Example Complete Flow

```
1. Extension → Record steps on https://dev.example.com/login
   ├─ Enter Username "admin" (xpath=//input[@id='user'])
   ├─ Enter Password "pass123" (xpath=//input[@id='pass'])
   └─ Click on Login (xpath=//button[@type='submit'])

2. Extension → Click "Send to SuperQA"
   ├─ Data stored on server (10-minute cache)
   └─ Opens review page: /extension-test-review?dataId=abc-123

3. Review Page → Edit URL to https://staging.example.com/login
   └─ Click "Generate Test Script"

4. AI → Generates Playwright C# test script using:
   ├─ Application URL: https://staging.example.com/login
   ├─ Recorded steps with locators
   └─ Page inspection (if available)

5. User → Executes or saves the generated test
```

## Key Benefits

### 1. **Full Control**
- You see exactly what will be sent to AI
- You can edit the Application URL before generation
- No surprises!

### 2. **Flexibility**
- Record on dev environment
- Generate test for staging environment
- Just edit the URL!

### 3. **Transparency**
- See all recorded steps
- Know which locators will be used
- Understand the test before it's generated

### 4. **Security**
- Data stored temporarily (10 minutes max)
- One-time use (removed after retrieval)
- No persistent storage of sensitive info

## Troubleshooting

### "Failed to load extension data" Error
**Cause:** The data link has expired (> 10 minutes old) or was already used

**Solution:** 
1. Go back to the browser extension
2. Click "Send to SuperQA" again
3. This creates a fresh data link

### Extension Opens Wrong URL
**Cause:** The extension detected a different URL from your test page

**Solution:**
1. On the review page, manually edit the Application URL field
2. Enter the correct URL
3. Click "Generate Test Script"

### Certificate/HTTPS Errors in Development
**Cause:** Development SSL certificate not trusted

**Solution:**
1. Navigate to https://localhost:7001 in your browser
2. Accept the certificate warning
3. Go back to the extension and try again

## Advanced Tips

### Testing Different Environments
Record once, test everywhere:
```
1. Record steps on DEV: https://dev.example.com
2. Review page → Change URL to STAGING: https://staging.example.com
3. Generate test for staging
4. Repeat for PROD: https://prod.example.com
```

### Editing Application URL Patterns
You can change any part of the URL:
- Protocol: `https://` → `http://`
- Subdomain: `dev.` → `staging.`
- Port: `:3000` → `:8080`
- Path: `/v1/login` → `/v2/login`

### Data Expiration
- Data expires after **10 minutes**
- Data is **one-time use** (removed after retrieval)
- If you need the same test again, use "Send to SuperQA" again

## API Reference (For Developers)

### Store Extension Data
```http
POST /api/playwright/store-extension-data
Content-Type: application/json

{
  "testName": "User Login Test",
  "applicationUrl": "https://example.com/login",
  "steps": [...]
}

Response:
{
  "dataId": "abc-123-def-456",
  "message": "Data stored successfully"
}
```

### Retrieve Extension Data
```http
GET /api/playwright/get-extension-data/{dataId}

Response:
{
  "testName": "User Login Test",
  "applicationUrl": "https://example.com/login",
  "steps": [...],
  "openAIApiKey": null,
  "model": null
}
```

Note: Data is removed from cache after retrieval (one-time use).

## Related Documentation
- See `EXTENSION_REVIEW_FEATURE.md` for technical details
- See `docs/extension-flow-demo.html` for interactive demo
