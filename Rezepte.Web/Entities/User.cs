namespace Rezepte.Web.Entities;

public class User
{
    public string Id { get; set; } = Guid.NewGuid().ToString("n");
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsAdmin { get; set; } // Erstregistrierter Benutzer wird Admin
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
