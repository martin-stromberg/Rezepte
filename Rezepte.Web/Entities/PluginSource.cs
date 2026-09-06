namespace Rezepte.Web.Entities;

/// <summary>
/// Represents the plugin source class.
/// </summary>
public class PluginSource
{
    /// <summary>
    /// guids the value.
    /// </summary>
    /// <returns>The result.</returns>
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public string RepositoryUrl { get; set; } = string.Empty;
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public string Owner { get; set; } = string.Empty;
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public string Repository { get; set; } = string.Empty;
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public bool IsPrivate { get; set; }
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public bool Enabled { get; set; } = true;
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public bool TrustConfirmed { get; set; }
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public string? SecretName { get; set; }
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public string? LastSuccessfulReleaseTag { get; set; }
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public string? LastError { get; set; }
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public DateTime? LastCheckedAt { get; set; }
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public DateTime? LastErrorAt { get; set; }
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public List<PluginSourceRelease> Releases { get; set; } = [];
}
