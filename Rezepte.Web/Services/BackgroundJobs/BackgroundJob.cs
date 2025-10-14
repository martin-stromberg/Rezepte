using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rezepte.Web.Services.BackgroundJobs;

public enum BackgroundJobStatus
{
    Pending = 0,
    Running = 1,
    Succeeded = 2,
    Failed = 3,
    Cancelled = 4
}

[Table("BackgroundJobs")]
public class BackgroundJob
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public string JobType { get; set; } = default!;

    public string? InitiatorUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public BackgroundJobStatus Status { get; set; } = BackgroundJobStatus.Pending;

    /// <summary>
    /// JSON payload for job arguments. Convention: handlers parse this.
    /// </summary>
    public string? PayloadJson { get; set; }

    /// <summary>
    /// Optional numeric progress 0..100
    /// </summary>
    public int Progress { get; set; }

    public string? ResultMessage { get; set; }

    public string? Error { get; set; }
}