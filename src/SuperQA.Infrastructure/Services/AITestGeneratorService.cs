using SuperQA.Core.Entities;
using SuperQA.Core.Interfaces;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace SuperQA.Infrastructure.Services;

public class AITestGeneratorService : IAITestGeneratorService
{
    private readonly IMCPService _mcpService;
    private readonly HttpClient _httpClient;
    private const string OpenAIEndpoint = "https://api.openai.com/v1/chat/completions";

    public AITestGeneratorService(IMCPService mcpService, HttpClient httpClient)
    {
        _mcpService = mcpService;
        _httpClient = httpClient;
    }

    public async Task<IEnumerable<TestCase>> GenerateTestCasesAsync(Requirement requirement)
    {
        var prompt = $@"Generate comprehensive test cases for the following requirement:
Title: {requirement.Title}
Description: {requirement.Description}
Type: {requirement.Type}

Please generate test cases with:
- Title
- Description
- Preconditions
- Steps (numbered list)
- Expected Results

Return as JSON array.";

        var context = $"Project ID: {requirement.ProjectId}, Requirement ID: {requirement.Id}";
        
        try
        {
            var response = await _mcpService.SendPromptAsync(prompt, context);
            
            // For now, return a sample test case
            // In production, this would parse the AI response
            var testCase = new TestCase
            {
                ProjectId = requirement.ProjectId,
                RequirementId = requirement.Id,
                Title = $"Test case for: {requirement.Title}",
                Description = "AI-generated test case",
                Preconditions = "System is ready",
                Steps = "1. Navigate to application\n2. Perform action\n3. Verify result",
                ExpectedResults = "Expected outcome is achieved",
                IsAIGenerated = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            
            await _mcpService.LogPromptAsync(new AIPromptLog
            {
                PromptType = "TestGeneration",
                Prompt = prompt,
                Response = response,
                Model = "MCP",
                CreatedAt = DateTime.UtcNow,
                TokensUsed = prompt.Length + response.Length
            });
            
            return new[] { testCase };
        }
        catch
        {
            // Return empty if AI service is not available
            return Array.Empty<TestCase>();
        }
    }

    public async Task<string> GenerateAutomationScriptAsync(TestCase testCase, string framework, string? pageStructure = null)
    {
        // For now, this method requires an OpenAI API key which should be passed via environment variable or configuration
        // Since we don't have direct access to OpenAI key here, we'll use MCP as fallback
        // The proper implementation will be in the controller/API layer where we have access to the key
        
        var pageStructureSection = string.IsNullOrWhiteSpace(pageStructure) 
            ? "" 
            : $@"

ACTUAL PAGE ELEMENTS (USE THESE EXACT SELECTORS):
{pageStructure}

CRITICAL: You MUST use ONLY the selectors from the 'selector' field above. DO NOT invent or guess selectors.
Example: If you see {{""selector"": ""#loginBtn"", ""type"": ""button""}}, use await Page.ClickAsync(""#loginBtn"");
Example: If you see {{""selector"": ""input[name='username']"", ""type"": ""input""}}, use await Page.FillAsync(""input[name='username']"", ""value"");
DO NOT use generic selectors like 'button', 'input[type=""submit""]', or make up IDs that don't exist in the page structure.";

        var prompt = $@"Generate a {framework} automation script for the following test case:

Title: {testCase.Title}
Description: {testCase.Description}
Preconditions: {testCase.Preconditions}
Steps: {testCase.Steps}
Expected Results: {testCase.ExpectedResults}{pageStructureSection}

CRITICAL REQUIREMENTS:
1. Use ONLY these exact namespaces (DO NOT use PlaywrightSharp or any other variation):
   - using Microsoft.Playwright;
   - using Microsoft.Playwright.NUnit;
   - using NUnit.Framework;
2. Your test class MUST inherit from PageTest (from Microsoft.Playwright.NUnit)
3. MANDATORY: Use ONLY selectors from the page structure above - copy them exactly from the 'selector' field
4. Implement test actions (click, fill, navigate) per the test steps using the Page property from PageTest
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
    public async Task {SanitizeTestName(testCase.Title)}()
    {{
        // Implement the test steps here
        // Use await Page.GotoAsync(""URL"");
        // Use await Page.ClickAsync(""selector"");
        // Use await Page.FillAsync(""selector"", ""value"");
        // Use await Expect(Page).ToHaveTitleAsync(...);
    }}
}}
```

Output ONLY C# code following the template above, no explanations or markdown.";

        var context = $"TestCase ID: {testCase.Id}, Framework: {framework}";
        
        try
        {
            var response = await _mcpService.SendPromptAsync(prompt, context);
            
            // Clean up the response (remove markdown code blocks if present)
            response = response.Trim();
            if (response.StartsWith("```csharp"))
            {
                response = response.Substring("```csharp".Length);
            }
            else if (response.StartsWith("```"))
            {
                response = response.Substring("```".Length);
            }
            
            if (response.EndsWith("```"))
            {
                response = response.Substring(0, response.Length - 3);
            }
            
            return response.Trim();
        }
        catch
        {
            return $@"// Automation script for {testCase.Title}
// Framework: {framework}
// Note: Add actual implementation based on test steps";
        }
    }

    private static string SanitizeTestName(string title)
    {
        // Remove special characters and replace spaces with underscores
        var sanitized = new string(title.Where(c => char.IsLetterOrDigit(c) || c == ' ').ToArray());
        sanitized = sanitized.Replace(" ", "_");
        return string.IsNullOrWhiteSpace(sanitized) ? "GeneratedTest" : sanitized;
    }

    /// <summary>
    /// Extracts URL from test case text (steps, preconditions, or description)
    /// </summary>
    public string? ExtractUrlFromTestCase(TestCase testCase)
    {
        // Combine all text fields where a URL might be mentioned
        var textToSearch = $"{testCase.Preconditions}\n{testCase.Steps}\n{testCase.Description}";
        
        // Look for URLs in the text (http:// or https://)
        var urlPattern = @"https?://[^\s\)\]\>]+";
        var match = System.Text.RegularExpressions.Regex.Match(textToSearch, urlPattern, 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        
        if (match.Success)
        {
            return match.Value.TrimEnd('.', ',', ';'); // Remove trailing punctuation
        }
        
        return null;
    }
}
