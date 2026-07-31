namespace Rezepte.Web.Configuration;

public sealed class ApplicationUpdateOptions
{
    public bool Enabled { get; set; }
    public bool EnableAutomaticDownload { get; set; } = true;
    public bool EnableAutomaticInstallation { get; set; }
    public string DownloadPath { get; set; } = "updates";
    public bool HostedServicesEnabled { get; set; } = true;
    public bool StopHostAfterScriptStart { get; set; }
    public int HealthTimeoutSeconds { get; set; } = 120;
    public string UpdateUnitName { get; set; } = "RezepteWebAutoUpdate";
    public string? RepositoryOwner { get; set; }
    public string? RepositoryName { get; set; }
    public string? ManifestAssetName { get; set; }
    public string? LocalSourceDirectory { get; set; }
}
