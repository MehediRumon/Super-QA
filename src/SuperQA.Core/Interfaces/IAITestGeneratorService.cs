using SuperQA.Core.Entities;

namespace SuperQA.Core.Interfaces;

public interface IAITestGeneratorService
{
    Task<IEnumerable<TestCase>> GenerateTestCasesAsync(Requirement requirement);
    Task<string> GenerateAutomationScriptAsync(TestCase testCase, string framework, string? pageStructure = null);
    string? ExtractUrlFromTestCase(TestCase testCase);
}
