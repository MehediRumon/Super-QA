using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using SuperQA.Core.Interfaces;

namespace SuperQA.Infrastructure.Services;

public class OpenAIService : IOpenAIService
{
    private readonly HttpClient _httpClient;
    private readonly ICSharpSyntaxValidationService _syntaxValidationService;
    private const string OpenAIEndpoint = "https://api.openai.com/v1/chat/completions";
    private const int MaxRetries = 2; // Maximum number of retries for syntax error fixes

    public OpenAIService(HttpClient httpClient, ICSharpSyntaxValidationService syntaxValidationService)
    {
        _httpClient = httpClient;
        _syntaxValidationService = syntaxValidationService;
    }

    public async Task<string> GeneratePlaywrightTestScriptAsync(string frsText, string applicationUrl, string apiKey, string model, string? pageStructure = null)
    {
        var pageStructureSection = string.IsNullOrWhiteSpace(pageStructure)
            ? string.Empty
            : $$"""

ACTUAL PAGE ELEMENTS
Format: [{ type, selector, alternatives[], role, name, id, tag, href, inputType, placeholder }]
Data:
{{pageStructure}}

CRITICAL SELECTOR POLICY:
- Prefer role+name when role and name are provided: Page.GetByRole(AriaRole.Button, new() { Name = "Name here" })
- Else prefer id: Page.Locator("#idValue")
- Else prefer data-testid/test/qa in selector/alternatives
- Else prefer input[name=], [placeholder=], [aria-label=]
- Else use the first provided 'selector' or an 'alternatives' entry
- DO NOT invent selectors; use only provided values
- Avoid generic selectors like 'button' or 'input' unless they came from selector list and nothing better exists

⚠️  CRITICAL: USE CORRECT AriaRole ENUM VALUES
- ✓ AriaRole.Combobox (NOT ComboBox - lowercase 'box')
- ✓ AriaRole.Radio (NOT RadioButton)
- ✓ AriaRole.Textbox (NOT TextBox - lowercase 'box')
- ✓ AriaRole.Checkbox, AriaRole.Button, AriaRole.Link, AriaRole.Listbox, AriaRole.Option, AriaRole.Searchbox
- See full list: Alert, Alertdialog, Application, Article, Banner, Blockquote, Button, Caption, Cell, Checkbox, Code, Columnheader, Combobox, Complementary, Contentinfo, Definition, Deletion, Dialog, Directory, Document, Emphasis, Feed, Figure, Form, Generic, Grid, Gridcell, Group, Heading, Img, Insertion, Link, List, Listbox, Listitem, Log, Main, Marquee, Math, Meter, Menu, Menubar, Menuitem, Menuitemcheckbox, Menuitemradio, Navigation, None, Note, Option, Paragraph, Presentation, Progressbar, Radio, Radiogroup, Region, Row, Rowgroup, Rowheader, Scrollbar, Search, Searchbox, Separator, Slider, Spinbutton, Status, Strong, Subscript, Superscript, Switch, Tab, Table, Tablist, Tabpanel, Term, Textbox, Time, Timer, Toolbar, Tooltip, Tree, Treegrid, Treeitem
""";

        var prompt = $$"""
Generate a Playwright test in C# (NUnit) for:

FRS: {{frsText}}
URL: {{applicationUrl}}{{pageStructureSection}}

CRITICAL REQUIREMENTS:
1) Generate COMPLETE, RUNNABLE C# code with NO syntax errors
2) Use Microsoft.Playwright and Microsoft.Playwright.NUnit with NUnit; class inherits from PageTest
3) Follow the CRITICAL SELECTOR POLICY strictly
4) Implement ALL actions and assertions based on FRS
5) If a step has a locator but NO test data (empty/missing value), you MUST generate appropriate test data
   - For email fields: use "test@example.com"
   - For username fields: use "testuser"
   - For password fields: use "Test@123"
   - For search/text fields: use descriptive test data based on field name
   - For numeric fields: use appropriate numbers
6) Use async/await properly with correct syntax
7) Return ONLY executable C# code with proper structure, no markdown fences
8) Include proper using statements: using Microsoft.Playwright; using Microsoft.Playwright.NUnit; using NUnit.Framework;
9) Class must be named with valid C# identifier (only letters, digits, underscore)
10) Test method must be named with valid C# identifier and have [Test] attribute

Example selector usage:
- Role+name: await Page.GetByRole(AriaRole.Button, new() { Name = "Login" }).ClickAsync();
- By id: await Page.Locator("#username").FillAsync("testuser");
- By data-testid: await Page.Locator("[data-testid=\"email\"]").FillAsync("test@example.com");
- By placeholder: await Page.Locator("input[placeholder=\"Email\"]").FillAsync("test@example.com");
- Fallback from provided 'selector': await Page.Locator("CSS_HERE").ClickAsync();

⚠️  CRITICAL ARIOLE VALUES - USE EXACT CASE:
✅ CORRECT: AriaRole.Combobox, AriaRole.Radio, AriaRole.Textbox, AriaRole.Checkbox
❌ WRONG: AriaRole.ComboBox, AriaRole.RadioButton, AriaRole.TextBox

IMPORTANT: The generated code MUST compile without errors. Every FillAsync/TypeAsync must have a non-empty string value.
""";

        var requestBody = new
        {
            model = model,
            messages = new[]
            {
                new { role = "system", content = "You are an expert Playwright test automation engineer. Generate COMPLETE, RUNNABLE C# code with NO syntax errors. Use Page.GetByRole when role+name exists; otherwise use provided selectors. Never invent selectors. Always provide test data for input fields - never leave them empty. The code must compile and run without errors. CRITICAL: Use correct AriaRole enum values: 'Combobox' NOT 'ComboBox', 'Radio' NOT 'RadioButton', 'Textbox' NOT 'TextBox'. Valid roles include: Button, Textbox, Combobox, Radio, Checkbox, Link, Listbox, Option, Searchbox, etc." },
                new { role = "user", content = prompt }
            },
            temperature = 0.2,
            max_tokens = 2000
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var response = await _httpClient.PostAsync(OpenAIEndpoint, content);
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            var errorMessage = response.StatusCode switch
            {
                System.Net.HttpStatusCode.TooManyRequests =>
                    "Rate limit exceeded. You've made too many requests to the OpenAI API. Please wait and try again, or check your quota at https://platform.openai.com/usage",
                System.Net.HttpStatusCode.Unauthorized =>
                    "Invalid API key. Please check your OpenAI API key and try again. Get your API key from https://platform.openai.com/api-keys",
                System.Net.HttpStatusCode.PaymentRequired =>
                    "Payment required. Your OpenAI account has insufficient credits. Please add credits at https://platform.openai.com/account/billing",
                System.Net.HttpStatusCode.InternalServerError =>
                    "OpenAI service error. Please try again later.",
                System.Net.HttpStatusCode.ServiceUnavailable =>
                    "OpenAI service unavailable. Please try again later.",
                _ =>
                    $"OpenAI API error ({response.StatusCode}): {errorContent}"
            };
            throw new HttpRequestException(errorMessage);
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        var jsonResponse = JsonDocument.Parse(responseContent);

        var generatedText = jsonResponse.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;

        generatedText = generatedText.Trim();
        if (generatedText.StartsWith("```csharp"))
            generatedText = generatedText.Substring("```csharp".Length);
        else if (generatedText.StartsWith("```"))
            generatedText = generatedText.Substring("```".Length);
        if (generatedText.EndsWith("```"))
            generatedText = generatedText[..^3];

        generatedText = generatedText.Trim();

        // Validate syntax and retry if needed
        var (isValid, detailedError) = _syntaxValidationService.ValidateSyntaxWithDetails(generatedText);
        if (!isValid)
        {
            // Try to regenerate with syntax error feedback
            for (int retry = 0; retry < MaxRetries; retry++)
            {
                var fixPrompt = $$"""
The previously generated code has syntax errors. Please fix them and regenerate.

{{detailedError}}

PREVIOUS CODE WITH ERRORS:
{{generatedText}}

Generate CORRECTED, COMPLETE, RUNNABLE C# code with NO syntax errors.
Return ONLY the fixed C# code (no markdown fences, no explanations).
""";

                var fixRequestBody = new
                {
                    model = model,
                    messages = new[]
                    {
                        new { role = "system", content = "You are an expert Playwright test automation engineer. Generate COMPLETE, RUNNABLE C# code with NO syntax errors. Fix any syntax errors in the code. The code must compile and run without errors. CRITICAL: Use correct AriaRole enum values: 'Combobox' NOT 'ComboBox', 'Radio' NOT 'RadioButton', 'Textbox' NOT 'TextBox'." },
                        new { role = "user", content = fixPrompt }
                    },
                    temperature = 0.1, // Lower temperature for more deterministic fixing
                    max_tokens = 2000
                };

                var fixJson = JsonSerializer.Serialize(fixRequestBody);
                var fixContent = new StringContent(fixJson, Encoding.UTF8, "application/json");

                var fixResponse = await _httpClient.PostAsync(OpenAIEndpoint, fixContent);
                if (!fixResponse.IsSuccessStatusCode)
                {
                    // If rate limit is hit during retry, provide more specific error message
                    if (fixResponse.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    {
                        throw new HttpRequestException(
                            "Initial test script generation succeeded but contained syntax errors. " +
                            "Rate limit exceeded while attempting to fix syntax errors. " +
                            "Please wait a few moments and try again, or check your quota at https://platform.openai.com/usage");
                    }
                    break; // Stop retrying if API fails
                }

                var fixResponseContent = await fixResponse.Content.ReadAsStringAsync();
                var fixJsonResponse = JsonDocument.Parse(fixResponseContent);

                var fixedText = fixJsonResponse.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString() ?? string.Empty;

                // Clean up markdown
                fixedText = fixedText.Trim();
                if (fixedText.StartsWith("```csharp"))
                    fixedText = fixedText.Substring("```csharp".Length);
                else if (fixedText.StartsWith("```"))
                    fixedText = fixedText.Substring("```".Length);
                if (fixedText.EndsWith("```"))
                    fixedText = fixedText[..^3];

                fixedText = fixedText.Trim();

                // Validate the fixed code
                var (fixedIsValid, _) = _syntaxValidationService.ValidateSyntaxWithDetails(fixedText);
                if (fixedIsValid)
                {
                    return fixedText; // Return the fixed code
                }

                generatedText = fixedText; // Update for next retry
            }

            // If all retries failed, throw an exception with details
            throw new InvalidOperationException(
                $"Failed to generate valid C# code after {MaxRetries} attempts. {detailedError}");
        }

        return generatedText;
    }

    public async Task<string> HealTestScriptAsync(string testScript, string? errorMessage, List<string>? executionLogs, string apiKey, string model)
    {
        var logsSection = executionLogs != null && executionLogs.Any()
            ? $"\n\nEXECUTION LOGS:\n{string.Join("\n", executionLogs)}"
            : string.Empty;

        var errorSection = !string.IsNullOrWhiteSpace(errorMessage)
            ? $"\n\nERROR MESSAGE:\n{errorMessage}"
            : string.Empty;

        var prompt = $$"""
You are an expert test automation engineer specializing in self-healing test scripts.

ORIGINAL TEST SCRIPT:
{{testScript}}{{errorSection}}{{logsSection}}

YOUR TASK:
Analyze the test failure and generate an IMPROVED, FIXED version of the test script that:
1. Addresses the root cause of the failure
2. Uses more robust selectors (prefer role+name, data-testid, or id over fragile CSS selectors)
3. Adds appropriate waits and error handling where needed
4. Maintains the same test intent and assertions
5. Is COMPLETE and RUNNABLE C# code with NO syntax errors

CRITICAL REQUIREMENTS:
- Return ONLY the complete, fixed C# code (no markdown fences, no explanations)
- Keep all using statements and class structure intact
- Use Microsoft.Playwright and Microsoft.Playwright.NUnit with NUnit
- Class must inherit from PageTest
- Follow Playwright best practices (explicit waits, robust selectors)
- If selectors are failing, suggest more stable alternatives (role-based, data attributes, etc.)
- Add .WaitForAsync() or .WaitForLoadStateAsync() where race conditions might occur
- If elements are not visible/enabled, add appropriate waits

Example improvements:
- Replace: Page.Locator("button").ClickAsync()
  With: Page.GetByRole(AriaRole.Button, new() { Name = "Submit" }).ClickAsync()
- Add waits: await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
- Add timeout: await element.ClickAsync(new() { Timeout = 10000 });
- Check visibility: await element.WaitForAsync(new() { State = WaitForSelectorState.Visible });

⚠️  CRITICAL ARIOLE VALUES - USE EXACT CASE:
✅ CORRECT: AriaRole.Combobox, AriaRole.Radio, AriaRole.Textbox, AriaRole.Checkbox
❌ WRONG: AriaRole.ComboBox, AriaRole.RadioButton, AriaRole.TextBox
Valid roles: Button, Textbox, Combobox, Radio, Checkbox, Link, Listbox, Option, Searchbox, etc.

Generate the COMPLETE, IMPROVED test script now:
""";

        var requestBody = new
        {
            model = model,
            messages = new[]
            {
                new { role = "system", content = "You are an expert test automation engineer with deep knowledge of self-healing test strategies. Analyze test failures and generate improved, resilient test scripts using Playwright best practices. Return ONLY executable C# code with NO markdown fences or explanations. CRITICAL: Use correct AriaRole enum values: 'Combobox' NOT 'ComboBox', 'Radio' NOT 'RadioButton', 'Textbox' NOT 'TextBox'. Valid roles include: Button, Textbox, Combobox, Radio, Checkbox, Link, Listbox, Option, Searchbox, etc." },
                new { role = "user", content = prompt }
            },
            temperature = 0.3,
            max_tokens = 2500
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var response = await _httpClient.PostAsync(OpenAIEndpoint, content);
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            var errorMessageResponse = response.StatusCode switch
            {
                System.Net.HttpStatusCode.TooManyRequests =>
                    "Rate limit exceeded. You've made too many requests to the OpenAI API. Please wait and try again.",
                System.Net.HttpStatusCode.Unauthorized =>
                    "Invalid API key. Please check your OpenAI API key and try again.",
                System.Net.HttpStatusCode.PaymentRequired =>
                    "Payment required. Your OpenAI account has insufficient credits.",
                System.Net.HttpStatusCode.InternalServerError =>
                    "OpenAI service error. Please try again later.",
                System.Net.HttpStatusCode.ServiceUnavailable =>
                    "OpenAI service unavailable. Please try again later.",
                _ =>
                    $"OpenAI API error ({response.StatusCode}): {errorContent}"
            };
            throw new HttpRequestException(errorMessageResponse);
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        var jsonResponse = JsonDocument.Parse(responseContent);

        var generatedText = jsonResponse.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;

        // Clean up markdown code blocks if present
        generatedText = generatedText.Trim();
        if (generatedText.StartsWith("```csharp"))
            generatedText = generatedText.Substring("```csharp".Length);
        else if (generatedText.StartsWith("```"))
            generatedText = generatedText.Substring("```".Length);
        if (generatedText.EndsWith("```"))
            generatedText = generatedText[..^3];

        generatedText = generatedText.Trim();

        // Validate syntax and retry if needed
        var (isValid, detailedError) = _syntaxValidationService.ValidateSyntaxWithDetails(generatedText);
        if (!isValid)
        {
            // Try to regenerate with syntax error feedback
            for (int retry = 0; retry < MaxRetries; retry++)
            {
                var fixPrompt = $$"""
The previously healed code has syntax errors. Please fix them and regenerate.

{{detailedError}}

ORIGINAL TEST SCRIPT:
{{testScript}}

ERROR MESSAGE:
{{errorMessage}}

PREVIOUS HEALED CODE WITH SYNTAX ERRORS:
{{generatedText}}

Generate CORRECTED, COMPLETE, RUNNABLE C# code with NO syntax errors.
Ensure all the healing improvements are preserved while fixing the syntax errors.
Return ONLY the fixed C# code (no markdown fences, no explanations).
""";

                var fixRequestBody = new
                {
                    model = model,
                    messages = new[]
                    {
                        new { role = "system", content = "You are an expert test automation engineer with deep knowledge of self-healing test strategies. Fix syntax errors while preserving healing improvements. Return ONLY executable C# code with NO markdown fences or explanations. CRITICAL: Use correct AriaRole enum values: 'Combobox' NOT 'ComboBox', 'Radio' NOT 'RadioButton', 'Textbox' NOT 'TextBox'." },
                        new { role = "user", content = fixPrompt }
                    },
                    temperature = 0.1, // Lower temperature for more deterministic fixing
                    max_tokens = 2500
                };

                var fixJson = JsonSerializer.Serialize(fixRequestBody);
                var fixContent = new StringContent(fixJson, Encoding.UTF8, "application/json");

                var fixResponse = await _httpClient.PostAsync(OpenAIEndpoint, fixContent);
                if (!fixResponse.IsSuccessStatusCode)
                {
                    // If rate limit is hit during retry, provide more specific error message
                    if (fixResponse.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    {
                        throw new HttpRequestException(
                            "Initial test healing succeeded but contained syntax errors. " +
                            "Rate limit exceeded while attempting to fix syntax errors. " +
                            "Please wait a few moments and try again, or check your quota at https://platform.openai.com/usage");
                    }
                    break; // Stop retrying if API fails
                }

                var fixResponseContent = await fixResponse.Content.ReadAsStringAsync();
                var fixJsonResponse = JsonDocument.Parse(fixResponseContent);

                var fixedText = fixJsonResponse.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString() ?? string.Empty;

                // Clean up markdown
                fixedText = fixedText.Trim();
                if (fixedText.StartsWith("```csharp"))
                    fixedText = fixedText.Substring("```csharp".Length);
                else if (fixedText.StartsWith("```"))
                    fixedText = fixedText.Substring("```".Length);
                if (fixedText.EndsWith("```"))
                    fixedText = fixedText[..^3];

                fixedText = fixedText.Trim();

                // Validate the fixed code
                var (fixedIsValid, _) = _syntaxValidationService.ValidateSyntaxWithDetails(fixedText);
                if (fixedIsValid)
                {
                    return fixedText; // Return the fixed code
                }

                generatedText = fixedText; // Update for next retry
            }

            // If all retries failed, throw an exception with details
            throw new InvalidOperationException(
                $"Failed to heal test script with valid C# code after {MaxRetries} attempts. {detailedError}");
        }

        return generatedText;
    }
}
