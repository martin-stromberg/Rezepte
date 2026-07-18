namespace Rezepte.Web.Services.Import.Plugins;

public sealed class PluginPackageInstallException(string status, string message, Exception innerException) : Exception(message, innerException)
{
    public string Status { get; } = status;
}
