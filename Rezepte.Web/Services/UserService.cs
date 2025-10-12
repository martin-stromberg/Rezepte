using Microsoft.EntityFrameworkCore;
using Rezepte.Web.Contracts;
using Rezepte.Web.Data;
using Rezepte.Web.Security;

namespace Rezepte.Web.Services;
public record User(string Id, string Username, string Email, string PasswordHash, bool IsAdmin);

public interface IUserService
{
    Task<(bool ok, string? error, User? user)> RegisterAsync(string username, string password, CancellationToken ct);
    Task<User?> LoginAsync(string username, string password, CancellationToken ct);
    Task<User?> FindByUsernameAsync(string username, CancellationToken ct);
    Task<bool> HasAnyUsersAsync(CancellationToken ct);
}
public class UserService(RezepteDbContext db) : IUserService
{
    private readonly RezepteDbContext _db = db;

    public async Task<(bool ok, string? error, User? user)> RegisterAsync(string username, string password, CancellationToken ct)
    {
        if (await _db.Users.AnyAsync(u => u.Username == username, ct))
            return (false, "Username already taken.", null);

        var isFirst = !await _db.Users.AnyAsync(ct);
        var entity = new Entities.User
        {
            Username = username,
            PasswordHash = PasswordHasher.Hash(password),
            IsAdmin = isFirst
        };
        _db.Users.Add(entity);
        await _db.SaveChangesAsync(ct);

        var user = new User(entity.Id, entity.Username, entity.Email, entity.PasswordHash, entity.IsAdmin);
        return (true, null, user);
    }

    public async Task<User?> LoginAsync(string username, string password, CancellationToken ct)
    {
        var entity = await _db.Users.FirstOrDefaultAsync(u => u.Username == username, ct);
        if (entity is null) return null;
        return PasswordHasher.Verify(password, entity.PasswordHash)
            ? new User(entity.Id, entity.Username, entity.Email, entity.PasswordHash, entity.IsAdmin)
            : null;
    }

    public async Task<User?> FindByUsernameAsync(string username, CancellationToken ct)
    {
        var entity = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == username, ct);
        return entity is null ? null : new User(entity.Id, entity.Username, entity.Email, entity.PasswordHash, entity.IsAdmin);
    }

    public async Task<bool> HasAnyUsersAsync(CancellationToken ct)
    {
        return await _db.Users.AsNoTracking().AnyAsync(ct);
    }
}
