# Testing Guide for SuperQA Browser Extension

This guide provides step-by-step instructions to test the browser extension functionality.

## Prerequisites

1. SuperQA backend running on `https://localhost:7001`
2. SuperQA frontend running on `https://localhost:5001`
3. Chrome browser installed
4. Extension loaded in Chrome (see installation guide)

## Test Suite

### Test 1: Extension Installation

**Objective**: Verify extension loads correctly

**Steps**:
1. Open Chrome
2. Navigate to `chrome://extensions/`
3. Enable "Developer mode"
4. Click "Load unpacked"
5. Select the `Test-Case-and-Selector-Generator-Extension` folder

**Expected Results**:
- ✅ Extension loads without errors
- ✅ Extension icon appears in toolbar
- ✅ No error messages in console

**Troubleshooting**:
- If manifest errors occur, check manifest.json syntax
- Ensure all required files exist

---

### Test 2: UI Display

**Objective**: Verify popup UI displays correctly

**Steps**:
1. Click the extension icon in toolbar

**Expected Results**:
- ✅ Popup opens (450-600px wide)
- ✅ Purple gradient header visible
- ✅ "Test Name" input field present
- ✅ Three control buttons visible (Start, Stop, Clear)
- ✅ "Test Output Viewer" section present
- ✅ "Send to SuperQA" button visible but disabled
- ✅ Empty state message: "No steps recorded yet"
- ✅ Step counter shows "0 steps recorded"
- ✅ Footer shows version "v1.0.0"

**Troubleshooting**:
- If popup doesn't open, check for JavaScript errors in console
- If styling is off, verify popup.css loaded correctly

---

### Test 3: Test Name Input

**Objective**: Verify test name input works and persists

**Steps**:
1. Open extension popup
2. Type "User Login Test" in Test Name field
3. Close popup
4. Reopen popup

**Expected Results**:
- ✅ Can type in Test Name field
- ✅ Input has focus indicator (purple border)
- ✅ Test name persists after reopening
- ✅ Placeholder text visible when empty

**Troubleshooting**:
- If persistence fails, check Chrome storage permissions
- Verify popup.js storage logic

---

### Test 4: Recording Control

**Objective**: Verify recording start/stop functionality

**Steps**:
1. Open extension popup
2. Enter test name: "Button Click Test"
3. Click "Start Recording" button
4. Click "Stop Recording" button

**Expected Results**:
- ✅ Cannot start recording without test name
- ✅ "Start Recording" button becomes disabled when recording
- ✅ "Stop Recording" button enables when recording starts
- ✅ Test name input locks during recording
- ✅ Status message appears: "Recording started..."
- ✅ Status message appears: "Recording stopped."
- ✅ Buttons return to initial state after stopping

**Troubleshooting**:
- Check browser console for messaging errors
- Verify content script injection

---

### Test 5: Click Recording

**Objective**: Verify click actions are recorded

**Test Site**: https://example.com

**Steps**:
1. Navigate to https://example.com
2. Open extension popup
3. Enter test name: "Click Test"
4. Click "Start Recording"
5. Click the "More information..." link on the page
6. Return to extension popup

**Expected Results**:
- ✅ One step appears in Test Output Viewer
- ✅ Step shows: `When I click the link "More information..."`
- ✅ Locator is displayed (e.g., `a[href="..."]`)
- ✅ Step counter shows "1 step recorded"
- ✅ "Send to SuperQA" button becomes enabled
- ✅ Step has purple left border
- ✅ Locator has gray background

**Troubleshooting**:
- If step doesn't appear, check content.js console logs
- Verify message passing between content and popup scripts

---

### Test 6: Form Input Recording

**Test Page**: Create a simple HTML page with form

```html
<!DOCTYPE html>
<html>
<body>
    <form>
        <input type="text" id="username" placeholder="Username">
        <input type="password" id="password" placeholder="Password">
        <button type="submit">Login</button>
    </form>
</body>
</html>
```

**Steps**:
1. Open test page
2. Open extension popup
3. Enter test name: "Form Test"
4. Click "Start Recording"
5. Type "testuser" in username field
6. Type "pass123" in password field
7. Wait 1 second (debounce)
8. Return to popup

**Expected Results**:
- ✅ Two steps appear
- ✅ Step 1: `When I enter "testuser" into the "Username" field`
  - Locator: `#username`
- ✅ Step 2: `When I enter "pass123" into the "Password" field`
  - Locator: `#password`
