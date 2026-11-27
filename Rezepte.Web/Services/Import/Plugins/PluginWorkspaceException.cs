namespace Rezepte.Web.Services.Import.Plugins;

/// <summary>
/// Signals that no temporary work directory could be provided for the plugin update check.
/// The message carries the <see cref="PluginUpdateCheckErrors.WorkDirectoryUnavailable"/> constant
/// so existing <c>Exception.Message</c>-based error channels transport the well-known error text.
/// </summary>
/// <param name="innerException">The inner exception parameter.</param>
/// <returns>The result.</returns>
public sealed class PluginWorkspaceException(Exception? innerException = null)
    : Exception(PluginUpdateCheckErrors.WorkDirectoryUnavailable, innerException)
{
}
