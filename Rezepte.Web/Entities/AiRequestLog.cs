namespace Rezepte.Web.Entities;

public enum AiRequestLogType
{
    Request = 0,
    Success = 1
}

public class AiRequestLog
{
    public string Id { get; set; } = Guid.NewGuid().ToString("n");
    public string UserId { get; set; } = string.Empty;
    public string Service { get; set; } = string.Empty; // "Vision" / "Gemini"
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public AiRequestLogType Type { get; set; } = AiRequestLogType.Request;
}