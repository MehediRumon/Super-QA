# Format Update Summary

## Changes Made

This PR implements the requested format changes for Gherkin steps and locators - merging them into a single unified format.

### Merged Format

**Before (Separate):**
```
Gherkin: When Select Target Entry Organization "<Organization>"
Locator: public static By Organization => By.XPath("//select[@id='OrganizationId']");
```

**After (Merged):**
```
Select Target Entry Organization "Udvash" (//select[@id='OrganizationId'])
```

**Key Changes:**
- Removed "When" prefix from all Gherkin steps
- Replaced placeholder variables (e.g., `<Organization>`) with actual values (e.g., `Udvash`)
- Merged step and locator into single line with XPath in parentheses
- For SELECT elements: Captures the text of the selected option
- For INPUT/TEXTAREA elements: Captures the actual value in the field
- For BUTTON elements: Shows action with XPath in parentheses
- Single unified output instead of separate sections

## Files Modified

### 1. content.js
- Updated `stepTemplates` object (lines 69-73)
  - Removed "When" prefix
  - Changed parameter from `varName` to `value`
  
- Updated click event listener (lines 207-216)
  - Added logic to capture actual values from INPUT elements
  - Added logic to capture selected option text from SELECT elements
  
- Updated locator generation (lines 252-263)
  - Changed from C# format to simplified "Xpath - {full xpath}" format
  - Shows complete XPath string for easy reference

### 2. logic.js
- Updated exported `stepTemplates` (lines 75-79)
  - Removed "When" prefix
  - Changed parameter from `varName` to `value`

### 3. SIMPLIFIED_VERSION.md
- Updated example outputs (lines 88-102)
  - Updated Gherkin steps examples
  - Updated locators examples
  - Updated usage description (lines 53-58)
  - Added live preview feature to the feature list

### 4. view.js
- Updated to display merged format
  - Refactored data loading into `updateDisplay()` function
  - Added storage change listener for real-time updates (live preview)
  - Removed separate locators section
  - Shows only merged Gherkin steps with locators

### 5. view.html
- Updated UI to show single merged section
  - Changed title to "Gherkin Steps with Locators"
  - Removed separate locators section
  - Simplified to one output area

### 6. NEW_FORMAT_UPDATE.md (New File)
- Comprehensive documentation of the merged format
- Before/after examples
- How it works explanation
- Multiple test case examples
- Benefits and migration notes

### 7. test_new_format.html (New File)
- Test page for verifying the new format
- Includes sample form elements
- Shows expected before/after output
- Includes testing instructions

## Testing

To test these changes:

1. Load the extension in Chrome (chrome://extensions/)
2. Open `test_new_format.html` in a browser
3. Enable collection in the extension popup
4. Fill in the form fields with actual data
5. Click on the filled fields to collect them
6. Open the View tab to see the merged format output

### Expected Results

When clicking on a SELECT element with "Udvash" selected:
- Output: `Select Target Entry Organization "Udvash" (//select[@id='OrganizationId'])`

When clicking on an INPUT element with "John Doe" entered:
- Output: `Enter Student Name "John Doe" (//input[@id='StudentName'])`

When clicking on a BUTTON element:
- Output: `Click On Submit Button (//button[@id='SubmitButton'])`

## Backward Compatibility

This is a breaking change in the output format. Users who have existing code that relies on:
- The "When" prefix in Gherkin steps
- Placeholder variables like `<Organization>`
- C# locator code format

Will need to update their code or manually edit the collected output.

## Benefits

1. **Simpler Format**: Less boilerplate, more readable
2. **Actual Data**: Shows real test data instead of placeholders
3. **Copy-Paste Ready**: Both formats are ready to use
4. **More Practical**: Reflects actual test execution with real values

## Files Changed
- `content.js` - Core logic for capturing and formatting
- `logic.js` - Exported templates for consistency
- `SIMPLIFIED_VERSION.md` - Updated documentation
- `NEW_FORMAT_UPDATE.md` - New comprehensive guide
- `test_new_format.html` - New test page
