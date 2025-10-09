using Microsoft.Extensions.Configuration;
using SuperQA.Infrastructure.Services;
using System.Reflection;

namespace SuperQA.Tests;

public class PageInspectorKeywordExtractionTests
{
    [Fact]
    public void ExtractKeywords_WithComplexFRS_ExtractsRelevantKeywords()
    {
        // Arrange
        var frs = @"Navigate to URL https://ums.osl.team/Account/Login
Enter User Email rumon.onnorokom@gmail.com
Enter Password Mrumon4726
Click on Login Button
Click on Administration in Navbar
Click on Branch in sub menu
Click on Add Branch sub menu
Select UDVASH from Organization dropdown
Enter Name Mrumon4726
Enter Short Mrumon4726
Select Kishoreganj from District Search Suggestion";

        // Act
        var keywords = InvokeExtractKeywords(frs);

        // Assert
        Assert.NotNull(keywords);
        Assert.NotEmpty(keywords);
        
        // Should extract meaningful keywords from the FRS
        // These are actual words from the FRS that should be extracted
        var expectedKeywords = new[] { 
            "administration", "branch", "organization", "district", 
            "login", "navbar", "menu", "dropdown", "udvash", "kishoreganj"
        };
        
        var extractedLower = keywords.Select(k => k.ToLowerInvariant()).ToArray();
        
        // Verify at least some of the expected keywords are present
        var matchCount = expectedKeywords.Count(ek => extractedLower.Any(k => k.Contains(ek)));
        Assert.True(matchCount >= 5, 
            $"Expected at least 5 matching keywords, but found {matchCount}. " +
            $"Extracted: {string.Join(", ", keywords.Take(20))}");
    }

    [Fact]
    public void ExtractKeywords_WithSimpleLoginFRS_ExtractsLoginRelatedKeywords()
    {
        // Arrange
        var frs = @"User Login Feature:
1. User should be able to navigate to the login page
2. User should see username and password fields
3. User should be able to enter credentials
4. Upon clicking Login, user should be authenticated";

        // Act
        var keywords = InvokeExtractKeywords(frs);

        // Assert
        Assert.NotNull(keywords);
        Assert.NotEmpty(keywords);
        
        var extractedLower = keywords.Select(k => k.ToLowerInvariant()).ToArray();
        
        // Should include login-related keywords
        Assert.Contains(extractedLower, k => k.Contains("login"));
        Assert.Contains(extractedLower, k => k.Contains("username") || k.Contains("user"));
        Assert.Contains(extractedLower, k => k.Contains("password"));
    }

    [Fact]
    public void ExtractKeywords_WithEmptyFRS_ReturnsEmpty()
    {
        // Act
        var keywords = InvokeExtractKeywords("");

        // Assert
        Assert.NotNull(keywords);
        Assert.Empty(keywords);
    }

    [Fact]
    public void ExtractKeywords_WithNullFRS_ReturnsEmpty()
    {
        // Act
        var keywords = InvokeExtractKeywords(null);

        // Assert
        Assert.NotNull(keywords);
        Assert.Empty(keywords);
    }

    [Fact]
    public void ExtractKeywords_FiltersOutStopWords()
    {
        // Arrange
        var frs = "The user should click on the button and then enter the password";

        // Act
        var keywords = InvokeExtractKeywords(frs);

        // Assert
        var extractedLower = keywords.Select(k => k.ToLowerInvariant()).ToArray();
        
        // Should not contain stop words
        Assert.DoesNotContain(extractedLower, k => k == "the");
        Assert.DoesNotContain(extractedLower, k => k == "and");
        Assert.DoesNotContain(extractedLower, k => k == "then");
        Assert.DoesNotContain(extractedLower, k => k == "should");
        
        // Should contain meaningful words
        Assert.Contains(extractedLower, k => k.Contains("user") || k.Contains("button") || k.Contains("password"));
    }

    // Helper method to invoke private ExtractKeywords method using reflection
    private string[] InvokeExtractKeywords(string? frs)
    {
        var service = typeof(PageInspectorService);
        var method = service.GetMethod("ExtractKeywords", 
            BindingFlags.NonPublic | BindingFlags.Static);
        
        Assert.NotNull(method);
        
        var result = method.Invoke(null, new object?[] { frs });
        return (string[])result!;
    }
}
