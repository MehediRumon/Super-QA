using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using SuperQA.Core.Interfaces;

namespace SuperQA.Infrastructure.Services;

public class OpenAIService : IOpenAIService
{
    private readonly HttpClient _httpClient;
    private const string OpenAIEndpoint = "https://api.openai.com/v1/chat/completions";

    public OpenAIService(HttpClient httpClient)
    {
        _httpClient = httpClient;
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

IMPORTANT: The generated code MUST compile without errors. Every FillAsync/TypeAsync must have a non-empty string value.
""";

        var requestBody = new
        {
            model = model,
            messages = new[]
            {
                new { role = "system", content = "You are an expert Playwright test automation engineer. Generate COMPLETE, RUNNABLE C# code with NO syntax errors. Use Page.GetByRole when role+name exists; otherwise use provided selectors. Never invent selectors. Always provide test data for input fields - never leave them empty. The code must compile and run without errors." },
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

        return generatedText.Trim();
    }
}
