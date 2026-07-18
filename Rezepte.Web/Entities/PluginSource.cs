namespace Rezepte.Web.Entities;

public class PluginSource
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string RepositoryUrl { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public string Repository { get; set; } = string.Empty;
    public bool IsPrivate { get; set; }
    public bool Enabled { get; set; } = true;
    public bool TrustConfirmed { get; set; }
    public string? SecretName { get; set; }
    public string? LastSuccessfulReleaseTag { get; set; }
    public string? LastError { get; set; }
    public DateTime? LastCheckedAt { get; set; }
    public DateTime? LastErrorAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public List<PluginSourceRelease> Releases { get; set; } = [];
}
