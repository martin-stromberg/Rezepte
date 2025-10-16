namespace Rezepte.Web.Entities;
public class AiRequestLog
{
    public string Id { get; set; } = Guid.NewGuid().ToString("n");
    public string UserId { get; set; } = string.Empty;
    public string Service { get; set; } = string.Empty; // "Vision" / "Gemini"
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}