namespace SuperQA.Shared.DTOs;

public class BrowserExtensionStep
{
    public string Action { get; set; } = string.Empty;
    public string Locator { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class BrowserExtensionDataRequest
{
    public string ApplicationUrl { get; set; } = string.Empty;
    public List<BrowserExtensionStep> Steps { get; set; } = new();
}

public class GenerateFromExtensionRequest
{
    public string ApplicationUrl { get; set; } = string.Empty;
    public List<BrowserExtensionStep> Steps { get; set; } = new();
    public string? OpenAIApiKey { get; set; }
    public string? Model { get; set; }
}
