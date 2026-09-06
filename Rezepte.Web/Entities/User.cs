namespace Rezepte.Web.Entities;

/// <summary>
/// Represents the user class.
/// </summary>
public class User
{
    /// <summary>
    /// guids the value.
    /// </summary>
    /// <returns>The result.</returns>
    public string Id { get; set; } = Guid.NewGuid().ToString("n");
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public string Username { get; set; } = string.Empty;
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public string Email { get; set; } = string.Empty;
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public bool IsAdmin { get; set; } // Erstregistrierter Benutzer wird Admin
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
