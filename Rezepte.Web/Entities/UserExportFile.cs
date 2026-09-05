namespace Rezepte.Web.Entities;

/// <summary>
/// Represents the user export file class.
/// </summary>
public class UserExportFile
{
    /// <summary>
    /// guids the value.
    /// </summary>
    /// <returns>The result.</returns>
    public string Id { get; set; } = Guid.NewGuid().ToString("n");

    /// <summary>
    /// Represents the public class.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Represents the public class.
    /// </summary>
    public bool IsAdminExport { get; set; }

    /// <summary>
    /// Represents the public class.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Represents the public class.
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// Represents the public class.
    /// </summary>
    public Guid? JobId { get; set; }

    /// <summary>
    /// Represents the public class.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
