namespace Rezepte.Web.Services;

public sealed class GoogleServiceAccountProvider : IGoogleServiceAccountProvider
{
    private const string FileName = "google.application-credentials.json";

    public string GetFilePath()
    {
        var jsonPath = Path.Combine(AppContext.BaseDirectory ?? Environment.CurrentDirectory, FileName);

        if (File.Exists(jsonPath))
        {
            // Nur setzen, wenn vorhanden
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", jsonPath);
        }

        return jsonPath;
    }

    public bool Exists()
    {
        var path = GetFilePath();
        return File.Exists(path);
    }
}