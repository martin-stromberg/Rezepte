namespace Rezepte.Web.Configuration;

public sealed class GoogleCredentialsOptions
{
    public string? ServiceAccountFilePath { get; set; } = "";
    public string? GeminiApiKey { get; set; } = "";
}
