namespace Rezepte.Web.Entities;

/// <summary>
/// Represents the user setting class.
/// </summary>
public class UserSetting
{
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public string UserId { get; set; } = string.Empty; // PK = UserId
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public bool AiEnabled { get; set; } = true;
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public bool GoogleVisionEnabled { get; set; } = true;
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public bool GeminiEnabled { get; set; } = true;
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public bool RequireAiConfirmation { get; set; } = false;
}
