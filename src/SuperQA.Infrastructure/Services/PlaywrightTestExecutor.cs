using System.Diagnostics;
using System.Text;
using SuperQA.Core.Interfaces;
using SuperQA.Shared.DTOs;

namespace SuperQA.Infrastructure.Services;

public class PlaywrightTestExecutor : IPlaywrightTestExecutor
{
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

                // Install Playwright browsers
                logs.Add("Installing Playwright browsers...");
                var installBrowsersResult = await RunCommandAsync("pwsh", "-c \"dotnet tool install --global Microsoft.Playwright.CLI 2>&1 ; playwright install chromium\"", projectPath);
                logs.Add(installBrowsersResult);

                // Write the test script
                var testFilePath = Path.Combine(projectPath, "GeneratedTest.cs");
                await File.WriteAllTextAsync(testFilePath, testScript);
                logs.Add($"Test script written to: {testFilePath}");

                // Build the project
                logs.Add("Building test project...");
                var buildResult = await RunCommandAsync("dotnet", "build", projectPath);
                logs.Add(buildResult);

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
