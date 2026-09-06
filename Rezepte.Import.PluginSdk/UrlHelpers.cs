namespace Rezepte.Import.PluginSdk;

/// <summary>
/// Helpers for validating and normalizing HTTP URLs.
/// </summary>
public static class UrlHelpers
{
    /// <summary>
    /// Tries to create an absolute HTTP or HTTPS URI from the provided value.
    /// </summary>
    /// <param name="value">String value to parse.</param>
    /// <param name="uri">The parsed URI when the method returns <c>true</c>.</param>
    /// <returns><c>true</c> when <paramref name="value"/> is a valid absolute HTTP or HTTPS URL; otherwise <c>false</c>.</returns>
    public static bool TryCreateHttpUri(string? value, out Uri uri)
    {
        uri = null!;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return Uri.TryCreate(value.Trim(), UriKind.Absolute, out uri!)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    /// <summary>
    /// Normalizes an absolute HTTP URL by lowercasing the scheme and host and removing default ports.
    /// </summary>
    /// <param name="value">URL to normalize.</param>
    /// <returns>The normalized URL string.</returns>
    /// <exception cref="FormatException">Thrown when <paramref name="value"/> is not a valid absolute HTTP or HTTPS URL.</exception>
    public static string NormalizeHttpUrl(string value)
    {
        if (!TryCreateHttpUri(value, out var uri))
            throw new FormatException($"Ungültige HTTP-URL: {value}");

        var builder = new UriBuilder(uri)
        {
            Scheme = uri.Scheme.ToLowerInvariant(),
            Host = uri.Host.ToLowerInvariant()
        };

        if ((builder.Scheme == Uri.UriSchemeHttp && builder.Port == 80)
            || (builder.Scheme == Uri.UriSchemeHttps && builder.Port == 443))
        {
            builder.Port = -1;
        }

        return builder.Uri.ToString();
    }
}
