using System.Net;
using Microsoft.EntityFrameworkCore;
using Moq;
using Moq.Protected;
using SuperQA.Core.Entities;
using SuperQA.Core.Interfaces;
using SuperQA.Infrastructure.Data;
using SuperQA.Infrastructure.Services;

namespace SuperQA.Tests;

/// <summary>
/// Tests for improved AI healing validation and protection mechanisms
/// </summary>
public class AIHealingImprovementsTests
{
    private SuperQADbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<SuperQADbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        return new SuperQADbContext(options);
    }

    private ILocatorValidationService CreateMockValidationService(bool isValid = true, bool hasMismatch = false)
    {
        var mock = new Mock<ILocatorValidationService>();
        mock.Setup(v => v.IsLocatorValid(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(isValid);
        mock.Setup(v => v.HasMismatchPatterns(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(hasMismatch);
        return mock.Object;
    }

    private IScriptComparisonService CreateMockComparisonService()
    {
        var mock = new Mock<IScriptComparisonService>();
        // Default behavior: validation passes (healed script is valid)
        mock.Setup(s => s.ValidateHealedScript(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);
        mock.Setup(s => s.GetChangedLocators(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new List<(string, string)>());
        mock.Setup(s => s.ExtractLocators(It.IsAny<string>()))
            .Returns(new List<string>());
        return mock.Object;
    }

    [Fact]
    public async Task HealTestScriptAsync_RejectsOverHealing_WhenTooManyLocatorsChanged()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var project = new Project { Id = 1, Name = "Test Project" };
        var testCase = new TestCase
        {
            Id = 1,
            ProjectId = 1,
            Title = "Test Case",
            Description = "Test",
            Steps = "Multiple steps",
            ExpectedResults = "Success",
            // Original script has 5 locators
            AutomationScript = @"
                await Page.ClickAsync(""#login"");
                await Page.FillAsync(""#username"", ""test"");
                await Page.FillAsync(""#password"", ""pass"");
                await Page.ClickAsync(""#submit"");
                await Page.ClickAsync(""#confirm"");
            ",
            Project = project
        };
        var execution = new TestExecution
        {
            Id = 1,
            TestCaseId = 1,
            ProjectId = 1,
            Status = "Failed",
            ErrorMessage = "Element not found: #login",
            TestCase = testCase,
            Project = project
        };

        context.Projects.Add(project);
        context.TestCases.Add(testCase);
        context.TestExecutions.Add(execution);
        await context.SaveChangesAsync();

        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        // AI response changes ALL 5 locators (over-healing)
        var jsonContent = @"{""choices"": [{""message"": {""content"": ""await Page.GetByRole(AriaRole.Button, new() { Name = \""Login\"" }).ClickAsync();\nawait Page.GetByLabel(\""Username\"").FillAsync(\""test\"");\nawait Page.GetByLabel(\""Password\"").FillAsync(\""pass\"");\nawait Page.GetByRole(AriaRole.Button, new() { Name = \""Submit\"" }).ClickAsync();\nawait Page.GetByRole(AriaRole.Button, new() { Name = \""Confirm\"" }).ClickAsync();""}}]}";
        var mockResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(jsonContent)
        };

        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(mockResponse);

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var validationService = CreateMockValidationService();
        
        // Setup comparison service to detect over-healing (4 working locators changed)
        var comparisonMock = new Mock<IScriptComparisonService>();
        comparisonMock.Setup(s => s.ValidateHealedScript(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(false); // Validation fails due to over-healing
        comparisonMock.Setup(s => s.GetChangedLocators(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new List<(string, string)> {
                ("Page.ClickAsync(\"#login\")", "Page.GetByRole(AriaRole.Button, new() { Name = \"Login\" }).ClickAsync()"),
                ("Page.FillAsync(\"#username\", \"test\")", "Page.GetByLabel(\"Username\").FillAsync(\"test\")"),
                ("Page.FillAsync(\"#password\", \"pass\")", "Page.GetByLabel(\"Password\").FillAsync(\"pass\")"),
                ("Page.ClickAsync(\"#submit\")", "Page.GetByRole(AriaRole.Button, new() { Name = \"Submit\" }).ClickAsync()")
            });
        var comparisonService = comparisonMock.Object;
        
        var service = new AITestHealingService(context, httpClient, validationService, comparisonService);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.HealTestScriptAsync(1, 1, "test-api-key", "gpt-4"));

        Assert.Contains("changed working locators", exception.Message);
    }

    [Fact]
    public async Task HealTestScriptAsync_AcceptsTargetedHealing_WhenOnlyFailingLocatorChanged()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var project = new Project { Id = 1, Name = "Test Project" };
        var testCase = new TestCase
        {
            Id = 1,
            ProjectId = 1,
            Title = "Test Case",
            Description = "Test",
            Steps = "Multiple steps",
            ExpectedResults = "Success",
            // Original script has 5 locators
            AutomationScript = @"
                await Page.ClickAsync(""#login"");
                await Page.FillAsync(""#username"", ""test"");
                await Page.FillAsync(""#password"", ""pass"");
                await Page.ClickAsync(""#submit"");
                await Page.ClickAsync(""#confirm"");
            ",
            Project = project
        };
        var execution = new TestExecution
        {
            Id = 1,
            TestCaseId = 1,
            ProjectId = 1,
            Status = "Failed",
            ErrorMessage = "Element not found: #login",
            TestCase = testCase,
            Project = project
        };

        context.Projects.Add(project);
        context.TestCases.Add(testCase);
        context.TestExecutions.Add(execution);
        await context.SaveChangesAsync();

        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        // AI response changes ONLY the failing locator (targeted healing)
        var jsonContent = @"{""choices"": [{""message"": {""content"": ""await Page.GetByRole(AriaRole.Button, new() { Name = \""Login\"" }).ClickAsync();\nawait Page.FillAsync(\""#username\"", \""test\"");\nawait Page.FillAsync(\""#password\"", \""pass\"");\nawait Page.ClickAsync(\""#submit\"");\nawait Page.ClickAsync(\""#confirm\"");""}}]}";
        var mockResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(jsonContent)
        };

        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(mockResponse);

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var validationService = CreateMockValidationService();
        var comparisonService = CreateMockComparisonService();
        var service = new AITestHealingService(context, httpClient, validationService, comparisonService);

        // Act
        var result = await service.HealTestScriptAsync(1, 1, "test-api-key", "gpt-4");

        // Assert
        Assert.Contains("GetByRole", result);
        Assert.Contains("#username", result); // Preserved
        Assert.Contains("#password", result); // Preserved
        Assert.Contains("#submit", result);   // Preserved
        Assert.Contains("#confirm", result);  // Preserved
        
        // Verify successful healing was logged
        var successfulHealing = await context.HealingHistories
            .Where(h => h.TestCaseId == 1 && h.WasSuccessful)
            .FirstOrDefaultAsync();
        Assert.NotNull(successfulHealing);
    }

    [Fact]
    public async Task HealTestScriptAsync_ExtractsPlaywrightGetByMethods()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var project = new Project { Id = 1, Name = "Test Project" };
        var testCase = new TestCase
        {
            Id = 1,
            ProjectId = 1,
            Title = "Test Case",
            Description = "Test",
            Steps = "Click button",
            ExpectedResults = "Success",
            AutomationScript = "await Page.ClickAsync(\"#old-btn\");",
            Project = project
        };
        var execution = new TestExecution
        {
            Id = 1,
            TestCaseId = 1,
            ProjectId = 1,
            Status = "Failed",
            ErrorMessage = "Element not found: #old-btn",
            TestCase = testCase,
            Project = project
        };

        context.Projects.Add(project);
        context.TestCases.Add(testCase);
        context.TestExecutions.Add(execution);
        await context.SaveChangesAsync();

        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        // AI response uses GetByText method
        var jsonContent = @"{""choices"": [{""message"": {""content"": ""await Page.GetByText(\""Submit\"").ClickAsync();""}}]}";
        var mockResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(jsonContent)
        };

        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(mockResponse);

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var validationService = CreateMockValidationService();
        var comparisonService = CreateMockComparisonService();
        var service = new AITestHealingService(context, httpClient, validationService, comparisonService);

        // Act
        var result = await service.HealTestScriptAsync(1, 1, "test-api-key", "gpt-4");

        // Assert - Should succeed as GetByText is a valid Playwright method
        Assert.Contains("GetByText", result);
        Assert.Contains("Submit", result);
    }

    [Fact]
    public async Task HealTestScriptAsync_DetectsExtendedElementTypes()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var project = new Project { Id = 1, Name = "Test Project" };
        var testCase = new TestCase
        {
            Id = 1,
            ProjectId = 1,
            Title = "Test Case",
            Description = "Test",
            Steps = "Select dropdown",
            ExpectedResults = "Success",
            AutomationScript = "await Page.SelectOptionAsync(\"#country\", \"US\");",
            Project = project
        };
        var execution = new TestExecution
        {
            Id = 1,
            TestCaseId = 1,
            ProjectId = 1,
            Status = "Failed",
            ErrorMessage = "Select dropdown element not found: #country",
            TestCase = testCase,
            Project = project
        };

        context.Projects.Add(project);
        context.TestCases.Add(testCase);
        context.TestExecutions.Add(execution);
        await context.SaveChangesAsync();

        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        // AI response changes select to a button (wrong element type)
        var jsonContent = @"{""choices"": [{""message"": {""content"": ""await Page.GetByRole(AriaRole.Button, new() { Name = \""Country\"" }).ClickAsync();""}}]}";
        var mockResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(jsonContent)
        };

        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(mockResponse);

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        
        // Configure validation service to detect the mismatch (select vs button)
        var validationService = CreateMockValidationService(isValid: true, hasMismatch: true);
        var comparisonService = CreateMockComparisonService();
        var service = new AITestHealingService(context, httpClient, validationService, comparisonService);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.HealTestScriptAsync(1, 1, "test-api-key", "gpt-4"));

        Assert.Contains("incompatible element type", exception.Message);
    }

    [Fact]
    public async Task HealTestScriptAsync_AllowsSmallChanges_EvenWhenMultipleLocatorsPresent()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var project = new Project { Id = 1, Name = "Test Project" };
        var testCase = new TestCase
        {
            Id = 1,
            ProjectId = 1,
            Title = "Test Case",
            Description = "Test",
            Steps = "Multiple steps",
            ExpectedResults = "Success",
            // Original script has just 2 locators
            AutomationScript = @"
                await Page.ClickAsync(""#login"");
                await Page.ClickAsync(""#submit"");
            ",
            Project = project
        };
        var execution = new TestExecution
        {
            Id = 1,
            TestCaseId = 1,
            ProjectId = 1,
            Status = "Failed",
            ErrorMessage = "Element not found: #login",
            TestCase = testCase,
            Project = project
        };

        context.Projects.Add(project);
        context.TestCases.Add(testCase);
        context.TestExecutions.Add(execution);
        await context.SaveChangesAsync();

        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        // AI response changes both locators (but only 2 total, should be allowed)
        var jsonContent = @"{""choices"": [{""message"": {""content"": ""await Page.GetByRole(AriaRole.Button, new() { Name = \""Login\"" }).ClickAsync();\nawait Page.GetByRole(AriaRole.Button, new() { Name = \""Submit\"" }).ClickAsync();""}}]}";
        var mockResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(jsonContent)
        };

        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(mockResponse);

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var validationService = CreateMockValidationService();
        var comparisonService = CreateMockComparisonService();
        var service = new AITestHealingService(context, httpClient, validationService, comparisonService);

        // Act
        var result = await service.HealTestScriptAsync(1, 1, "test-api-key", "gpt-4");

        // Assert - Should succeed because only 2 locators changed (threshold allows this)
        Assert.Contains("GetByRole", result);
        
        // Verify successful healing was logged
        var successfulHealing = await context.HealingHistories
            .Where(h => h.TestCaseId == 1 && h.WasSuccessful)
            .FirstOrDefaultAsync();
        Assert.NotNull(successfulHealing);
    }

    [Fact]
    public async Task HealTestScriptAsync_ProtectsWorkingLocators_InPresenceOfOneFailure()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var project = new Project { Id = 1, Name = "Test Project" };
        var testCase = new TestCase
        {
            Id = 1,
            ProjectId = 1,
            Title = "Test Case",
            Description = "Test",
            Steps = "Multiple actions",
            ExpectedResults = "Success",
            AutomationScript = @"
                await Page.ClickAsync(""#step1"");
                await Page.ClickAsync(""#step2"");
                await Page.ClickAsync(""#step3-broken"");
                await Page.ClickAsync(""#step4"");
            ",
            Project = project
        };
        var execution = new TestExecution
        {
            Id = 1,
            TestCaseId = 1,
            ProjectId = 1,
            Status = "Failed",
            ErrorMessage = "Element not found: #step3-broken",
            TestCase = testCase,
            Project = project
        };

        context.Projects.Add(project);
        context.TestCases.Add(testCase);
        context.TestExecutions.Add(execution);
        await context.SaveChangesAsync();

        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        // AI correctly fixes only the broken locator
        var jsonContent = @"{""choices"": [{""message"": {""content"": ""await Page.ClickAsync(\""#step1\"");\nawait Page.ClickAsync(\""#step2\"");\nawait Page.GetByTestId(\""step3\"").ClickAsync();\nawait Page.ClickAsync(\""#step4\"");""}}]}";
        var mockResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(jsonContent)
        };

        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(mockResponse);

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var validationService = CreateMockValidationService();
        var comparisonService = CreateMockComparisonService();
        var service = new AITestHealingService(context, httpClient, validationService, comparisonService);

        // Act
        var result = await service.HealTestScriptAsync(1, 1, "test-api-key", "gpt-4");

        // Assert - Should succeed with only the broken locator changed
        Assert.Contains("#step1", result);   // Preserved
        Assert.Contains("#step2", result);   // Preserved
        Assert.Contains("GetByTestId", result); // Fixed
        Assert.Contains("#step4", result);   // Preserved
        Assert.DoesNotContain("#step3-broken", result); // Replaced
        
        // Verify successful healing was logged
        var successfulHealing = await context.HealingHistories
            .Where(h => h.TestCaseId == 1 && h.WasSuccessful)
            .FirstOrDefaultAsync();
        Assert.NotNull(successfulHealing);
    }
}
