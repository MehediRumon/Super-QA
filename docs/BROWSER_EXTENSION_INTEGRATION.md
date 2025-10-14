# Browser Extension Integration Guide

## Overview

SuperQA now supports receiving test steps and locators directly from browser extensions. This allows you to record user interactions in your browser and automatically generate Playwright test scripts from them.

## How It Works

1. **Record Steps**: Use a browser extension to record user actions (clicks, typing, navigation, etc.)
2. **Capture Locators**: The extension captures DOM selectors for each element interacted with
3. **Send to SuperQA**: With a single button click, send the recorded data to SuperQA
4. **Auto-Generate Tests**: SuperQA automatically creates a test script using AI
5. **Execute & Report**: Run the generated test and view detailed results

## API Endpoint

### POST `/api/playwright/generate-from-extension`

Accepts browser extension data and generates a Playwright test script.

**Request Body:**
```json
{
  "applicationUrl": "https://example.com",
  "steps": [
    {
      "action": "navigate",
      "locator": "",
      "value": "https://example.com/login",
      "description": "Navigate to login page"
    },
    {
      "action": "fill",
      "locator": "#username",
      "value": "testuser",
      "description": "Enter username"
    },
    {
      "action": "fill",
      "locator": "#password",
      "value": "password123",
      "description": "Enter password"
    },
    {
      "action": "click",
      "locator": "button[type='submit']",
      "value": "",
      "description": "Click login button"
    },
    {
      "action": "assert",
      "locator": ".welcome-message",
      "value": "Welcome",
      "description": "Verify welcome message is displayed"
    }
  ],
  "openAIApiKey": "sk-...",  // Optional - uses saved settings if not provided
  "model": "gpt-4o-mini"      // Optional - uses saved settings if not provided
}
```

**Response:**
```json
{
  "success": true,
  "generatedScript": "// Playwright test script in C#\nusing Microsoft.Playwright;\n...",
  "warnings": null
}
```

## Supported Actions

| Action | Description | Locator Required | Value Required |
|--------|-------------|------------------|----------------|
| `navigate` | Navigate to a URL | No | Yes (URL) |
| `click` | Click an element | Yes | No |
| `fill` | Fill input field | Yes | Yes (text to enter) |
| `select` | Select dropdown option | Yes | Yes (option value) |
| `check` | Check checkbox | Yes | No |
| `uncheck` | Uncheck checkbox | Yes | No |
| `assert` | Verify element contains text | Yes | Yes (expected text) |
| `hover` | Hover over element | Yes | No |
| `wait` | Wait for element | Yes | No |

## Locator Formats

SuperQA supports all Playwright locator strategies:

- **ID**: `#elementId`
- **Class**: `.className`
- **CSS Selector**: `button[type='submit']`
- **XPath**: `//button[@id='submit']`
- **Text**: `text=Login`
- **Role**: `role=button[name="Login"]`
- **Test ID**: `[data-testid='login-button']`

## Example Browser Extension Implementation

Here's a basic example of how a browser extension could integrate with SuperQA:

```javascript
// Background script or popup script
async function sendToSuperQA(steps) {
  const data = {
    applicationUrl: window.location.origin,
    steps: steps
  };

  try {
    const response = await fetch('https://localhost:7001/api/playwright/generate-from-extension', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(data)
    });

    const result = await response.json();
    
    if (result.success) {
      console.log('Test script generated successfully!');
      console.log(result.generatedScript);
      
      // You could open SuperQA in a new tab with the script
      chrome.tabs.create({
        url: 'https://localhost:5001/playwright-generator?fromExtension=true'
      });
    } else {
      console.error('Error generating test:', result.errorMessage);
    }
  } catch (error) {
    console.error('Failed to send data to SuperQA:', error);
  }
}

// Example: Record a button click
function recordClick(element) {
  const step = {
    action: 'click',
    locator: getSelector(element),
    value: '',
    description: `Click ${element.tagName.toLowerCase()}`
  };
  
  recordedSteps.push(step);
}

// Helper function to get a good selector for an element
function getSelector(element) {
  // Try ID first
  if (element.id) {
    return `#${element.id}`;
  }
  
  // Try data-testid
  if (element.dataset.testid) {
    return `[data-testid='${element.dataset.testid}']`;
  }
  
  // Try name attribute
  if (element.name) {
    return `[name='${element.name}']`;
  }
  
  // Try class
  if (element.className && typeof element.className === 'string') {
    return `.${element.className.split(' ')[0]}`;
  }
  
  // Fallback to tag name
  return element.tagName.toLowerCase();
}
```

## Benefits

1. **No Manual Scripting**: Record interactions instead of writing test code
2. **Accurate Locators**: Capture actual DOM selectors from the page
3. **Fast Test Creation**: Generate tests in seconds
4. **Settings Integration**: Uses saved API keys and preferences
5. **Intelligent Generation**: AI understands context and creates robust tests

## Security Notes

- API keys can be saved in SuperQA Settings (encrypted and secure)
- If not saved, extension can pass the API key in the request
- All communication should use HTTPS
- Consider CORS configuration for production deployments

## Next Steps

1. Install or develop a browser extension that records user interactions
2. Configure your SuperQA settings (API key, model, etc.)
3. Record test steps in your browser
4. Click "Send to SuperQA" in the extension
5. Review and execute the generated test in SuperQA

## Example Extensions

While SuperQA doesn't provide its own browser extension yet, you can integrate with existing extensions or build your own using the API endpoint documented above.

Popular recorder extensions that could be adapted:
- Selenium IDE
- Puppeteer Recorder
- Playwright Test Generator
- Custom Chrome Extension

For support or questions, please visit: https://github.com/MehediRumon/Super-QA
