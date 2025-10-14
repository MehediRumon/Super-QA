# Simplified Extension - Only Gherkin Steps and Locators

## Overview
This extension has been simplified to focus only on collecting **Gherkin steps** (human-readable test steps) and **locators** (XPath selectors). All other features have been removed to make the extension cleaner and easier to use.

## What Was Removed
- ❌ **Method Code** - No longer generates C# method code
- ❌ **Parameter Values** - No longer tracks parameter values
- ❌ **Excel Export** - No longer exports parameters to Excel
- ❌ **JMeter Export** - No longer exports JMX files
- ❌ **Step File Generator** - No longer generates step file code
- ❌ **Visual Studio Integration** - No longer sends files to Visual Studio
- ❌ **XHR Logs** - No longer captures network requests
- ❌ **Root File Name** - No longer needed (was for namespace generation)
- ❌ **Network Logging** - No longer tracks HTTP requests

## What Was Kept
- ✅ **Merged Format** - Gherkin steps with locators in one line (e.g., `Select Country "USA" (//select[@id='country'])`)
- ✅ **Menu Name** - Prefix for Gherkin steps
- ✅ **Action Name** - For organizing your tests
- ✅ **Enable/Disable Toggle** - Turn collection on/off
- ✅ **Dark/Light Theme** - Theme toggle for UI
- ✅ **Copy & Edit** - Copy and edit collected data
- ✅ **Live Preview** - Real-time updates when collecting data

## User Interface Changes

### Popup (Extension Settings)
**Before:** Multiple cards with VS integration, namespace settings, logging, Excel/JMeter export
**After:** Single card with just Menu Name, Action Name, and Enable toggle

![Simplified Popup](https://github.com/user-attachments/assets/783ee311-3463-49bb-92ef-38434cd3dd09)

### View (Output Display)
**Before:** Multiple sections (Config Info, Locators, Gherkin Steps, Step File Generator, Methods, Excel Values, XHR Logs, Action Names)
**After:** Only 1 section - Gherkin Steps with Locators (merged format)

![Simplified View](https://github.com/user-attachments/assets/e15289f2-fc5c-46b6-80ef-ab6766e26c56)

## How to Use

1. **Install the Extension**
   - Load the extension in Chrome (chrome://extensions/)
   - Enable "Developer mode"
   - Click "Load unpacked" and select the extension folder

2. **Configure Settings**
   - Click the extension icon
   - Enter a **Menu Name** (e.g., "StudentReport")
   - Enter an **Action Name** (e.g., "CreateReport")
   - Toggle **Enable Collection** ON

3. **Collect Data**
   - Navigate to your web application
   - Click on form elements (inputs, buttons, selects)
   - The extension will collect:
     - Combined format: `Click On Submit Button (//button[@id='submit'])`

4. **View Results**
   - Click the extension icon
   - Click **"View"** button
   - See your collected Gherkin steps and locators

5. **Copy or Edit**
   - Click the 📋 button to copy
   - Click the ✏️ button to edit directly
   - Click 💾 to save edits

## Files Modified
- **content.js** - Removed parameter extraction, VS integration, methods generation
- **view.html** - Removed all sections except Gherkin Steps and Locators
- **view.js** - Simplified to only load and display Gherkin steps and locators
- **popup.html** - Removed VS integration, namespace, logging, and export buttons
- **popup.js** - Removed all complex features, keeping only basic settings

## Code Reduction
- **4,393 lines removed**
- **78 lines added** (simplified code)
- **Net reduction: 4,315 lines**

## Benefits
- ✅ **Simpler** - Easier to understand and use
- ✅ **Faster** - No complex processing or network calls
- ✅ **Cleaner** - Focus on what matters (steps and locators)
- ✅ **Maintainable** - Less code to maintain and debug

## Example Output

### Gherkin Steps with Locators
```
1. Click On Submit Button (//button[@id='submit'])
2. Enter Username "john.doe" (//input[@id='username'])
3. Select Country "United States" (//select[@id='country'])
```

## Migration Notes
If you were using the old version with methods, parameters, Excel export, etc., you will need to:
1. Export your existing data before upgrading
2. Install the simplified version
3. Start fresh with the new simplified workflow

## Support
For issues or questions, please create an issue in the GitHub repository.
