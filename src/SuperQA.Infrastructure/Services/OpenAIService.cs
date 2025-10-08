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
            ? "" 
            : $@"

ACTUAL PAGE ELEMENTS (USE THESE EXACT SELECTORS):
{pageStructure}

CRITICAL: You MUST use ONLY the selectors from the 'selector' field above. DO NOT invent or guess selectors.
Example: If you see {{""selector"": ""#loginBtn"", ""type"": ""button""}}, use await page.ClickAsync(""#loginBtn"");
Example: If you see {{""selector"": ""input[name='username']"", ""type"": ""input""}}, use await page.FillAsync(""input[name='username']"", ""value"");
DO NOT use generic selectors like 'button', 'input[type=""submit""]', or make up IDs that don't exist in the page structure.";

        var prompt = $@"Generate a Playwright test in C# (NUnit) for:

FRS: {frsText}
URL: {applicationUrl}{pageStructureSection}

CRITICAL REQUIREMENTS:
1. Use ONLY these exact namespaces (DO NOT use PlaywrightSharp or any other variation):
   - using Microsoft.Playwright;
   - using Microsoft.Playwright.NUnit;
   - using NUnit.Framework;
2. Your test class MUST inherit from PageTest (from Microsoft.Playwright.NUnit)
3. MANDATORY: Use ONLY selectors from the page structure above - copy them exactly from the 'selector' field
4. Implement test actions (click, fill, navigate) per FRS using the Page property from PageTest
5. Add assertions for expected behavior using Expect from Microsoft.Playwright
6. Use async/await properly
7. NO placeholder comments like 'Modify selector...'
8. Production-ready, executable code

REQUIRED CODE STRUCTURE (follow this template exactly):
```csharp
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace PlaywrightTests;

[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class Tests : PageTest
{{
    [Test]
    public async Task YourTestName()
    {{
        await Page.GotoAsync(""URL_HERE"");
        // Your test steps using Page property
        // Use await Page.ClickAsync(""selector"");
        // Use await Page.FillAsync(""selector"", ""value"");
        // Use await Expect(Page).ToHaveTitleAsync(...);
    }}
}}
```

Output ONLY C# code following the template above, no explanations or markdown.";

        var requestBody = new
        {
            model = model,
            messages = new[]
            {
                new { role = "system", content = "You are a Playwright test automation engineer. CRITICAL: Use ONLY Microsoft.Playwright namespace (NOT PlaywrightSharp). Test class MUST inherit from PageTest. Use ONLY selectors explicitly provided in the page structure." },
                new { role = "user", content = prompt }
            },
            temperature = 0.3,
            max_tokens = 1500
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var response = await _httpClient.PostAsync(OpenAIEndpoint, content);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            
            // Handle specific error status codes with user-friendly messages
            var errorMessage = response.StatusCode switch
            {
                System.Net.HttpStatusCode.TooManyRequests => 
                    "Rate limit exceeded. You've made too many requests to the OpenAI API. Please wait a moment and try again, or check your OpenAI account quota at https://platform.openai.com/usage",
                System.Net.HttpStatusCode.Unauthorized => 
                    "Invalid API key. Please check your OpenAI API key and try again. Get your API key from https://platform.openai.com/api-keys",
                System.Net.HttpStatusCode.PaymentRequired => 
                    "Payment required. Your OpenAI account has insufficient credits. Please add credits to your account at https://platform.openai.com/account/billing",
                System.Net.HttpStatusCode.InternalServerError => 
                    "OpenAI service error. The OpenAI API is experiencing issues. Please try again in a few moments.",
                System.Net.HttpStatusCode.ServiceUnavailable => 
                    "OpenAI service unavailable. The OpenAI API is temporarily unavailable. Please try again in a few moments.",
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

        // Clean up the response (remove markdown code blocks if present)
        generatedText = generatedText.Trim();
        if (generatedText.StartsWith("```csharp"))
        {
            generatedText = generatedText.Substring("```csharp".Length);
        }
        else if (generatedText.StartsWith("```"))
        {
            generatedText = generatedText.Substring("```".Length);
        }
        
        if (generatedText.EndsWith("```"))
        {
            generatedText = generatedText.Substring(0, generatedText.Length - 3);
        }

        return generatedText.Trim();
    }
}
