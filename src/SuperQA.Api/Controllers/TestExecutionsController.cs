using Microsoft.AspNetCore.Mvc;
using SuperQA.Core.Interfaces;
using SuperQA.Shared.DTOs;

namespace SuperQA.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestExecutionsController : ControllerBase
{
    private readonly ITestExecutionService _testExecutionService;
    private readonly IBackgroundTestRunner _backgroundTestRunner;

    public TestExecutionsController(
        ITestExecutionService testExecutionService,
        IBackgroundTestRunner backgroundTestRunner)
    {
        _testExecutionService = testExecutionService;
        _backgroundTestRunner = backgroundTestRunner;
    }

    [HttpPost("execute")]
    public async Task<ActionResult<int>> ExecuteTest([FromBody] ExecuteTestRequest request)
    {
        try
        {
            var executionId = await _testExecutionService.ExecuteTestAsync(
                request.TestCaseId, 
                request.BaseUrl);
            
            return Ok(executionId);
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error executing test: {ex.Message}");
        }
    }

    [HttpGet("project/{projectId}")]
    public async Task<ActionResult<IEnumerable<object>>> GetProjectExecutions(int projectId)
    {
        var executions = await _testExecutionService.GetTestExecutionsAsync(projectId);
        return Ok(executions);
    }

    [HttpGet("{executionId}")]
    public async Task<ActionResult<object>> GetExecution(int executionId)
    {
        var execution = await _testExecutionService.GetTestExecutionAsync(executionId);
        
        if (execution == null)
        {
            return NotFound();
        }

        return Ok(execution);
    }

    [HttpPost("project/{projectId}/run-all")]
    public async Task<ActionResult> RunAllTests(int projectId)
    {
        await _backgroundTestRunner.RunTestsInBackgroundAsync(projectId);
        return Accepted(new { message = "Test execution started in background" });
    }

    [HttpGet("project/{projectId}/status")]
    public async Task<ActionResult<string>> GetTestRunStatus(int projectId)
    {
        var status = await _backgroundTestRunner.GetTestRunStatusAsync(projectId);
        return Ok(new { status });
    }
}
