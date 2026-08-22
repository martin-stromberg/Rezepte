namespace Rezepte.Web.Entities;

public class UserExportFile
{
    public string Id { get; set; } = Guid.NewGuid().ToString("n");

    public string UserId { get; set; } = string.Empty;

    public bool IsAdminExport { get; set; }

    public string FileName { get; set; } = string.Empty;

    public long Size { get; set; }

    public Guid? JobId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
