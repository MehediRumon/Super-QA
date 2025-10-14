# Extension Test Review Feature

## Overview

This feature implements a menu-based workflow for generating Playwright tests from browser extension data, as requested in the issue.

## New Workflow

### Before (Direct AI Generation)
1. User records steps in browser extension
2. User clicks "Send to SuperQA" 
3. Extension sends data directly to AI to generate test
4. Test is generated immediately

### After (Review-First Approach)
1. User records steps in browser extension
2. User clicks "Send to SuperQA"
3. Extension stores data on server and opens Review page
4. User reviews:
   - Test Name
   - Application URL (can be edited)
   - Generated Gherkin Steps with Locators (displayed in textarea)
5. User clicks "Generate Test Script" button
6. AI generates Playwright test script with proper prompt
7. User can run the test script

## Implementation Details

### New Components

1. **ExtensionTestReview.razor** - New page for reviewing and generating tests
   - URL: `/extension-test-review?dataId={guid}`
   - Features:
     - Test Name input field
     - Application URL input field (editable)
     - Readonly textarea showing recorded Gherkin steps with locators
     - Generate Test Script button
     - Generated test script display
     - Execute test functionality

2. **API Endpoints**
   - `POST /api/playwright/store-extension-data` - Stores extension data in memory cache
   - `GET /api/playwright/get-extension-data/{dataId}` - Retrieves stored data (one-time use)

3. **Updated Extension** (view.js)
   - Parses Gherkin steps format: `"Action description (xpath=locator)"`
   - Stores data on server via API
   - Opens SuperQA review page with data ID in URL

### Technical Architecture

```
┌─────────────────────┐
│ Browser Extension   │
│  - Records steps    │
│  - Parses locators  │
└──────────┬──────────┘
           │
           │ 1. POST /store-extension-data
           ▼
┌─────────────────────┐
│   API Server        │
│  - IMemoryCache     │
│  - Stores data      │
│  - Returns dataId   │
└──────────┬──────────┘
           │
           │ 2. Opens URL with dataId
           ▼
┌─────────────────────┐
│ Review Page         │
│  - Test Name        │
│  - App URL (edit)   │
│  - Gherkin Steps    │
│  - Generate Button  │
└──────────┬──────────┘
           │
           │ 3. GET /get-extension-data/{dataId}
           ▼
┌─────────────────────┐
│   API Server        │
│  - Retrieves data   │
│  - Removes from     │
│    cache (one-time) │
└──────────┬──────────┘
           │
           │ 4. User clicks Generate
           ▼
┌─────────────────────┐
│   AI Generation     │
│  - OpenAI Service   │
│  - Page Inspector   │
│  - Test Script Gen  │
└─────────────────────┘
```

### Data Flow

1. **Extension captures data:**
   ```javascript
   {
     "testName": "User Login Test",
     "applicationUrl": "https://example.com/login",
     "steps": [
       {
         "action": "fill",
         "locator": "xpath=//input[@id='username']",
         "value": "testuser",
         "description": "Enter Username \"testuser\" (xpath=//input[@id='username'])"
       }
     ]
   }
   ```

2. **Stored in IMemoryCache** with 10-minute expiration

3. **Retrieved on review page** and displayed for user confirmation/editing

4. **Sent to AI** when user clicks "Generate Test Script"

### Security & Data Management

- Data stored in-memory only (IMemoryCache)
- 10-minute expiration time
- One-time use (removed after retrieval)
- No persistent storage
- Unique GUID for each session

## Benefits

✅ **User Control**: Users can review and edit data before generating tests  
✅ **Transparency**: Users see exactly what will be sent to AI  
✅ **Flexibility**: Application URL can be edited if needed  
✅ **Better UX**: Clear separation between data collection and test generation  
✅ **No Breaking Changes**: Existing `/api/playwright/generate-from-extension` endpoint still works  

## Testing

### Manual Test Steps

1. Open browser extension
2. Record some steps on a web page
3. Enter a test name
4. Click "Send to SuperQA"
5. Verify:
   - Extension shows success message
   - New tab opens with review page
   - Test name is pre-filled
   - Application URL is pre-filled
   - Gherkin steps are displayed
6. Edit Application URL if needed
7. Click "Generate Test Script"
8. Verify test script is generated
9. Click "Execute Test" to run the test

### API Testing

Test the store endpoint:
```bash
curl -k -X POST https://localhost:7001/api/playwright/store-extension-data \
  -H "Content-Type: application/json" \
  -d '{
    "testName": "Test",
    "applicationUrl": "https://example.com",
    "steps": [...]
  }'
```

Test the retrieve endpoint:
```bash
curl -k -X GET https://localhost:7001/api/playwright/get-extension-data/{dataId}
```

## Files Modified

- `src/SuperQA.Client/Pages/ExtensionTestReview.razor` (new)
- `src/SuperQA.Api/Controllers/PlaywrightController.cs`
- `src/SuperQA.Api/Program.cs`
- `Test-Case-and-Selector-Generator-Extension/view.js`
- `Test-Case-and-Selector-Generator-Extension/view.html`

## Future Enhancements

- Add validation for Gherkin steps format
- Allow editing individual steps before generation
- Save draft tests for later completion
- Add test history/recent tests list
