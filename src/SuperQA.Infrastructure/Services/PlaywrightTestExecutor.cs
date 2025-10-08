using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Configuration;
using SuperQA.Core.Interfaces;
using SuperQA.Shared.DTOs;

namespace SuperQA.Infrastructure.Services;

public class PlaywrightTestExecutor : IPlaywrightTestExecutor
{
    private readonly IConfiguration _configuration;

    public PlaywrightTestExecutor(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<PlaywrightTestExecutionResponse> ExecuteTestScriptAsync(string testScript, string applicationUrl)
    {
        var response = new PlaywrightTestExecutionResponse();
        var logs = new List<string>();

        try
        {
            // Create a temporary directory for test execution
            var tempDir = Path.Combine(Path.GetTempPath(), $"PlaywrightTest_{Guid.NewGuid()}");
            Directory.CreateDirectory(tempDir);

            logs.Add($"Created temporary directory: {tempDir}");

            try
            {
                // Create a test project
                var projectName = "PlaywrightTestProject";
                var projectPath = Path.Combine(tempDir, projectName);
                
                logs.Add("Creating test project...");
                var createProjectResult = await RunCommandAsync("dotnet", $"new nunit -n {projectName}", tempDir);
                logs.Add(createProjectResult);

                // Add Playwright package
                logs.Add("Adding Playwright packages...");
                var addPackageResult = await RunCommandAsync("dotnet", "add package Microsoft.Playwright.NUnit", projectPath);
                logs.Add(addPackageResult);

                // Inject headless configuration into the test script
                var headless = _configuration.GetValue<bool>("Playwright:Headless", true);
                var modifiedTestScript = InjectHeadlessConfiguration(testScript, headless);

                // Write the test script
                var testFilePath = Path.Combine(projectPath, "GeneratedTest.cs");
                await File.WriteAllTextAsync(testFilePath, modifiedTestScript);
                logs.Add($"Test script written to: {testFilePath} (Headless: {headless})");

                // Build the project first (required before installing browsers)
                logs.Add("Building test project...");
                var buildResult = await RunCommandAsync("dotnet", "build", projectPath);
                logs.Add(buildResult);

                // Install Playwright browsers using the node CLI from the build output
                logs.Add("Installing Playwright browsers...");
                var installBrowsersResult = await InstallPlaywrightBrowsersAsync(projectPath);
                logs.Add(installBrowsersResult);

                // Run the tests
                logs.Add("Executing tests...");
                var testResult = await RunCommandAsync("dotnet", "test --logger:\"console;verbosity=detailed\"", projectPath);
                logs.Add(testResult);

                // Determine pass/fail based on test output
                if (testResult.Contains("Passed!") || testResult.Contains("Test Run Successful"))
                {
                    response.Success = true;
                    response.Status = "Pass";
                    response.Output = testResult;
                }
                else if (testResult.Contains("Failed!") || testResult.Contains("Test Run Failed"))
                {
                    response.Success = false;
                    response.Status = "Fail";
                    response.Output = testResult;
                    response.ErrorMessage = "One or more tests failed";
                }
                else
                {
                    response.Success = false;
                    response.Status = "Unknown";
                    response.Output = testResult;
                    response.ErrorMessage = "Could not determine test result";
                }
            }
            finally
            {
                // Clean up temporary directory
                try
                {
                    if (Directory.Exists(tempDir))
                    {
                        Directory.Delete(tempDir, true);
                        logs.Add("Cleaned up temporary directory");
                    }
                }
                catch (Exception ex)
                {
                    logs.Add($"Warning: Failed to clean up temporary directory: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.Status = "Error";
            response.ErrorMessage = ex.Message;
            logs.Add($"Error: {ex.Message}");
        }

        response.Logs = logs;
        return response;
    }

    private async Task<string> InstallPlaywrightBrowsersAsync(string projectPath)
    {
        try
        {
            // Find the build output directory
            var buildOutputPath = Path.Combine(projectPath, "bin", "Debug", "net9.0");
            var playwrightPath = Path.Combine(buildOutputPath, ".playwright");
            
            if (!Directory.Exists(playwrightPath))
            {
                return "Warning: Playwright package directory not found. Browsers may not be installed.";
            }

            // Find the node executable
            var nodeDir = Path.Combine(playwrightPath, "node");
            if (!Directory.Exists(nodeDir))
            {
                return "Warning: Node directory not found. Browsers may not be installed.";
            }

            var nodeDirs = Directory.GetDirectories(nodeDir);
            if (nodeDirs.Length == 0)
            {
                return "Warning: Node executable not found. Browsers may not be installed.";
            }

            var nodeExe = Path.Combine(nodeDirs[0], OperatingSystem.IsWindows() ? "node.exe" : "node");
            if (!File.Exists(nodeExe))
            {
                return "Warning: Node executable not found. Browsers may not be installed.";
            }

            // Find the Playwright CLI script
            var cliScript = Path.Combine(playwrightPath, "package", "cli.js");
            if (!File.Exists(cliScript))
            {
                return "Warning: Playwright CLI script not found. Browsers may not be installed.";
            }

            // Execute node cli.js install chromium
            var installResult = await RunCommandAsync(nodeExe, $"\"{cliScript}\" install chromium", projectPath);
            return installResult;
        }
        catch (Exception ex)
        {
            return $"Warning: Failed to install Playwright browsers: {ex.Message}";
        }
    }

    private string InjectHeadlessConfiguration(string testScript, bool headless)
    {
        // Check if the test inherits from PageTest (the standard pattern from AI-generated tests)
        if (testScript.Contains(": PageTest"))
        {
            // Add a SetUp method that configures browser options before each test
            // We need to insert this after the class declaration but before the first test method
            
            var setupMethod = $@"
    [SetUp]
    public async Task Setup()
    {{
        // Configure browser launch options to control headless mode
        BrowserNewContextOptions = new BrowserNewContextOptions();
        BrowserTypeLaunchOptions = new BrowserTypeLaunchOptions
        {{
            Headless = {headless.ToString().ToLower()}
        }};
    }}
";
            
            // Find the position to insert the setup method
            // Look for the test class opening brace and insert after it
            var classPattern = @"public class \w+ : PageTest\s*\{";
            var match = System.Text.RegularExpressions.Regex.Match(testScript, classPattern);
            
            if (match.Success)
            {
                var insertPosition = match.Index + match.Length;
                testScript = testScript.Insert(insertPosition, setupMethod);
            }
        }
        
        return testScript;
    }

    private async Task<string> RunCommandAsync(string command, string arguments, string workingDirectory)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = command,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = processStartInfo };
        var output = new StringBuilder();
        var error = new StringBuilder();

        process.OutputDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                output.AppendLine(e.Data);
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                error.AppendLine(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync();

        var result = output.ToString();
        if (error.Length > 0)
        {
            result += "\nErrors:\n" + error.ToString();
        }

        return result;
    }
}
