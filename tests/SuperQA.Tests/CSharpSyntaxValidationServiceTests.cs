using SuperQA.Infrastructure.Services;

namespace SuperQA.Tests;

public class CSharpSyntaxValidationServiceTests
{
    [Fact]
    public void ValidateSyntax_ValidCode_ReturnsTrue()
    {
        // Arrange
        var service = new CSharpSyntaxValidationService();
        var validCode = @"
using System;

namespace Tests
{
    public class MyClass
    {
        public void MyMethod()
        {
            Console.WriteLine(""Hello"");
        }
    }
}";

        // Act
        var (isValid, errors) = service.ValidateSyntax(validCode);

        // Assert
        Assert.True(isValid);
        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateSyntax_InvalidCode_MissingSemicolon_ReturnsFalse()
    {
        // Arrange
        var service = new CSharpSyntaxValidationService();
        var invalidCode = @"
using System;

namespace Tests
{
    public class MyClass
    {
        public void MyMethod()
        {
            Console.WriteLine(""Hello"")
        }
    }
}";

        // Act
        var (isValid, errors) = service.ValidateSyntax(invalidCode);

        // Assert
        Assert.False(isValid);
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains("; expected"));
    }

    [Fact]
    public void ValidateSyntax_InvalidCode_MissingBrace_ReturnsFalse()
    {
        // Arrange
        var service = new CSharpSyntaxValidationService();
        var invalidCode = @"
using System;

namespace Tests
{
    public class MyClass
    {
        public void MyMethod()
        {
            Console.WriteLine(""Hello"");
        
    }
}";

        // Act
        var (isValid, errors) = service.ValidateSyntax(invalidCode);

        // Assert
        Assert.False(isValid);
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains("} expected"));
    }

    [Fact]
    public void ValidateSyntax_EmptyCode_ReturnsFalse()
    {
        // Arrange
        var service = new CSharpSyntaxValidationService();
        var emptyCode = "";

        // Act
        var (isValid, errors) = service.ValidateSyntax(emptyCode);

        // Assert
        Assert.False(isValid);
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains("empty or whitespace"));
    }

    [Fact]
    public void ValidateSyntaxWithDetails_InvalidCode_ReturnsDetailedMessage()
    {
        // Arrange
        var service = new CSharpSyntaxValidationService();
        var invalidCode = @"
using System;

namespace Tests
{
    public class MyClass
    {
        public void MyMethod()
        {
            Console.WriteLine(""Hello"")
        }
    }
}";

        // Act
        var (isValid, detailedError) = service.ValidateSyntaxWithDetails(invalidCode);

        // Assert
        Assert.False(isValid);
        Assert.Contains("syntax errors", detailedError);
        Assert.Contains("semicolons", detailedError);
    }

    [Fact]
    public void GetDetailedErrors_InvalidCode_ReturnsErrorDetails()
    {
        // Arrange
        var service = new CSharpSyntaxValidationService();
        var invalidCode = @"
using System;

namespace Tests
{
    public class MyClass
    {
        public void MyMethod()
        {
            Console.WriteLine(""Hello"")
        }
    }
}";

        // Act
        var errors = service.GetDetailedErrors(invalidCode);

        // Assert
        Assert.NotEmpty(errors);
        var error = errors.First();
        Assert.True(error.LineNumber > 0);
        Assert.NotEmpty(error.ErrorCode);
        Assert.NotEmpty(error.ErrorMessage);
        Assert.NotEmpty(error.LineContent);
    }

    [Fact]
    public void ValidateSyntax_PlaywrightTestCode_ReturnsTrue()
    {
        // Arrange
        var service = new CSharpSyntaxValidationService();
        var playwrightCode = @"
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace PlaywrightTests;

[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class Tests : PageTest
{
    [Test]
    public async Task LoginTest()
    {
        await Page.GotoAsync(""https://example.com"");
        await Page.Locator(""#username"").FillAsync(""testuser"");
        await Page.Locator(""#password"").FillAsync(""testpass"");
        await Page.GetByRole(AriaRole.Button, new() { Name = ""Login"" }).ClickAsync();
        await Expect(Page).ToHaveTitleAsync(""Dashboard"");
    }
}";

        // Act
        var (isValid, errors) = service.ValidateSyntax(playwrightCode);

        // Assert
        Assert.True(isValid);
        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateSyntax_PlaywrightTestCodeWithSyntaxError_ReturnsFalse()
    {
        // Arrange
        var service = new CSharpSyntaxValidationService();
        var playwrightCode = @"
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace PlaywrightTests;

[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class Tests : PageTest
{
    [Test]
    public async Task LoginTest()
    {
        await Page.GotoAsync(""https://example.com"")
        await Page.Locator(""#username"").FillAsync(""testuser"");
        await Page.Locator(""#password"").FillAsync(""testpass"");
        await Page.GetByRole(AriaRole.Button, new() { Name = ""Login"" }).ClickAsync();
        await Expect(Page).ToHaveTitleAsync(""Dashboard"");
    }
}";

        // Act
        var (isValid, errors) = service.ValidateSyntax(playwrightCode);

        // Assert
        Assert.False(isValid);
        Assert.NotEmpty(errors);
    }
}
