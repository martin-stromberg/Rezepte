namespace Rezepte.Web.Services;

/// <summary>
/// Represents the base service class.
/// </summary>
public class BaseService
{
    /// <summary>
    /// matchs the user.
    /// </summary>
    /// <param name="entity">The entity parameter.</param>
    /// <returns>The result.</returns>
    protected static User MatchUser(Entities.User entity)
    {
        return new User(entity.Id, entity.Username, entity.Email, entity.PasswordHash, entity.IsAdmin, entity.CreatedAt);
    }
}
