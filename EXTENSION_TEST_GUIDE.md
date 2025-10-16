# Quick Test Guide - Extension Integration Features

## Prerequisites
- SuperQA.Api running on http://localhost:5000
- Browser with extension installed

## Test 1: Port Configuration ✅

### Steps:
1. Open extension in browser
2. Record some test steps
3. Enter test name: "Login Test"
4. Click "Send to SuperQA"

### Expected Result:
- Extension sends POST to `http://localhost:5000/api/playwright/store-extension-data`
- Browser opens new tab to `http://localhost:5000/extension-test-review?dataId={id}`
- Data loads successfully in ExtensionTestReview page

## Test 2: Database Persistence ✅

### Steps:
1. Complete Test 1 above
2. Note the dataId in the URL (e.g., dataId=1)
3. Restart the SuperQA.Api server
4. Navigate to `http://localhost:5000/extension-test-review?dataId=1`

### Expected Result:
- Data loads successfully (proving it's persisted in DB, not memory cache)
- Test name, URL, and steps are all displayed correctly

## Test 3: Edit Functionality ✅

### Steps:
1. Load extension test data (dataId=1)
2. Change test name to "Updated Login Test"
3. Modify a step's description
4. Click "Save Changes" button

### Expected Result:
- Success alert shown
- Reload the page (`dataId=1`)
- Changes are persisted and displayed

## Test 4: Delete Functionality ✅

### Steps:
1. Load extension test data (dataId=1)
2. Click "Delete" button
3. Confirm deletion in dialog

### Expected Result:
- Redirected to home page "/"
- Data is deleted from database
- Navigating to `dataId=1` shows "Data not found" error

## Test 5: Debug Mode Execution ✅

### Steps:
1. Load extension test data
2. Generate test script
3. Enable "Debug Mode" toggle
4. Set "Slow Motion" to 1000ms
5. Click "Execute Test"

### Expected Result:
- Browser window opens visibly (headed mode)
- Test executes with 1 second delay between actions
- You can watch each step happening in real-time

## Test 6: API Endpoints ✅

### Test Store Endpoint:
```bash
curl -X POST http://localhost:5000/api/playwright/store-extension-data \
  -H "Content-Type: application/json" \
  -d '{
    "testName": "API Test",
    "applicationUrl": "https://example.com",
    "steps": [
      {
        "action": "click",
        "locator": "xpath=//button[@id=\"login\"]",
        "value": "",
        "description": "Click login button"
      }
    ]
  }'
```

Expected: `{ "dataId": "2", "message": "Data stored successfully" }`

### Test Get Endpoint:
```bash
curl http://localhost:5000/api/playwright/get-extension-data/2
```

Expected: Returns the extension data

### Test List Endpoint:
```bash
curl http://localhost:5000/api/playwright/list-extension-data
```

Expected: Returns array of all extension data

### Test Update Endpoint:
```bash
curl -X PUT http://localhost:5000/api/playwright/update-extension-data/2 \
  -H "Content-Type: application/json" \
  -d '{
    "testName": "Updated API Test",
    "applicationUrl": "https://example.com",
    "steps": [
      {
        "action": "click",
        "locator": "xpath=//button[@id=\"login\"]",
        "value": "",
        "description": "Click updated login button"
      }
    ]
  }'
```

Expected: `{ "message": "Data updated successfully", "dataId": "2" }`

### Test Delete Endpoint:
```bash
curl -X DELETE http://localhost:5000/api/playwright/delete-extension-data/2
```

Expected: `{ "message": "Data deleted successfully" }`

## Verification Checklist

- [ ] Extension uses correct port (5000, not 7001)
- [ ] Data persists across server restarts
- [ ] Edit functionality saves changes
- [ ] Delete functionality removes data
- [ ] Debug mode shows visible browser
- [ ] Slow motion adds delay between actions
- [ ] All API endpoints work correctly
- [ ] Test case linking works (check TestCaseId after generation)

## Database Verification

### Check Extension Data Table:
If using SQL Server:
```sql
SELECT * FROM ExtensionTestData;
```

If using in-memory DB:
Use the list endpoint or check through the API.

### Verify Test Case Linking:
```sql
SELECT e.Id, e.TestName, e.TestCaseId, tc.Title 
FROM ExtensionTestData e
LEFT JOIN TestCases tc ON e.TestCaseId = tc.Id;
```

## Common Issues

### Issue: "Data not found"
**Cause**: Using in-memory database and server restarted
**Solution**: Use SQL Server for persistence, or test immediately after storing data

### Issue: Debug mode not working
**Cause**: Playwright browsers not installed
**Solution**: Build the project first to ensure browsers are installed

### Issue: Port mismatch error
**Cause**: API running on different port
**Solution**: Ensure API is running on port 5000 (check launchSettings.json)

## Success Criteria

All tests pass = ✅ Implementation is working correctly!

## Performance Notes

- Database operations add ~10-50ms latency vs memory cache
- Debug mode significantly slows test execution (by design)
- Slow motion setting affects execution time linearly (1000ms delay = ~1s per action)
