namespace SuperQA.Shared.DTOs;

public class UserSettingsDto
{
    public int Id { get; set; }
    public string OpenAIApiKey { get; set; } = string.Empty;
    public string SelectedModel { get; set; } = "gpt-4o-mini";
    public bool PlaywrightHeadless { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class SaveUserSettingsRequest
{
    public string OpenAIApiKey { get; set; } = string.Empty;
    public string SelectedModel { get; set; } = "gpt-4o-mini";
    public bool PlaywrightHeadless { get; set; } = true;
}
