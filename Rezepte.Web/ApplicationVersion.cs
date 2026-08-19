using System.Reflection;

namespace Rezepte.Web;

public static class ApplicationVersion
{
    private static readonly string Value = Resolve();

    public static string Current => Value;

    private static string Resolve()
    {
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
}
