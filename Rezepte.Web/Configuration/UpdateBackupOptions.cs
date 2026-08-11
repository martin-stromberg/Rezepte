namespace Rezepte.Web.Configuration;

public sealed class UpdateBackupOptions
{
    public string Directory { get; set; } = "update-backups";
    public int RetentionCount { get; set; } = 5;
    public bool IncludeImages { get; set; } = true;
    public bool IncludePdf { get; set; }
    public string SystemInitiatorUserId { get; set; } = "system-update-backup";
}
