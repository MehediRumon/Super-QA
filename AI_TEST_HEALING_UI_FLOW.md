# AI Test Healing Feature - Visual Guide

## Feature Overview

The AI Test Healing feature adds an intelligent "AI Heal" button to failed test executions, allowing automatic analysis and repair of broken test scripts.

## User Interface Flow

### 1. Test Executions Page - Failed Test Detection

When tests fail, they are displayed with:
- ❌ **Failed** badge in red
- Error message and details
- **"AI Heal"** button (green, with magic wand icon) next to the Details button

```
+------------------------------------------------------------------+
| Test Executions                                                  |
+------------------------------------------------------------------+
| Test Case         | Status    | Duration | Executed At | Actions |
+------------------------------------------------------------------+
| Login Test        | ✅ Passed | 850ms    | 2 min ago  | 👁 Details  |
| Search Test       | ❌ Failed | 320ms    | 1 min ago  | 👁 Details  ✨ AI Heal |
| Checkout Test     | ✅ Passed | 1200ms   | 3 min ago  | 👁 Details  |
+------------------------------------------------------------------+
```

### 2. AI Healing Dialog - Initial State

Clicking "AI Heal" opens a modal with:

```
+------------------------------------------------------------------+
| AI Test Healing: Search Test                                [X] |
+------------------------------------------------------------------+
| ℹ️ AI Test Healing will analyze this test failure and generate  |
| an improved, more resilient test script.                        |
|                                                                  |
| The AI will examine the error messages, stack traces, and       |
| original test to suggest fixes for:                             |
| • Selector issues (elements not found)                          |
| • Timing problems (elements not ready)                          |
| • Navigation issues                                             |
| • Test data problems                                            |
+------------------------------------------------------------------+
|                                                                  |
| OpenAI API Key *                                                |
| [sk-...                                              ]           |
| Your API key is not stored and used only for this request.      |
|                                                                  |
| AI Model                                                        |
| [GPT-4 (Recommended)    ▼]                                      |
|                                                                  |
+------------------------------------------------------------------+
|                                      [✨ Heal Test]    [Close]  |
+------------------------------------------------------------------+
```

### 3. AI Healing Dialog - Processing State

While the AI analyzes and generates the fix:

```
+------------------------------------------------------------------+
| AI Test Healing: Search Test                                [X] |
+------------------------------------------------------------------+
|                                                                  |
|                         ⟳                                        |
|                   (spinner animation)                            |
|                                                                  |
| AI is analyzing the failure and generating a healed test        |
| script...                                                       |
|                                                                  |
+------------------------------------------------------------------+
|                                                        [Close]  |
+------------------------------------------------------------------+
```

### 4. AI Healing Dialog - Success State

After successful healing:

```
+------------------------------------------------------------------+
| AI Test Healing: Search Test                                [X] |
+------------------------------------------------------------------+
| ✅ Test healed successfully! Review the improved script below.  |
|                                                                  |
| Healed Test Script                                              |
| +--------------------------------------------------------------+ |
| | // Healed script with robust selectors                       | |
| | using Microsoft.Playwright;                                  | |
| | using Microsoft.Playwright.NUnit;                            | |
| | using NUnit.Framework;                                       | |
| |                                                              | |
| | namespace Tests;                                             | |
| |                                                              | |
| | public class SearchTest : PageTest                           | |
| | {                                                            | |
| |     [Test]                                                   | |
| |     public async Task TestSearch()                           | |
| |     {                                                        | |
| |         await Page.GotoAsync("https://example.com");        | |
| |                                                              | |
| |         // Use robust role+name selector instead of CSS     | |
| |         await Page.GetByRole(AriaRole.Textbox,              | |
| |             new() { Name = "Search" })                      | |
| |             .FillAsync("test query");                       | |
| |                                                              | |
| |         // Add explicit wait for element to be visible      | |
| |         await Page.GetByRole(AriaRole.Button,               | |
| |             new() { Name = "Search" })                      | |
| |             .ClickAsync();                                  | |
| |                                                              | |
| |         // Wait for results with proper timeout             | |
| |         await Page.WaitForSelectorAsync(                    | |
| |             "[data-testid='search-results']",               | |
| |             new() { State = WaitForSelectorState.Visible,   | |
| |                     Timeout = 5000 });                      | |
| |     }                                                        | |
| | }                                                            | |
| +--------------------------------------------------------------+ |
|                                                                  |
| ⚠️ Next Steps: Review the healed script above and manually     |
| update your test case. The AI has attempted to fix the issues, |
| but you should verify the changes before using them.           |
|                                                                  |
+------------------------------------------------------------------+
|                         [📋 Copy Script]              [Close]  |
+------------------------------------------------------------------+
```

## Key Features in the UI

### 1. Conditional Display
- "AI Heal" button only appears for **failed** tests
- Button uses Bootstrap success color (green) to stand out
- Magic wand icon (✨) indicates AI-powered feature

### 2. User Guidance
- Clear information about what AI healing does
- List of common issues the AI can fix
- Privacy note that API key is not stored

### 3. Model Selection
- Dropdown with recommended model (GPT-4)
- Options for GPT-4 Turbo and GPT-3.5 Turbo
- User can choose based on cost/quality tradeoff

### 4. Progress Indication
- Loading spinner during analysis
- Clear status message
- Disabled input while processing

### 5. Result Display
- Read-only textarea with healed script
- Syntax-highlighted code (monospace font)
- Warning message to review before applying
- Copy button for easy clipboard transfer

### 6. Error Handling
- Friendly error messages for API issues
- Guidance on what to do when errors occur
- Non-blocking - user can close and retry

## Integration Points

### In Test Execution Table
```razor
@if (execution.Status == "Failed")
{
    <button class="btn btn-sm btn-outline-success ms-2" 
            @onclick="() => ShowHealDialog(execution)">
        <i class="bi bi-magic"></i> AI Heal
    </button>
}
```

### In Execution Details Modal
The "AI Heal" button is also available in the detailed view of a failed execution, making it accessible from two places for user convenience.

## Data Flow

```
User Action: Click "AI Heal"
     ↓
Client: ShowHealDialog(execution)
     ↓
User Input: API Key + Model Selection
     ↓
Client: HealTest() → POST /api/testexecutions/heal
     ↓
Server: AITestHealingService.HealTestScriptAsync()
     ↓
OpenAI API: Analyze failure + Generate healed script
     ↓
Server: Return HealTestResponse
     ↓
Client: Display healed script
     ↓
User: Review + Copy + Apply
```

## Benefits

1. **User-Friendly**: Simple, intuitive interface
2. **Non-Intrusive**: Only appears when needed (for failed tests)
3. **Transparent**: Shows what's happening at each step
4. **Safe**: Requires user review before applying changes
5. **Accessible**: Available from both table view and detail view
6. **Informative**: Clear guidance and error messages

## Future Enhancements (Potential)

- Auto-apply healing with user confirmation
- Compare before/after scripts side-by-side
- History of healing attempts
- Success rate metrics for healed tests
- Batch healing for multiple failed tests
- Integration with version control for automatic PR creation
