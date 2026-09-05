namespace Rezepte.Web.Configuration;

/// <summary>
/// Represents the google credentials options class.
/// </summary>
public sealed class GoogleCredentialsOptions
{
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public string? ServiceAccountFilePath { get; set; } = "";
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public string? GeminiApiKey { get; set; } = "";
}