- ✅ Step counter shows "2 steps recorded"
- ✅ Input debouncing works (doesn't create step for each keystroke)

**Troubleshooting**:
- If multiple steps created, check debounce timing
- Verify input event handler in content.js

---

### Test 7: Dropdown Recording

**Test Page**: HTML with select element

```html
<!DOCTYPE html>
<html>
<body>
    <select id="country" name="country">
        <option value="">Select Country</option>
        <option value="us">United States</option>
        <option value="uk">United Kingdom</option>
        <option value="ca">Canada</option>
    </select>
</body>
</html>
```

**Steps**:
1. Open test page
2. Start recording with test name "Dropdown Test"
3. Select "United States" from dropdown
4. Return to popup

**Expected Results**:
- ✅ One step appears
- ✅ Step: `When I select "United States" from the "country" dropdown`
- ✅ Locator: `#country`
- ✅ Step counter updated

**Troubleshooting**:
- Verify change event handler for select elements
- Check getLabel function for proper label detection

---

### Test 8: Multiple Interactions

**Test Site**: https://httpbin.org/forms/post

**Steps**:
1. Navigate to httpbin forms page
2. Start recording with test name "Form Submission Test"
3. Fill in customer name field
4. Fill in telephone field
5. Fill in email field
6. Select pizza size
7. Add a topping checkbox
8. Add delivery instructions
9. Stop recording

**Expected Results**:
- ✅ Multiple steps appear (5-7 steps)
- ✅ Each step has appropriate keyword (When)
- ✅ Each step has proper description
- ✅ Each step has locator
- ✅ Steps appear in order of interaction
- ✅ Test Output Viewer is scrollable
- ✅ All locators are valid CSS selectors

**Troubleshooting**:
- If some interactions not captured, check content.js event handlers
- Verify getSelector function covers all element types

---

### Test 9: Clear Steps

**Objective**: Verify clear functionality

**Steps**:
1. Record 3-5 steps (any interactions)
2. Click "Clear" button
3. Confirm in dialog

**Expected Results**:
- ✅ Confirmation dialog appears
- ✅ After confirmation, all steps removed
- ✅ Empty state message appears
- ✅ Step counter shows "0 steps recorded"
- ✅ "Send to SuperQA" button becomes disabled
- ✅ If cancel dialog, steps remain

**Troubleshooting**:
- Check popup.js clear button handler
- Verify Chrome storage clear logic

---

### Test 10: Send to SuperQA (Success)

**Prerequisites**: SuperQA API running

**Steps**:
1. Record 2-3 test steps
2. Ensure test name is filled: "SuperQA Integration Test"
3. Click "Send to SuperQA" button
4. Wait for response

**Expected Results**:
- ✅ Button shows "Sending to SuperQA..." status
- ✅ Button becomes disabled temporarily
- ✅ Success message appears: "Test successfully sent to SuperQA!"
- ✅ New tab opens with SuperQA UI
- ✅ SuperQA loads with `?fromExtension=true` parameter
- ✅ Status message auto-dismisses after 5 seconds

**Network Request Verification**:
```
POST https://localhost:7001/api/playwright/generate-from-extension
Content-Type: application/json

{
  "applicationUrl": "https://...",
  "testName": "SuperQA Integration Test",
  "steps": [
    {
      "action": "...",
      "locator": "...",
      "value": "...",
      "description": "..."
    }
  ]
}
```

**Troubleshooting**:
- Open DevTools Network tab to inspect request
- Check SuperQA API logs for errors
- Verify CORS settings if request blocked

---

### Test 11: Send to SuperQA (Error Handling)

**Prerequisites**: SuperQA API NOT running

**Steps**:
1. Stop SuperQA backend
2. Record test steps
3. Click "Send to SuperQA"

**Expected Results**:
- ✅ Error message appears
- ✅ Message indicates connection failure
- ✅ Button re-enables after error
- ✅ No new tab opens
- ✅ Error message persists (doesn't auto-dismiss)

**Troubleshooting**:
- Check error handling in popup.js
- Verify fetch catch block

---

### Test 12: Validation - No Test Name

**Objective**: Verify validation prevents recording without test name

**Steps**:
1. Open popup
2. Leave Test Name field empty
3. Click "Start Recording"

**Expected Results**:
- ✅ Error message appears: "Please enter a test name before recording"
- ✅ Test name field receives focus
- ✅ Recording does not start
- ✅ Error message is dismissible

**Troubleshooting**:
- Check validation logic in popup.js startRecording handler

---

### Test 13: Validation - No Steps

**Objective**: Verify validation prevents sending with no steps

**Steps**:
1. Open popup
2. Enter test name
3. Don't record any steps
4. Attempt to click "Send to SuperQA"

**Expected Results**:
- ✅ "Send to SuperQA" button is disabled
- ✅ Cannot click button
- ✅ After recording at least one step, button enables

**Troubleshooting**:
- Check sendToSuperQABtn.disabled logic
- Verify updateRecordingState function

---

### Test 14: Locator Priority

**Test Page**: HTML with various selector options

```html
<!DOCTYPE html>
<html>
<body>
    <button id="btn-id">Button with ID</button>
    <button data-testid="btn-testid">Button with testid</button>
    <button name="btn-name">Button with name</button>
    <button aria-label="Close">Button with aria-label</button>
    <button class="btn-primary">Button with class</button>
    <button>Plain Button</button>
</body>
</html>
```

**Steps**:
1. Start recording
2. Click each button in order
3. Check recorded locators

**Expected Results**:
- ✅ Button 1 locator: `#btn-id` (ID priority)
- ✅ Button 2 locator: `[data-testid="btn-testid"]` (testid priority)
- ✅ Button 3 locator: `[name="btn-name"]` (name priority)
- ✅ Button 4 locator: `[aria-label="Close"]` (aria-label priority)
- ✅ Button 5 locator: `.btn-primary` (class priority)
- ✅ Button 6 locator: `button:has-text("Plain Button")` or CSS path

**Troubleshooting**:
- Review getSelector function in content.js
- Verify priority order matches specification

---

### Test 15: Cross-Tab Recording

**Objective**: Verify recording state persists across tabs

**Steps**:
1. Open Tab A, start recording
2. Open Tab B with extension popup
3. Check if recording state is shown
4. Close Tab A
5. Return to Tab B

**Expected Results**:
- ✅ Tab B shows recording in progress
- ✅ Steps recorded in Tab A visible in Tab B
- ✅ Can stop recording from either tab
- ✅ Recording continues if page refreshed

**Troubleshooting**:
- Check Chrome storage for isRecording flag
- Verify background.js tab update listener

---

### Test 16: Performance

**Objective**: Verify extension doesn't slow down page

**Steps**:
1. Open complex website (e.g., Gmail, Twitter)
2. Open browser DevTools Performance tab
3. Start recording extension + browser performance
4. Interact with page normally
5. Stop both recordings

**Expected Results**:
- ✅ No noticeable lag when clicking elements
- ✅ Page remains responsive
- ✅ No memory leaks
- ✅ Content script CPU usage < 5%

**Troubleshooting**:
- Check for inefficient DOM queries
- Review event listener attachment/removal
- Optimize selector generation

---

### Test 17: Edge Cases

**Test various edge cases**:

1. **Empty Field Values**
   - Type in field, then delete all text
   - Expected: No step recorded

2. **Rapid Clicks**
   - Click button 10 times quickly
   - Expected: 10 steps recorded

3. **Hidden Elements**
   - Click hidden element (display:none)
   - Expected: Step recorded with CSS path

4. **iFrame Content**
   - Click element inside iframe
   - Expected: May not record (known limitation)

5. **Dynamic Content**
   - Click button that adds element, then click new element
   - Expected: Both clicks recorded

6. **Very Long Values**
   - Type 1000 characters in field
   - Expected: Full value recorded, description truncated if needed

---

## Test Report Template

```
Test Date: __________
Tester: __________
Browser: Chrome v____
OS: __________

| Test # | Test Name | Status | Notes |
|--------|-----------|--------|-------|
| 1      | Extension Installation | ☐ Pass ☐ Fail | |
| 2      | UI Display | ☐ Pass ☐ Fail | |
| 3      | Test Name Input | ☐ Pass ☐ Fail | |
| 4      | Recording Control | ☐ Pass ☐ Fail | |
| 5      | Click Recording | ☐ Pass ☐ Fail | |
| 6      | Form Input Recording | ☐ Pass ☐ Fail | |
| 7      | Dropdown Recording | ☐ Pass ☐ Fail | |
| 8      | Multiple Interactions | ☐ Pass ☐ Fail | |
| 9      | Clear Steps | ☐ Pass ☐ Fail | |
| 10     | Send to SuperQA (Success) | ☐ Pass ☐ Fail | |
| 11     | Send to SuperQA (Error) | ☐ Pass ☐ Fail | |
| 12     | Validation - No Test Name | ☐ Pass ☐ Fail | |
| 13     | Validation - No Steps | ☐ Pass ☐ Fail | |
| 14     | Locator Priority | ☐ Pass ☐ Fail | |
| 15     | Cross-Tab Recording | ☐ Pass ☐ Fail | |
| 16     | Performance | ☐ Pass ☐ Fail | |
| 17     | Edge Cases | ☐ Pass ☐ Fail | |

Overall Status: ☐ All Tests Pass ☐ Some Failures

Critical Issues Found:
_____________________

Minor Issues Found:
_____________________

Recommendations:
_____________________
```

## Automated Testing Considerations

While this extension is primarily tested manually, consider:

1. **Selenium WebDriver** for popup automation
2. **Puppeteer** for headless testing
3. **Chrome DevTools Protocol** for lower-level testing
4. **Unit tests** for utility functions (getSelector, getLabel, etc.)

## Known Limitations

1. **iFrames**: Content script may not record interactions within iframes
2. **Shadow DOM**: Elements in shadow DOM may not be accessible
3. **Single-Page Apps**: Heavy SPA navigation may require script reinjection
4. **File Uploads**: File input interactions not yet supported
5. **Hover States**: Hover-only interactions not captured

## Bug Reporting

When reporting bugs, include:
1. Browser version
2. Extension version
3. Steps to reproduce
4. Expected vs actual behavior
5. Screenshots/recordings
6. Browser console errors
7. Network request details (if API related)
