using Google.Apis.Services;
using Microsoft.EntityFrameworkCore;
using Rezepte.Web.Contracts;
using Rezepte.Web.Data;
using Rezepte.Web.Security;
using Rezepte.Web.Services.Validation;

namespace Rezepte.Web.Services;

/// <summary>
/// Lightweight user projection used by the service layer.
/// </summary>
/// <param name="Id">User identifier.</param>
/// <param name="Username">Username (unique).</param>
/// <param name="Email">E-mail address (optional).</param>
/// <param name="PasswordHash">Hashed password.</param>
/// <param name="IsAdmin">True if user has admin privileges.</param>
/// <param name="RegistrationTime">Timestamp of user registration.</param>
public record User(string Id, string Username, string Email, string PasswordHash, bool IsAdmin, DateTime RegistrationTime);

/// <summary>
/// User management service contract.
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Registers a new user account.
    /// </summary>
    /// <param name="username">Desired username.</param>
    /// <param name="password">Plain password to hash and store.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Tuple with success flag, optional error and created user projection.</returns>
    Task<(bool ok, string? error, User? user)> RegisterAsync(string username, string password, CancellationToken ct);

    /// <summary>
    /// Authenticates a user with credentials.
    /// </summary>
    /// <param name="username">Username.</param>
    /// <param name="password">Password.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>User projection when credentials are valid; otherwise null.</returns>
    Task<User?> LoginAsync(string username, string password, CancellationToken ct);

    /// <summary>
    /// Finds a user by username.
    /// </summary>
    /// <param name="username">Username to search.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>User projection or null.</returns>
    Task<User?> FindByUsernameAsync(string username, CancellationToken ct);

    /// <summary>
    /// Indicates whether any users exist in the system.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if at least one user exists.</returns>
    Task<bool> HasAnyUsersAsync(CancellationToken ct);

    /// <summary>
    /// Gets a user by identifier.
    /// </summary>
    /// <param name="id">User identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>User projection or null.</returns>
    Task<User?> GetByIdAsync(string id, CancellationToken ct);

    /// <summary>
    /// Updates the profile (username/email) of a user.
    /// </summary>
    /// <param name="id">User identifier.</param>
    /// <param name="username">New username.</param>
    /// <param name="email">New e-mail address (optional).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Tuple with success flag, optional error and updated user projection.</returns>
    Task<(bool ok, string? error, User? user)> UpdateProfileAsync(string id, string? username, string? email, CancellationToken ct);

    /// <summary>
    /// Changes the password of a user after verifying the current password.
    /// </summary>
    /// <param name="id">User identifier.</param>
    /// <param name="currentPassword">Current password to verify.</param>
    /// <param name="newPassword">New password to set.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Tuple with success flag and optional error.</returns>
    Task<(bool ok, string? error)> ChangePasswordAsync(string id, string currentPassword, string newPassword, CancellationToken ct);

    // Admin functions

    /// <summary>
    /// Returns all users for administration.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of users.</returns>
    Task<List<User>> GetAllAsync(CancellationToken ct);

    /// <summary>
    /// Updates an arbitrary user (admin endpoint).
    /// </summary>
    /// <param name="id">User identifier.</param>
    /// <param name="username">New username.</param>
    /// <param name="email">New e-mail (optional).</param>
    /// <param name="isAdmin">Admin flag.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Tuple with success flag and optional error.</returns>
    Task<(bool ok, string? error)> UpdateUserAsync(string id, string username, string? email, bool isAdmin, CancellationToken ct);

    /// <summary>
    /// Deletes a user by id.
    /// </summary>
    /// <param name="id">User identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Tuple with success flag and optional error.</returns>
    Task<(bool ok, string? error)> DeleteAsync(string id, CancellationToken ct);
}
public class UserService(RezepteDbContext db, IUsernameValidator usernameValidator) : BaseService, IUserService
{
    private readonly RezepteDbContext _db = db;
    private readonly IUsernameValidator _usernameValidator = usernameValidator;


    /// <inheritdoc />
    public async Task<(bool ok, string? error, User? user)> RegisterAsync(string username, string password, CancellationToken ct)
    {
        var normalizedUsername = username?.Trim() ?? string.Empty;
        var validation = _usernameValidator.Validate(normalizedUsername);
        if (!validation.IsValid)
            return (false, validation.ErrorMessage, null);

        if (await _db.Users.AnyAsync(u => u.Username == normalizedUsername, ct))
            return (false, "Benutzername ist bereits vergeben.", null);

        var isFirst = !await _db.Users.AnyAsync(ct);
        var entity = new Entities.User
        {
            Username = normalizedUsername,
            PasswordHash = PasswordHasher.Hash(password),
            IsAdmin = isFirst
        };
        _db.Users.Add(entity);
        await _db.SaveChangesAsync(ct);
        User user = MatchUser(entity);
        return (true, null, user);
    }

    

