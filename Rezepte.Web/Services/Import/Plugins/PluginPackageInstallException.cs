namespace Rezepte.Web.Services.Import.Plugins;

/// <summary>
/// plugins the package install exception.
/// </summary>
/// <param name="status">The status parameter.</param>
/// <param name="message">The message parameter.</param>
/// <param name="innerException">The inner exception parameter.</param>
/// <returns>The result.</returns>
public sealed class PluginPackageInstallException(string status, string message, Exception innerException) : Exception(message, innerException)
{
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public string Status { get; } = status;
}
