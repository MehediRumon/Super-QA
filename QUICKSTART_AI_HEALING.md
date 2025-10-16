# Quick Start: AI Test Healing with Auto-Apply

## 🎯 What is AI Test Healing?

When your automated tests fail, AI Test Healing analyzes the failure and generates an improved version of your test script. **NEW**: You can now apply the healed script with a single click!

## 🚀 5-Minute Quick Start

### Step 1: Run Your Tests

Navigate to your project and run tests:

```
Dashboard → Select Project → Test Executions → Run All Tests
```

### Step 2: Identify Failed Tests

Look for tests with a **red "Failed" badge** in the Test Executions list.

### Step 3: Click "AI Heal"

Click the **"AI Heal"** button next to any failed test.

### Step 4: Enter Your OpenAI API Key

In the healing dialog:
1. Enter your OpenAI API key (get one from [OpenAI Platform](https://platform.openai.com/api-keys))
2. Select an AI model (GPT-4 recommended)
3. Click **"Heal Test"**

### Step 5: Apply the Healed Script ✨

After the AI generates the healed script:
1. Review the improved script in the dialog
2. Click **"Apply Healed Script"** button
3. See the success confirmation
4. Close the dialog

### Step 6: Re-Run the Test

The healed script is now part of your test case. Click **"Run All Tests"** again to verify the fix works!

## 📋 Example Scenario

### Before Healing

**Test**: User Login  
**Status**: ❌ Failed  
**Error**: `Element not found: #login-button`

**Original Script**:
```csharp
await Page.ClickAsync("#login-button");
```

### After AI Healing & Auto-Apply

**Test**: User Login  
**Status**: ✅ Passed  
**Fix Applied**: More robust selector used

**Healed Script**:
```csharp
await Page.GetByRole(AriaRole.Button, new() { Name = "Login" }).ClickAsync();
```

## 🎨 UI Walkthrough

### 1. Test Executions Page

```
┌────────────────────────────────────────────────────────┐
│ Test Executions                           [Run All]    │
├────────────────────────────────────────────────────────┤
│ Test Case          │ Status  │ Duration │ Actions      │
├────────────────────┼─────────┼──────────┼──────────────┤
│ User Login         │ ❌ Failed│ 1234ms   │ [Details]    │
│                    │         │          │ [🪄 AI Heal] │
├────────────────────┼─────────┼──────────┼──────────────┤
│ User Registration  │ ✅ Passed│ 2345ms   │ [Details]    │
└────────────────────────────────────────────────────────┘
```

### 2. Healing Dialog - Enter Credentials

```
┌─────────────────────────────────────────────────────┐
│ AI Test Healing: User Login                    [×] │
├─────────────────────────────────────────────────────┤
│                                                     │
│ ℹ️  AI Test Healing will analyze this failure      │
│    and generate an improved script.                │
│                                                     │
│ OpenAI API Key *                                    │
│ [sk-......................................]         │
│                                                     │
│ AI Model                                            │
│ [GPT-4 (Recommended)             ▼]                │
│                                                     │
├─────────────────────────────────────────────────────┤
│                           [🪄 Heal Test]   [Close] │
└─────────────────────────────────────────────────────┘
```

### 3. Healing Dialog - Script Generated

```
┌─────────────────────────────────────────────────────┐
│ AI Test Healing: User Login                    [×] │
├─────────────────────────────────────────────────────┤
│                                                     │
│ ✅ Test healed successfully! Review the script.    │
│                                                     │
│ Healed Test Script                                  │
│ ┌─────────────────────────────────────────────────┐ │
│ │ using Microsoft.Playwright;                     │ │
│ │                                                 │ │
│ │ await Page.GetByRole(AriaRole.Button,          │ │
│ │     new() { Name = "Login" }).ClickAsync();    │ │
│ │                                                 │ │
│ └─────────────────────────────────────────────────┘ │
│                                                     │
│ ℹ️  You can apply it automatically or copy it.     │
│                                                     │
├─────────────────────────────────────────────────────┤
│     [✅ Apply Healed Script]  [📋 Copy]   [Close] │
└─────────────────────────────────────────────────────┘
```

### 4. Healing Dialog - Success Confirmation

```
┌─────────────────────────────────────────────────────┐
│ AI Test Healing: User Login                    [×] │
├─────────────────────────────────────────────────────┤
│                                                     │
│ ✅ Healed script applied successfully!              │
│                                                     │
│ Applied Script                                      │
│ ┌─────────────────────────────────────────────────┐ │
│ │ using Microsoft.Playwright;                     │ │
│ │                                                 │ │
│ │ await Page.GetByRole(AriaRole.Button,          │ │
│ │     new() { Name = "Login" }).ClickAsync();    │ │
│ │                                                 │ │
│ └─────────────────────────────────────────────────┘ │
│                                                     │
├─────────────────────────────────────────────────────┤
│                                          [Close]    │
└─────────────────────────────────────────────────────┘
```

## 💡 Tips & Best Practices

### ✅ DO
- Review the healed script before applying
- Test in a non-production environment first
- Use GPT-4 for best healing results
- Re-run the test immediately after applying

### ❌ DON'T
- Apply without reviewing the script
- Share your API key with others
- Expect 100% success rate (some issues are complex)
- Forget to verify the fix worked

## 🔍 What AI Can Fix

The AI Test Healing feature can fix:

✅ **Selector Issues**
- Element not found
- Changed element IDs/classes
- Dynamic selectors

✅ **Timing Issues**
- Elements not ready
- Async operations
- Page load timing

✅ **Navigation Issues**
- Incorrect URLs
- Redirect problems
- Page state issues

✅ **Data Issues**
- Incorrect test data
- Validation failures
- Input format problems

## 🆘 Troubleshooting

### "Invalid API Key"
- Verify your OpenAI API key is correct
- Check if the key has necessary permissions
- Ensure the key hasn't expired

### "Test Case Not Found"
- Refresh the page and try again
- Verify the test case still exists
- Check for database connection issues

### "Healing Failed"
- Try again with a different model
- Check your OpenAI account balance
- Ensure you have sufficient API credits

### "Script Applied But Test Still Fails"
- Review the healed script for logical errors
- Some issues may require manual debugging
- Try healing again with more context
- Consider manual script adjustments

## 📚 Learn More

- **Full Guide**: [AI_TEST_HEALING_GUIDE.md](AI_TEST_HEALING_GUIDE.md)
- **Feature Details**: [AI_HEALING_AUTO_APPLY_FEATURE.md](AI_HEALING_AUTO_APPLY_FEATURE.md)
- **Implementation**: [IMPLEMENTATION_AI_HEALING_AUTO_APPLY.md](IMPLEMENTATION_AI_HEALING_AUTO_APPLY.md)
- **Main Docs**: [README.md](README.md)

## 🎉 Benefits

With the new auto-apply feature, you'll enjoy:

- ⚡ **67% faster workflow** (9 steps → 6 steps)
- 🎯 **Zero copy/paste errors**
- 🚀 **One-click automation**
- ✅ **Immediate feedback**
- 📱 **Better user experience**

## 🤝 Need Help?

- Open an issue on GitHub
- Check the troubleshooting section
- Review the comprehensive documentation
- Contact the development team

---

**Happy Testing! 🚀**
