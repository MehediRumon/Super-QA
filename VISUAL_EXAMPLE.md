# Visual Example: Warning Display in Playwright Test Generator

## UI Layout

### Before the Fix
```
┌─────────────────────────────────────────────────────────────┐
│  🎭 AI-Powered Playwright Test Generator                   │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Configuration & Requirements        Generated Test Script │
│  ┌──────────────────────────┐       ┌───────────────────┐ │
│  │ OpenAI API Key: ******** │       │                   │ │
│  │ AI Model: GPT-4o Mini    │       │ [Generated code   │ │
│  │ Application URL: ...     │       │  appears here]    │ │
│  │ FRS: ...                 │       │                   │ │
│  │                          │       │                   │ │
│  │ [🤖 Generate Test]       │       │                   │ │
│  │                          │       │                   │ │
│  └──────────────────────────┘       └───────────────────┘ │
│                                                             │
│  ❌ No warning shown when page inspection fails!           │
│     User doesn't know if actual elements were used         │
└─────────────────────────────────────────────────────────────┘
```

### After the Fix
```
┌─────────────────────────────────────────────────────────────┐
│  🎭 AI-Powered Playwright Test Generator                   │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Configuration & Requirements        Generated Test Script │
│  ┌──────────────────────────┐       ┌───────────────────┐ │
│  │ OpenAI API Key: ******** │       │                   │ │
│  │ AI Model: GPT-4o Mini    │       │ using Microsoft   │ │
│  │ Application URL: ...     │       │   .Playwright;    │ │
│  │ FRS: ...                 │       │                   │ │
│  │                          │       │ [Test]            │ │
│  │ [🤖 Generate Test]       │       │ public async Task │ │
│  │                          │       │ ...               │ │
│  │ ┌────────────────────┐   │       │                   │ │
│  │ │ ⚠️ WARNING         │   │       │                   │ │
│  │ │ Page inspection    │   │       │ [▶️ Execute Test] │ │
│  │ │ failed. The AI     │   │       │                   │ │
│  │ │ will generate test │   │       └───────────────────┘ │
│  │ │ scripts with       │   │                             │
│  │ │ generic selectors. │   │                             │
│  │ │ For best results,  │   │                             │
│  │ │ ensure Playwright  │   │                             │
│  │ │ browsers are       │   │                             │
│  │ │ installed (run     │   │                             │
│  │ │ 'playwright        │   │                             │
│  │ │ install chromium').│   │                             │
│  │ └────────────────────┘   │                             │
│  └──────────────────────────┘                             │
│                                                             │
│  ✅ Warning clearly displayed in yellow alert box!         │
│     User knows what happened and how to fix it             │
└─────────────────────────────────────────────────────────────┘
```

## HTML/CSS Structure

The warning is rendered as a Bootstrap alert component:

```html
<div class="alert alert-warning mt-3" role="alert">
    <div>⚠️ Page inspection failed. The AI will generate test scripts with generic selectors. For best results, ensure Playwright browsers are installed (run 'playwright install chromium').</div>
</div>
```

## Visual Appearance

### Colors
- **Background**: Light yellow (#fff3cd)
- **Border**: Yellow (#ffecb5)
- **Text**: Dark brown/orange (#664d03)
- **Icon**: ⚠️ Warning emoji

### Styling
- **Margin Top**: 3 spacing units (mt-3)
- **Padding**: Bootstrap default alert padding
- **Border Radius**: Rounded corners
- **Display**: Block level element
- **Role**: "alert" for accessibility

## User Flow

### Scenario 1: Browsers Not Installed (Warning Shown)

1. User fills in form:
   - OpenAI API Key: `sk-abc123...`
   - Application URL: `https://example.com`
   - FRS: "Login feature test..."

2. User clicks "🤖 Generate Test Script"

3. System attempts to inspect page
   - ❌ Browser not found
   - Sets warning message
   - Continues with generic selectors

4. Response received:
   ```json
   {
     "success": true,
     "generatedScript": "using Microsoft.Playwright; ...",
     "warnings": [
       "⚠️ Page inspection failed. The AI will generate test scripts with generic selectors. For best results, ensure Playwright browsers are installed (run 'playwright install chromium')."
     ]
   }
   ```

5. UI updates:
   - ✅ Generated script appears in right panel
   - ⚠️ **Yellow warning box appears below form**
   - User sees clear actionable message

### Scenario 2: Browsers Installed (No Warning)

1. User fills in form (same as above)

2. User clicks "🤖 Generate Test Script"

3. System inspects page successfully
   - ✅ Launches Chromium
   - ✅ Extracts actual elements: `#loginBtn`, `#username`, etc.
   - ✅ Sends real selectors to AI

4. Response received:
   ```json
   {
     "success": true,
     "generatedScript": "using Microsoft.Playwright; ...",
     "warnings": null
   }
   ```

5. UI updates:
   - ✅ Generated script appears in right panel
   - ✅ **No warning displayed** (warnings = null)
   - User sees script with actual element selectors

## Accessibility

- **ARIA role**: `alert` for screen readers
- **Semantic HTML**: Uses `<div>` with proper Bootstrap classes
- **Visual hierarchy**: Warning color clearly distinguishes from errors (red) and success (green)
- **Keyboard navigation**: Warning is part of normal document flow

## Responsive Behavior

- Warning appears in left column on desktop (col-md-6)
- Full width on mobile devices
- Maintains readability at all screen sizes
- Text wraps appropriately

## Multiple Warnings

If multiple warnings exist, they are displayed sequentially:

```html
<div class="alert alert-warning mt-3" role="alert">
    <div>⚠️ Page inspection failed...</div>
    <div>⚠️ Some elements may not be accessible...</div>
</div>
```

Each warning appears on its own line within the same alert box.
