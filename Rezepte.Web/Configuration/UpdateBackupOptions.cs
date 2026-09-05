namespace Rezepte.Web.Configuration;

/// <summary>
/// Represents the update backup options class.
/// </summary>
public sealed class UpdateBackupOptions
{
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public string Directory { get; set; } = "update-backups";
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public int RetentionCount { get; set; } = 5;
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public bool IncludeImages { get; set; } = true;
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public bool IncludePdf { get; set; }
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public string SystemInitiatorUserId { get; set; } = "system-update-backup";
}
