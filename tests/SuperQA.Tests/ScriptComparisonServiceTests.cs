using SuperQA.Infrastructure.Services;

namespace SuperQA.Tests;

public class ScriptComparisonServiceTests
{
    [Fact]
    public void ExtractLocators_WithPlaywrightLocators_ExtractsAll()
    {
        // Arrange
        var service = new ScriptComparisonService();
        var script = @"
            await Page.Locator(""#username"").FillAsync(""test"");
            await Page.Locator(""//button[@id='submit']"").ClickAsync();
            await Page.GetByTestId(""login-btn"").ClickAsync();
        ";

        // Act
        var locators = service.ExtractLocators(script);

        // Assert
        Assert.Contains("Page.Locator(\"#username\")", locators);
        Assert.Contains("Page.Locator(\"//button[@id='submit']\")", locators);
        Assert.Contains("Page.GetByTestId(\"login-btn\")", locators);
        Assert.Equal(3, locators.Count);
    }

    [Fact]
    public void ExtractLocators_WithEmptyScript_ReturnsEmpty()
    {
        // Arrange
        var service = new ScriptComparisonService();

        // Act
        var locators = service.ExtractLocators("");

        // Assert
        Assert.Empty(locators);
    }

    [Fact]
    public void GetChangedLocators_WhenLocatorsChanged_ReturnsChanges()
    {
        // Arrange
        var service = new ScriptComparisonService();
        var originalScript = @"
            await Page.Locator(""#login"").ClickAsync();
            await Page.Locator(""#username"").FillAsync(""test"");
        ";
        var healedScript = @"
            await Page.GetByTestId(""login-btn"").ClickAsync();
            await Page.Locator(""#username"").FillAsync(""test"");
        ";
        var errorMessage = "Element not found: #login";

        // Act
        var changes = service.GetChangedLocators(originalScript, healedScript, errorMessage);

        // Assert
        Assert.Single(changes);
        Assert.Contains(changes, c => c.Original.Contains("#login"));
    }

    [Fact]
    public void GetChangedLocators_WhenNoChanges_ReturnsEmpty()
    {
        // Arrange
        var service = new ScriptComparisonService();
        var script = @"await Page.Locator(""#login"").ClickAsync();";
        var errorMessage = "Some error";

        // Act
        var changes = service.GetChangedLocators(script, script, errorMessage);

        // Assert
        Assert.Empty(changes);
    }

    [Fact]
    public void ValidateHealedScript_WhenOnlyFailingLocatorChanged_ReturnsTrue()
    {
        // Arrange
        var service = new ScriptComparisonService();
        var originalScript = @"
            await Page.Locator(""#UserName"").FillAsync(""user"");
            await Page.Locator(""#Password"").FillAsync(""pass"");
            await Page.Locator(""#SpecialElement"").ClickAsync();
        ";
        var healedScript = @"
            await Page.Locator(""#UserName"").FillAsync(""user"");
            await Page.Locator(""#Password"").FillAsync(""pass"");
            var element = Page.Locator(""#SpecialElement-fixed"");
            await element.WaitForAsync();
            await element.ClickAsync();
        ";
        var errorMessage = "Element not found: #SpecialElement";

        // Act
        var isValid = service.ValidateHealedScript(originalScript, healedScript, errorMessage);

        // Assert
        Assert.True(isValid); // Should pass because only the failing locator was changed
    }

    [Fact]
    public void ValidateHealedScript_WhenWorkingLocatorsChanged_ReturnsFalse()
    {
        // Arrange
        var service = new ScriptComparisonService();
        var originalScript = @"
            await Page.Locator(""#UserName"").FillAsync(""user"");
            await Page.Locator(""#Password"").FillAsync(""pass"");
            await Page.Locator(""#SpecialElement"").ClickAsync();
        ";
        // Healed script changes BOTH UserName AND SpecialElement (over-healing)
        var healedScript = @"
            await Page.GetByLabel(""User Name"").FillAsync(""user"");
            await Page.Locator(""#Password"").FillAsync(""pass"");
            await Page.Locator(""#SpecialElement-fixed"").ClickAsync();
        ";
        var errorMessage = "Element not found: #SpecialElement";

        // Act
        var isValid = service.ValidateHealedScript(originalScript, healedScript, errorMessage);

        // Assert
        Assert.False(isValid); // Should fail because #UserName was changed
    }

    [Fact]
    public void ValidateHealedScript_WithEmptyInputs_ReturnsTrue()
    {
        // Arrange
        var service = new ScriptComparisonService();

        // Act
        var isValid = service.ValidateHealedScript("", "", "");

        // Assert
        Assert.True(isValid); // Can't validate empty, so allow it
    }

    [Fact]
    public void ValidateHealedScript_WithNoError_ReturnsTrue()
    {
        // Arrange
        var service = new ScriptComparisonService();
        var script = @"await Page.Locator(""#login"").ClickAsync();";

        // Act
        var isValid = service.ValidateHealedScript(script, script, "");

        // Assert
        Assert.True(isValid); // Can't validate without error, so allow it
    }

    [Fact]
    public void ExtractLocators_WithGetByRoleMethods_ExtractsCorrectly()
    {
        // Arrange
        var service = new ScriptComparisonService();
        var script = @"
            await Page.GetByRole(AriaRole.Button, new() { Name = ""Submit"" }).ClickAsync();
            await Page.GetByLabel(""Username"").FillAsync(""test"");
        ";

        // Act
        var locators = service.ExtractLocators(script);

        // Assert
        Assert.Contains(locators, l => l.Contains("GetByRole"));
        Assert.Contains(locators, l => l.Contains("GetByLabel"));
    }

    [Fact]
    public void ExtractLocators_WithChainedLocators_ExtractsPageLocators()
    {
        // Arrange
        var service = new ScriptComparisonService();
        var script = @"
            var container = Page.Locator("".container"");
            await container.Locator(""button"").ClickAsync();
        ";

        // Act
        var locators = service.ExtractLocators(script);

        // Assert - Should at least extract the Page.Locator call
        Assert.Contains(locators, l => l.Contains(".container"));
    }
}
