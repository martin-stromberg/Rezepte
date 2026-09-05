using System.Reflection;
using System.Text.Json;

namespace Rezepte.Web;

/// <summary>
/// Represents the application version class.
/// </summary>
public static class ApplicationVersion
{
    private static readonly string Value = Resolve();

    /// <summary>
    /// Represents the public class.
    /// </summary>
    public static string Current => Value;

    private static string Resolve()
    {
        var metadataVersion = GetReleaseMetadataVersion();
        if (!string.IsNullOrWhiteSpace(metadataVersion))
        {
            return metadataVersion;
        }

        var assembly = Assembly.GetEntryAssembly() ?? typeof(ApplicationVersion).Assembly;
        var version = assembly.GetName().Version;

        var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(informational))
        {
            var plusIndex = informational.IndexOf('+');
            var display = plusIndex >= 0 ? informational[..plusIndex] : informational;

            if (display.Contains('-') ||
                (version is not null && !string.Equals(display, version.ToString(), StringComparison.Ordinal)))
            {
                return display;
            }
        }

        return version?.ToString(3) ?? "unknown";
    }

    private static string? GetReleaseMetadataVersion()
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "release-metadata.json");
            if (!File.Exists(path))
            {
                return null;
            }

            using var stream = File.OpenRead(path);
            using var document = JsonDocument.Parse(stream);
            if (document.RootElement.TryGetProperty("version", out var versionElement))
            {
                return versionElement.GetString();
            }
        }
        catch
        {
            // Ignore invalid or missing metadata and fall back to assembly version.
        }

        return null;
    }
}
