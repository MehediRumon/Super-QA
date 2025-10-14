# SuperQA Test Case and Selector Generator Extension

A Chrome extension that records user interactions on web pages and generates Gherkin test cases with locators for SuperQA.

## Features

- **Test Name Input**: Define a descriptive name for your test case
- **Interactive Recording**: Captures clicks, form inputs, dropdowns, and form submissions
- **Gherkin Step Generation**: Automatically generates Given/When/Then steps
- **Smart Locator Detection**: Prioritizes stable locators (ID, data-testid, name, aria-label)
- **Test Output Viewer**: Real-time display of recorded Gherkin steps with locators
- **Send to SuperQA**: One-click integration to send tests to SuperQA for execution

## Installation

### Development Mode

1. Clone the repository or download the extension folder
2. Open Chrome and navigate to `chrome://extensions/`
3. Enable "Developer mode" in the top right corner
4. Click "Load unpacked"
5. Select the `Test-Case-and-Selector-Generator-Extension` folder
6. The extension icon should appear in your toolbar

### Production Mode

1. Package the extension as a .crx file or publish to Chrome Web Store
2. Install from Chrome Web Store

## Usage

### Step 1: Enter Test Name

1. Click the SuperQA extension icon in your toolbar
2. Enter a descriptive test name (e.g., "User Login Test", "Shopping Cart Checkout")
3. This name will be used to organize your test in SuperQA

### Step 2: Record Interactions

1. Click "Start Recording" button
2. Interact with the web page:
   - Click buttons, links, and elements
   - Fill in form fields
   - Select dropdown options
   - Submit forms
3. The extension captures each interaction as a Gherkin step
4. Click "Stop Recording" when finished

### Step 3: Review Test Output

The **Test Output Viewer** displays your recorded steps in Gherkin format:

```gherkin
Given I am on the login page
When I enter "testuser@example.com" into the "Email" field
  Locator: #email
When I enter "password123" into the "Password" field
  Locator: #password
When I click the "Login" button
  Locator: button[type="submit"]
Then I should see the dashboard
```

Each step includes:
- **Keyword**: Given, When, Then
- **Description**: Human-readable action
- **Locator**: CSS selector for the element

### Step 4: Send to SuperQA

1. Review your recorded steps in the Test Output Viewer
2. Click "Send to SuperQA" button
3. The extension will:
   - Package your Gherkin steps with locators
   - Send them to SuperQA API
   - Open SuperQA in a new tab with your test loaded
4. SuperQA will generate executable Playwright test scripts from your recorded steps

## Supported Interactions

The extension automatically captures:

### Click Events
- Buttons
- Links
- Checkboxes
- Radio buttons
- Generic clickable elements

### Form Inputs
- Text fields
- Password fields
- Textareas
- Email fields
- Number fields

### Dropdowns
- Select elements
- Option selection

### Form Submissions
- Form submit events

## Locator Priority

The extension uses intelligent locator detection with the following priority:

1. **ID** - Most stable and unique (`#elementId`)
2. **data-testid** - Testing-specific attribute (`[data-testid="login-button"]`)
3. **name** - Form field names (`[name="username"]`)
4. **aria-label** - Accessibility labels (`[aria-label="Close"]`)
5. **class** - CSS classes (`.btn-primary`)
6. **text content** - For links and buttons (`button:has-text("Login")`)
7. **CSS path** - Full CSS path as fallback

## Configuration

### SuperQA API Endpoint

By default, the extension sends data to:
- **API**: `https://localhost:7001/api/playwright/generate-from-extension`
- **UI**: `https://localhost:5001/playwright-generator`

To change the endpoint, update the URLs in `popup.js`.

### Permissions

The extension requires:
- **activeTab**: To access the current tab's content
- **storage**: To persist recorded steps
- **scripting**: To inject content script
- **host_permissions**: To communicate with SuperQA API

## Clear Steps

To start over:
1. Click the "Clear" button
2. Confirm the action
3. All recorded steps will be removed

## Troubleshooting

### Extension Not Recording

- Ensure you clicked "Start Recording"
- Check that you entered a test name
- Verify the page is fully loaded
- Try refreshing the page and starting recording again

### Cannot Send to SuperQA

- Check that SuperQA is running locally
- Verify the API endpoint is correct
- Ensure you have recorded at least one step
- Check browser console for error messages

### Locators Not Accurate

- Use `data-testid` attributes in your application for best results
- Add IDs to important interactive elements
- Consider using aria-labels for accessibility and testing

## Development

### File Structure

```
Test-Case-and-Selector-Generator-Extension/
├── manifest.json       # Extension configuration
├── popup.html          # Extension popup UI
├── popup.css           # Popup styling
├── popup.js            # Popup logic
├── content.js          # Content script for recording
├── background.js       # Background service worker
├── icons/              # Extension icons
│   ├── icon16.png
│   ├── icon48.png
│   └── icon128.png
└── README.md           # This file
```

### Technologies Used

- Chrome Extension Manifest V3
- Vanilla JavaScript (ES6+)
- Chrome Storage API
- Chrome Tabs API
- Chrome Runtime Messaging API

## Version History

### v1.0.0 (Current)
- Initial release
- Test Name input field
- Interactive recording of user interactions
- Gherkin step generation with locators
- Test Output Viewer
- Send to SuperQA integration
- Smart locator detection
- Modern UI with purple gradient theme

## Support

For issues, feature requests, or questions:
- Open an issue on the SuperQA repository
- Visit: https://github.com/MehediRumon/Super-QA

## License

This extension is part of the SuperQA project.
