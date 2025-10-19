namespace Rezepte.Web.Entities;

public class UserSetting
{
    public string UserId { get; set; } = string.Empty; // PK = UserId
    public bool AiEnabled { get; set; } = true;
    public bool GoogleVisionEnabled { get; set; } = true;
    public bool GeminiEnabled { get; set; } = true;
    public bool RequireAiConfirmation { get; set; } = false;
}