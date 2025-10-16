namespace Rezepte.Web.Services;

public class BaseService
{
    protected static User MatchUser(Entities.User entity)
    {
        return new User(entity.Id, entity.Username, entity.Email, entity.PasswordHash, entity.IsAdmin, entity.CreatedAt);
    }
}
