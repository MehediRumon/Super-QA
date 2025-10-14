# New Format Update - Merged Gherkin Steps with Locators

## Overview
This update merges Gherkin steps and locators into a single, unified format that combines the human-readable step with the XPath locator in parentheses.

## Changes Summary

### Merged Format

#### Before (Old Format - Separate)
```
Gherkin Step: When Select Target Entry Organization "<Organization>"
Locator: public static By Organization => By.XPath("//select[@id='OrganizationId']");
```

#### After (New Format - Merged)
```
Select Target Entry Organization "Udvash" (//select[@id='OrganizationId'])
```

**Key Changes:**
- ❌ Removed "When" prefix from all steps
- ✅ Uses actual values instead of placeholders
- ✅ Merges step and locator into one line with XPath in parentheses
- ✅ For SELECT elements: Shows the selected option text (e.g., "Udvash")
- ✅ For INPUT elements: Shows the entered value (e.g., "John Doe")
- ✅ For BUTTON/Click actions: Shows action with XPath
- ✅ Single unified output instead of separate sections

## How It Works

### Value Capture Logic

1. **For SELECT elements:**
   - Captures the visible text of the selected option
   - Example: If `<option value="udv">Udvash</option>` is selected, it captures "Udvash"

2. **For INPUT/TEXTAREA elements:**
   - Captures the actual value in the input field
   - Example: If user typed "John Doe", it captures "John Doe"
   - If field is empty, captures empty string ""

3. **For BUTTON/Click actions:**
   - No value needed, just the action label

### Locator Priority

The extension generates a merged format combining the Gherkin step with the complete XPath in parentheses.

Format: `{Step Text} "{Value}" ({complete xpath})`

Examples:
- `Select Organization "Udvash" (//select[@id='OrganizationId'])`
- `Enter Department "Sales" (//input[@name='department'])`
- `Click On Save Button (//*[@data-testid='save-btn'])`

## Examples

### Example 1: Organization Dropdown

**HTML:**
```html
<label for="OrganizationId">Target Entry Organization:</label>
<select id="OrganizationId">
    <option value="">Select...</option>
    <option value="udv" selected>Udvash</option>
    <option value="esm">English Medium School</option>
</select>
```

**When you click on this select (with "Udvash" selected):**
- Output: `Select Target Entry Organization "Udvash" (//select[@id='OrganizationId'])`

### Example 2: Student Name Input

**HTML:**
```html
<label for="StudentName">Student Name:</label>
<input type="text" id="StudentName" value="John Doe">
```

**When you click on this input (with "John Doe" entered):**
- Output: `Enter Student Name "John Doe" (//input[@id='StudentName'])`

### Example 3: Submit Button

**HTML:**
```html
<button id="SubmitButton" type="submit">Submit Form</button>
```

**When you click on this button:**
- Output: `Click On Submit Button (//button[@id='SubmitButton'])`

### Example 4: Element with Menu Name

**If Menu Name is set to "Target Entry":**

**HTML:**
```html
<select id="OrganizationId">
    <option value="udv" selected>Udvash</option>
</select>
```

**Output:**
- `Select Target Entry Organization "Udvash" (//select[@id='OrganizationId'])`

## Benefits

1. **Clearer Intent**: Actual values make it obvious what data was used
2. **Simpler Format**: Combined step and locator in one line
3. **Copy-Paste Ready**: Format is ready to use as-is
4. **Concise**: Removed unnecessary "When" prefix
5. **Unified**: Single output instead of separate sections

## Migration Notes

If you have existing data in the old format:

1. **Gherkin Steps**: You can manually remove "When" prefix if needed
2. **Locators**: Old format still contains the XPath, just extract the ID from it
3. **New Collections**: All new collections will use the new format automatically

## Testing the Changes

Use the `test_new_format.html` file included in the repository:

1. Load the extension in Chrome
2. Open `test_new_format.html`
3. Set a menu name (optional)
4. Enable collection
5. Fill in the form fields
6. Click on the filled fields
7. Open the View tab to see the new format

## Technical Details

### Files Modified

1. **content.js**
   - Updated `stepTemplates` to remove "When" prefix
   - Changed templates to accept actual values instead of variable names
   - Added value capture logic for SELECT and INPUT elements
   - Updated locator generation to simple "Xpath - {id}" format

2. **logic.js**
   - Updated exported `stepTemplates` to match new format
   - Maintained backward compatibility with menuName parameter

3. **SIMPLIFIED_VERSION.md**
   - Updated examples to reflect new format

## Support

For issues or questions about the new format, please create an issue in the GitHub repository.
