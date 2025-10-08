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

ACTUAL PAGE ELEMENTS DETECTED:
{pageStructure}

IMPORTANT: Use the ACTUAL selectors from the page elements above. DO NOT create placeholder or generic selectors. Match the exact element selectors, IDs, names, and attributes from the detected page structure.";

        var prompt = $@"You are an expert test automation engineer. Generate a comprehensive Playwright test script in C# based on the following:

Functional Requirement Specification (FRS):
{frsText}

Application URL: {applicationUrl}{pageStructureSection}

Requirements for the generated script:
1. Create a complete C# Playwright test class using NUnit
2. Include necessary using statements
3. Use the ACTUAL element selectors from the page structure provided above (if available)
4. If page structure is provided, match element selectors exactly as they appear (by ID, name, type, or class)
5. Implement actions such as clicks, inputs, and navigation based on the FRS
6. Add assertions derived from the FRS to validate expected behavior
7. Use proper Playwright best practices (Page Object Model concepts where appropriate)
8. Include setup and teardown methods
9. Add meaningful test method names and comments
10. Use async/await patterns properly
11. Include proper error handling
12. DO NOT add comments like 'Modify selector as per actual...' - use the actual selectors provided

Generate ONLY the C# code without any additional explanation. The code should be production-ready and executable.";

        var requestBody = new
        {
            model = model,
            messages = new[]
            {
                new { role = "system", content = "You are an expert test automation engineer specializing in Playwright and C#." },
                new { role = "user", content = prompt }
            },
            temperature = 0.7,
            max_tokens = 2000
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
