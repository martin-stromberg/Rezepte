using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rezepte.Web.Services.BackgroundJobs;

/// <summary>
/// Defines the background job status values.
/// </summary>
public enum BackgroundJobStatus
{
    /// <summary>
    /// Represents the pending class.
    /// </summary>
    Pending = 0,
    /// <summary>
    /// Represents the running class.
    /// </summary>
    Running = 1,
    /// <summary>
    /// Represents the succeeded class.
    /// </summary>
    Succeeded = 2,
    /// <summary>
    /// Represents the failed class.
    /// </summary>
    Failed = 3,
    /// <summary>
    /// Represents the cancelled class.
    /// </summary>
    Cancelled = 4
}

/// <summary>
/// Represents the background job class.
/// </summary>
[Table("BackgroundJobs")]
public class BackgroundJob
{
    /// <summary>
    /// guids the value.
    /// </summary>
    /// <returns>The result.</returns>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Represents the public class.
    /// </summary>
    [Required]
    public string JobType { get; set; } = default!;

    /// <summary>
    /// Represents the public class.
    /// </summary>
    public string? InitiatorUserId { get; set; }

    /// <summary>
    /// Represents the public class.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Represents the public class.
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// Represents the public class.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Represents the public class.
    /// </summary>
    public BackgroundJobStatus Status { get; set; } = BackgroundJobStatus.Pending;

    /// <summary>
    /// JSON payload for job arguments. Convention: handlers parse this.
    /// </summary>
    public string? PayloadJson { get; set; }

    /// <summary>
    /// Optional numeric progress 0..100
    /// </summary>
    public int Progress { get; set; }

    /// <summary>
    /// Represents the public class.
    /// </summary>
    public string? ResultMessage { get; set; }

    /// <summary>
    /// Represents the public class.
    /// </summary>
    public string? Error { get; set; }
}