    /// <inheritdoc />
    public async Task<User?> LoginAsync(string username, string password, CancellationToken ct)
    {
        var entity = await _db.Users.FirstOrDefaultAsync(u => u.Username == username, ct);
        if (entity is null) return null;
        return PasswordHasher.Verify(password, entity.PasswordHash)
            ? MatchUser(entity)
            : null;
    }

    /// <inheritdoc />
    public async Task<User?> FindByUsernameAsync(string username, CancellationToken ct)
    {
        var entity = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == username, ct);
        return entity is null ? null : MatchUser(entity);
    }

    /// <inheritdoc />
    public async Task<bool> HasAnyUsersAsync(CancellationToken ct)
    {
        return await _db.Users.AsNoTracking().AnyAsync(ct);
    }

    /// <inheritdoc />
    public async Task<User?> GetByIdAsync(string id, CancellationToken ct)
    {
        var entity = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct);
        return entity is null ? null : MatchUser(entity);
    }

    /// <inheritdoc />
    public async Task<(bool ok, string? error, User? user)> UpdateProfileAsync(string id, string? username, string? email, CancellationToken ct)
    {
        var entity = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (entity is null) return (false, "User not found.", null);

        var normalizedUsername = username?.Trim() ?? string.Empty;
        var validation = _usernameValidator.Validate(normalizedUsername);
        if (!validation.IsValid)
            return (false, validation.ErrorMessage, null);

        if (!string.IsNullOrWhiteSpace(email))
        {
            if (!email.Contains('@') || email.Length > 256)
                return (false, "Die E-Mail ist ungültig.", null);
        }

        if (!string.Equals(entity.Username, normalizedUsername, StringComparison.Ordinal))
        {
            var exists = await _db.Users.AnyAsync(u => u.Username == normalizedUsername, ct);
            if (exists)
                return (false, "Benutzername ist bereits vergeben.", null);
        }

        entity.Username = normalizedUsername;
        entity.Email = email ?? string.Empty;

        await _db.SaveChangesAsync(ct);

        return (true, null, MatchUser(entity));
    }

    /// <inheritdoc />
    public async Task<(bool ok, string? error)> ChangePasswordAsync(string id, string currentPassword, string newPassword, CancellationToken ct)
    {
        var entity = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (entity is null) return (false, "User not found.");

        if (!PasswordHasher.Verify(currentPassword, entity.PasswordHash))
            return (false, "Aktuelles Passwort ist falsch.");

        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            return (false, "Das neue Passwort muss mindestens 6 Zeichen haben.");

        entity.PasswordHash = PasswordHasher.Hash(newPassword);
        await _db.SaveChangesAsync(ct);

        return (true, null);
    }

    /// <inheritdoc />
    public async Task<List<User>> GetAllAsync(CancellationToken ct)
    {
        var list = await _db.Users.AsNoTracking().OrderBy(u => u.Username).ToListAsync(ct);
        return list.Select(u => MatchUser(u)).ToList();
    }

    /// <inheritdoc />
    public async Task<(bool ok, string? error)> UpdateUserAsync(string id, string username, string? email, bool isAdmin, CancellationToken ct)
    {
        var entity = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (entity is null) return (false, "User not found.");

        var normalizedUsername = username?.Trim() ?? string.Empty;
        var validation = _usernameValidator.Validate(normalizedUsername);
        if (!validation.IsValid)
            return (false, validation.ErrorMessage);

        if (!string.Equals(entity.Username, normalizedUsername, StringComparison.Ordinal))
        {
            var exists = await _db.Users.AnyAsync(u => u.Username == normalizedUsername && u.Id != id, ct);
            if (exists) return (false, "Benutzername ist bereits vergeben.");
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            if (!email.Contains('@') || email.Length > 256)
                return (false, "Die E-Mail ist ungültig.");
        }

        entity.Username = normalizedUsername;
        entity.Email = email ?? string.Empty;
        entity.IsAdmin = isAdmin;
        await _db.SaveChangesAsync(ct);
        return (true, null);
    }

    /// <inheritdoc />
    public async Task<(bool ok, string? error)> DeleteAsync(string id, CancellationToken ct)
    {
        var entity = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (entity is null) return (false, "User not found.");
        _db.Users.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return (true, null);
    }
}
