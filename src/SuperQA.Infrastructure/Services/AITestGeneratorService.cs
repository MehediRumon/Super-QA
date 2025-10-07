using SuperQA.Core.Entities;
using SuperQA.Core.Interfaces;

namespace SuperQA.Infrastructure.Services;

public class AITestGeneratorService : IAITestGeneratorService
{
    private readonly IMCPService _mcpService;

    public AITestGeneratorService(IMCPService mcpService)
    {
        _mcpService = mcpService;
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

    public async Task<string> GenerateAutomationScriptAsync(TestCase testCase, string framework)
    {
        var prompt = $@"Generate a {framework} automation script for:
Title: {testCase.Title}
Steps: {testCase.Steps}
Expected Results: {testCase.ExpectedResults}";

        var context = $"TestCase ID: {testCase.Id}, Framework: {framework}";
        
        try
        {
            var response = await _mcpService.SendPromptAsync(prompt, context);
            return response;
        }
        catch
        {
            return $"// Automation script for {testCase.Title}\n// Framework: {framework}";
        }
    }
}
