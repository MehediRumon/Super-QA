using SuperQA.Infrastructure.Services;

namespace SuperQA.Tests;

public class LocatorValidationServiceTests
{
    [Fact]
    public void IsLocatorValid_SameLocator_ReturnsTrue()
    {
        // Arrange
        var service = new LocatorValidationService();
        var locator = "#submit-button";

        // Act
        var result = service.IsLocatorValid(locator, locator);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsLocatorValid_CompatibleButtonTypes_ReturnsTrue()
    {
        // Arrange
        var service = new LocatorValidationService();
        var oldLocator = "#submit-button";
        var newLocator = "[data-testid='submit-btn']";

        // Act
        var result = service.IsLocatorValid(oldLocator, newLocator);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsLocatorValid_IncompatibleTypes_ReturnsFalse()
    {
        // Arrange
        var service = new LocatorValidationService();
        var oldLocator = "#login-button";
        var newLocator = "#username-input";

        // Act
        var result = service.IsLocatorValid(oldLocator, newLocator);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsLocatorValid_WithHtmlContext_ValidatesSameElement()
    {
        // Arrange
        var service = new LocatorValidationService();
        var oldLocator = "#submit";
        var newLocator = "[data-testid='submit']";
        var htmlContext = @"<button id=""submit"" data-testid=""submit"">Submit</button>";

        // Act
        var result = service.IsLocatorValid(oldLocator, newLocator, htmlContext);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasMismatchPatterns_GenericLocator_ReturnsTrue()
    {
        // Arrange
        var service = new LocatorValidationService();
        var locator = "button";
        var errorMessage = "Element not found";

        // Act
        var result = service.HasMismatchPatterns(locator, errorMessage);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasMismatchPatterns_SpecificLocator_ReturnsFalse()
    {
        // Arrange
        var service = new LocatorValidationService();
        var locator = "#submit-button";
        var errorMessage = "Element not found";

        // Act
        var result = service.HasMismatchPatterns(locator, errorMessage);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HasMismatchPatterns_ErrorMentionsButtonButLocatorIsInput_ReturnsTrue()
    {
        // Arrange
        var service = new LocatorValidationService();
        var locator = "#username-input";
        var errorMessage = "Button not found: submit-button";

        // Act
        var result = service.HasMismatchPatterns(locator, errorMessage);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsLocatorValid_RoleBasedLocators_ValidatesCorrectly()
    {
        // Arrange
        var service = new LocatorValidationService();
        var oldLocator = "#submit-btn";
        var newLocator = "AriaRole.Button";

        // Act
        var result = service.IsLocatorValid(oldLocator, newLocator);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsLocatorValid_EmptyLocators_ReturnsFalse()
    {
        // Arrange
        var service = new LocatorValidationService();

        // Act
        var result = service.IsLocatorValid("", "");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsLocatorValid_NullLocators_ReturnsFalse()
    {
        // Arrange
        var service = new LocatorValidationService();

        // Act
        var result = service.IsLocatorValid(null!, null!);

        // Assert
        Assert.False(result);
    }
}
