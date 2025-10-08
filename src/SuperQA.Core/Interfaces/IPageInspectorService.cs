namespace SuperQA.Core.Interfaces;

public interface IPageInspectorService
{
    Task<string> GetPageStructureAsync(string url);
}
