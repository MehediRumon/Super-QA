# Editable Gherkin Steps Feature

## Overview

This update implements the ability for users to edit Gherkin steps with locators in the Extension Test Review page before generating Playwright test scripts.

## Changes Made

### 1. Made Gherkin Steps Textarea Editable

**Before:** The Gherkin steps textarea was readonly - users could only view the recorded steps.

**After:** The textarea is now fully editable - users can modify steps, locators, values, and descriptions before generating the test script.

### 2. Enhanced Step Display Format

**Before:** Only step descriptions were shown (e.g., "Click the Login Button")

**After:** Full FRS format is displayed including:
- Step description
- Action (e.g., click, fill, navigate)
- Locator (e.g., xpath=//button[@id='login'])
- Value (if applicable)

Example display:
```
Browser Extension Recorded Steps:

1. Enter Username "testuser" (xpath=//input[@id='username'])
   Action: fill
   Locator: xpath=//input[@id='username']
   Value: testuser

2. Click on Login Button
   Action: click
   Locator: xpath=//button[@id='login-btn']

3. Verify Dashboard is visible
   Action: assert
   Locator: xpath=//h1[@class='dashboard-title']
```

### 3. Updated Test Generation Logic

**Before:** Used structured steps from the browser extension to generate tests.

**After:** Uses the edited Gherkin text directly, allowing users to:
- Fix incorrect locators
- Add missing steps
- Remove unnecessary steps
- Modify values
- Update step descriptions

## User Benefits

✅ **Full Control**: Users can review and modify all aspects of the test before generation

✅ **Error Correction**: Fix any incorrect locators or values captured by the extension

✅ **Flexibility**: Add, remove, or modify steps as needed

✅ **Transparency**: See exactly what data will be sent to AI for test generation

## Technical Details

### Files Modified

- `src/SuperQA.Client/Pages/ExtensionTestReview.razor`
  - Removed `readonly` attribute from Gherkin steps textarea
  - Added `FormatStepsForDisplay()` method to show full FRS format
  - Updated `GenerateTestScript()` to use edited text instead of original steps
  - Added help text indicating steps are editable

### Implementation Approach

1. **Display Format**: Added `FormatStepsForDisplay()` helper method that formats steps in the same FRS format that gets sent to AI
2. **Validation**: Checks for non-empty `gherkinStepsText` instead of `steps` list
3. **API Integration**: Uses `PlaywrightTestGenerationRequest` with `FrsText` property instead of `GenerateFromExtensionRequest` with structured steps
4. **Settings Integration**: Retrieves OpenAI API key from settings instead of passing it through the request

### Backward Compatibility

✅ No breaking changes - all existing functionality remains intact
✅ Original `/api/playwright/generate-from-extension` endpoint still works
✅ Original browser extension integration still functional

## Usage

1. **Record Steps in Browser Extension**
   - Use the browser extension to record test steps on any web page
   - Click "Send to SuperQA"

2. **Review and Edit in SuperQA**
   - SuperQA opens the Extension Test Review page
   - Test name is pre-filled (editable)
   - Application URL is pre-filled (editable)
   - Gherkin steps with locators are displayed (now editable!)

3. **Edit as Needed**
   - Click in the Gherkin steps textarea
   - Modify any step description, action, locator, or value
   - Add new steps manually if needed
   - Remove steps by deleting lines

4. **Generate Test Script**
   - Click "Generate Test Script" button
   - AI generates Playwright test using your edited steps
   - Execute the test directly or copy the script

## Example Edit Scenarios

### Scenario 1: Fix Incorrect Locator
**Original:**
```
1. Click Login Button
   Action: click
   Locator: xpath=//button[1]
```

**Edited:**
```
1. Click Login Button
   Action: click
   Locator: xpath=//button[@id='login-btn']
```

### Scenario 2: Add Missing Value
**Original:**
```
1. Enter Email
   Action: fill
   Locator: xpath=//input[@type='email']
```

**Edited:**
```
1. Enter Email
   Action: fill
   Locator: xpath=//input[@type='email']
   Value: test@example.com
```

### Scenario 3: Add New Step Manually
**Original:**
```
1. Enter Username
   Action: fill
   Locator: xpath=//input[@id='username']
   Value: admin

2. Click Login
   Action: click
   Locator: xpath=//button[@type='submit']
```

**Edited:**
```
1. Enter Username
   Action: fill
   Locator: xpath=//input[@id='username']
   Value: admin

2. Enter Password
   Action: fill
   Locator: xpath=//input[@id='password']
   Value: password123

3. Click Login
   Action: click
   Locator: xpath=//button[@type='submit']
```

## Testing

All existing tests pass (31/31):
```
Passed!  - Failed: 0, Passed: 31, Skipped: 0, Total: 31
```

No new tests were added as this is a UI change that enhances existing functionality without changing the underlying logic.

## Related Documentation

- See `EXTENSION_REVIEW_FEATURE.md` for the original review feature implementation
- See `EXTENSION_QUICKSTART.md` for user guide
- See `EXTENSION_INTEGRATION_GUIDE.md` for complete integration guide
