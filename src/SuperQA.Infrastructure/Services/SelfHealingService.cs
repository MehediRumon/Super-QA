using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SuperQA.Core.Interfaces;
using SuperQA.Infrastructure.Data;

namespace SuperQA.Infrastructure.Services;

/// <summary>
/// Service for automatic self-healing of test locators when elements are not found
/// </summary>
public class SelfHealingService : ISelfHealingService
{
    private readonly SuperQADbContext _context;
    private readonly HttpClient _httpClient;
    private const string OpenAIEndpoint = "https://api.openai.com/v1/chat/completions";

    public SelfHealingService(SuperQADbContext context, HttpClient httpClient)
    {
        _context = context;
        _httpClient = httpClient;
    }

    public Task<string> SuggestUpdatedLocatorAsync(string failedLocator, string htmlStructure)
    {
        // For now, implement a simple fallback strategy
        // In a production scenario, this could use AI to analyze the HTML structure
        // and suggest the most robust locator
        
        if (string.IsNullOrWhiteSpace(htmlStructure))
        {
            // If no HTML structure provided, return a more generic but potentially more stable locator
            return Task.FromResult(ConvertToFallbackLocator(failedLocator));
        }

        // Parse HTML structure to find alternative locators
        var alternatives = ExtractAlternativeLocators(failedLocator, htmlStructure);
        
        // Return the most stable alternative (prefer id > data-testid > class > tag)
        return Task.FromResult(alternatives.FirstOrDefault() ?? ConvertToFallbackLocator(failedLocator));
    }

    public async Task<bool> UpdateLocatorAsync(int testCaseId, string oldLocator, string newLocator)
    {
        var testCase = await _context.TestCases.FindAsync(testCaseId);
        if (testCase == null)
        {
            return false;
        }

        // Update the test steps with the new locator
        if (!string.IsNullOrWhiteSpace(testCase.Steps))
        {
            testCase.Steps = testCase.Steps.Replace(oldLocator, newLocator);
        }

        // Update the automation script with the new locator
        if (!string.IsNullOrWhiteSpace(testCase.AutomationScript))
        {
            testCase.AutomationScript = testCase.AutomationScript.Replace(oldLocator, newLocator);
        }

        testCase.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return true;
    }

    private string ConvertToFallbackLocator(string failedLocator)
    {
        // Convert specific selectors to more generic fallback locators
        // This is a simple implementation - can be enhanced with AI
        
        if (failedLocator.StartsWith("#"))
        {
            // If ID selector failed, try data-testid or class
            var elementName = failedLocator.Substring(1);
            return $"[data-testid='{elementName}']";
        }
        else if (failedLocator.StartsWith("."))
        {
            // If class selector failed, try tag with class
            return failedLocator; // Keep as is for now
        }
        else if (failedLocator.Contains("["))
        {
            // Attribute selector - try to make it more generic
            return failedLocator; // Keep as is for now
        }
        
        return failedLocator;
    }

    private List<string> ExtractAlternativeLocators(string failedLocator, string htmlStructure)
    {
        var alternatives = new List<string>();
        
        // This is a simplified implementation
        // In production, parse the HTML and extract actual alternative selectors
        
        // For now, generate common alternatives
        if (failedLocator.StartsWith("#"))
        {
            var elementId = failedLocator.Substring(1);
            alternatives.Add($"[data-testid='{elementId}']");
            alternatives.Add($"[id='{elementId}']");
        }
        else if (failedLocator.StartsWith("."))
        {
            var className = failedLocator.Substring(1);
            alternatives.Add($"[class*='{className}']");
        }
        
        return alternatives;
    }
}
