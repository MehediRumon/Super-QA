# Quick Start Guide - SuperQA Browser Extension

Get up and running with the SuperQA Test Case and Selector Generator Extension in 5 minutes!

## Installation (2 minutes)

### Step 1: Load the Extension
1. Open Google Chrome
2. Type `chrome://extensions/` in the address bar
3. Toggle "Developer mode" ON (top right corner)
4. Click "Load unpacked" button
5. Navigate to and select the `Test-Case-and-Selector-Generator-Extension` folder
6. You should see the extension appear with a purple icon

### Step 2: Verify Installation
- Look for the SuperQA extension icon in your Chrome toolbar
- If you don't see it, click the puzzle piece icon and pin the SuperQA extension

## First Test Recording (3 minutes)

### Simple Example - Recording a Login Flow

1. **Open the Extension**
   - Click the SuperQA extension icon in your toolbar
   - The popup will appear

2. **Enter a Test Name**
   ```
   Test Name: User Login Test
   ```
   - This is required before you can start recording

3. **Start Recording**
   - Click the green "Start Recording" button
   - You'll see a message: "Recording started. Interact with the page."
   - The button will turn gray (disabled)
   - The Stop button will turn red (enabled)

4. **Perform Actions on Your Website**
   
   Go to your login page and:
   - Type your email: `test@example.com`
   - Type your password: `password123`
   - Click the Login button

5. **Watch Steps Appear**
   
   In the Test Output Viewer, you'll see:
   ```gherkin
   When I enter "test@example.com" into the "Email" field
   Locator: #email
   
   When I enter "password123" into the "Password" field
   Locator: #password
   
   When I click the "Login" button
   Locator: button[type="submit"]
   ```

6. **Stop Recording**
   - Click the red "Stop Recording" button
   - Recording ends, buttons return to normal state

7. **Send to SuperQA**
   - Click the purple "Send to SuperQA" button
   - Wait for success message
   - SuperQA will open in a new tab
   - Your test will be ready for execution!

## What Gets Recorded?

The extension captures:
- ✅ Button clicks
- ✅ Link clicks
- ✅ Text input in fields
- ✅ Password input
- ✅ Dropdown selections
- ✅ Checkbox checks/unchecks
- ✅ Radio button selections
- ✅ Form submissions

## Understanding the Output

Each recorded step shows:

```
┌─────────────────────────────────────────────┐
│ When I enter "value" into the "Label" field │
│ Locator: #fieldId                           │
└─────────────────────────────────────────────┘
```

- **First line**: Gherkin step in human-readable format
- **Second line**: CSS selector used to identify the element

## Tips for Best Results

### 1. Use Good Test Names
✅ Good: "User Login Test"
✅ Good: "Add Item to Cart"
❌ Bad: "Test1"
❌ Bad: "asdf"

### 2. Interact Naturally
- Click actual buttons, not just anywhere on the page
- Fill forms completely before submitting
- Wait for page loads between steps

### 3. For Better Locators
Add these to your HTML:
```html
<!-- Best: Use IDs -->
<button id="login-btn">Login</button>

<!-- Better: Use data-testid -->
<button data-testid="login-button">Login</button>

<!-- Good: Use aria-label -->
<button aria-label="Login">Login</button>
```

### 4. Review Before Sending
- Check that all steps are captured
- Verify locators look correct
- Use Clear button to re-record if needed

## Common Issues & Solutions

### Issue: "Please enter a test name before recording"
**Solution**: Fill in the Test Name field at the top

### Issue: Steps not appearing
**Solution**: 
1. Make sure recording is started (Stop button is red)
2. Refresh the page and try again
3. Check browser console for errors

### Issue: "Cannot send to SuperQA"
**Solution**: 
1. Ensure SuperQA backend is running on `localhost:7001`
2. Check that you have at least one recorded step
3. Verify test name is filled in

### Issue: Wrong locators captured
**Solution**: 
1. Add IDs or data-testid attributes to your elements
2. Re-record with better element attributes
3. Manually edit locators in SuperQA if needed

## Example Workflows

### E-commerce Checkout
```
Test Name: Complete Checkout Flow

Steps:
1. Search for product
2. Click on product
3. Click Add to Cart
4. Click Cart icon
5. Click Checkout
6. Fill shipping information
7. Select payment method
8. Click Place Order
```

### User Registration
```
Test Name: New User Registration

Steps:
1. Click Sign Up link
2. Enter email
3. Enter password
4. Confirm password
5. Accept terms checkbox
6. Click Create Account
```

### Content Creation
```
Test Name: Create New Blog Post

Steps:
1. Click New Post button
2. Enter post title
3. Enter post content
4. Select category
5. Add tags
6. Click Publish
```

## Next Steps

Once you've recorded your first test:

1. **Review in SuperQA**
   - The generated Playwright test will open automatically
   - Review the C# code
   - Make any necessary adjustments

2. **Execute the Test**
   - Click "Execute Test" in SuperQA
   - Watch the test run
   - Review results

3. **Iterate**
   - Record more tests
   - Build your test suite
   - Improve element selectors

## Keyboard Shortcuts

None yet, but coming soon!

## Getting Help

- **Documentation**: See README.md for full details
- **Testing Guide**: See TESTING_GUIDE.md for test cases
- **UI Guide**: See UI_WALKTHROUGH.md for interface details
- **Issues**: Report bugs on GitHub

## Pro Tips

💡 **Tip 1**: Record in small chunks
- Record login separately from checkout
- Easier to manage and reuse

💡 **Tip 2**: Use descriptive test names
- Include the feature and action
- "User Login - Valid Credentials"
- "User Login - Invalid Password"

💡 **Tip 3**: Review immediately
- Check steps right after recording
- Re-record while fresh in mind

💡 **Tip 4**: Clear and restart for errors
- Don't try to fix mid-recording
- Start fresh if something goes wrong

💡 **Tip 5**: Test on stable pages
- Avoid recording on pages with heavy animations
- Wait for dynamic content to load

## Congratulations! 🎉

You're now ready to use the SuperQA Browser Extension!

Start recording your first test and see how easy it is to automate your testing with SuperQA.

---

**Need more help?**
- Full documentation: README.md
- Technical details: BROWSER_EXTENSION_IMPLEMENTATION.md
- Testing guide: TESTING_GUIDE.md
- UI walkthrough: UI_WALKTHROUGH.md
