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
    private readonly IAITestHealingService _healingService;

    public TestExecutionsController(
        ITestExecutionService testExecutionService,
        IBackgroundTestRunner backgroundTestRunner,
        IAITestHealingService healingService)
    {
        _testExecutionService = testExecutionService;
        _backgroundTestRunner = backgroundTestRunner;
        _healingService = healingService;
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

    [HttpPost("heal")]
    public async Task<ActionResult<HealTestResponse>> HealTest([FromBody] HealTestRequest request)
    {
        try
        {
            var healedScript = await _healingService.HealTestScriptAsync(
                request.TestCaseId,
                request.ExecutionId,
                request.ApiKey,
                request.Model);

            return Ok(new HealTestResponse
            {
                HealedScript = healedScript,
                Message = "Test script healed successfully. Review and update your test case with the improved script."
            });
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(502, $"OpenAI API error: {ex.Message}");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error healing test: {ex.Message}");
        }
    }

    [HttpPost("apply-healed-script")]
    public async Task<ActionResult<ApplyHealedScriptResponse>> ApplyHealedScript([FromBody] ApplyHealedScriptRequest request)
    {
        try
        {
            var testCase = await _testExecutionService.GetTestCaseAsync(request.TestCaseId);
            if (testCase == null)
            {
                return NotFound(new ApplyHealedScriptResponse
                {
                    Success = false,
                    Message = $"Test case with ID {request.TestCaseId} not found."
                });
            }

            await _testExecutionService.UpdateTestCaseAutomationScriptAsync(request.TestCaseId, request.HealedScript);

            return Ok(new ApplyHealedScriptResponse
            {
                Success = true,
                Message = "Healed script applied successfully to the test case."
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApplyHealedScriptResponse
            {
                Success = false,
                Message = $"Error applying healed script: {ex.Message}"
            });
        }
    }
}
