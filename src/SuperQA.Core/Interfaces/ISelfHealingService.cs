namespace SuperQA.Core.Interfaces;

public interface ISelfHealingService
{
    Task<string> SuggestUpdatedLocatorAsync(string failedLocator, string htmlStructure);
    Task<bool> UpdateLocatorAsync(int testCaseId, string oldLocator, string newLocator);
}
