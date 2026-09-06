namespace Rezepte.Web.Entities;

/// <summary>
/// Defines the ai request log type values.
/// </summary>
public enum AiRequestLogType
{
    /// <summary>
    /// Represents the ai request log class.
    /// </summary>
    Request = 0,
    /// <summary>
    /// Represents the ai request log class.
    /// </summary>
    Success = 1
}

/// <summary>
/// Represents the ai request log class.
/// </summary>
public class AiRequestLog
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
    public string Service { get; set; } = string.Empty; // "Vision" / "Gemini"
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public AiRequestLogType Type { get; set; } = AiRequestLogType.Request;
}
