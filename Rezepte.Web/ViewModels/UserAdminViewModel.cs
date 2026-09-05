using System.Net.Http.Json;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Rezepte.Web.ViewModels;

/// <summary>
/// Represents the user admin view model class.
/// </summary>
public class UserAdminViewModel
{
    private readonly ApiClient _api;
    private readonly ILogger<UserAdminViewModel> _logger;
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public event Action? OnChange;

    /// <summary>
    /// Represents the public class.
    /// </summary>
    public bool IsLoading { get; private set; } = true;
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public bool IsBusy { get; private set; }
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public bool IsError { get; private set; }
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public string? Message { get; private set; }

    /// <summary>
    /// Represents the public class.
    /// </summary>
    public List<UserRow> Users { get; private set; } = [];
    /// <summary>
    /// strings the value.
    /// </summary>
    /// <param name="Query">The query parameter.</param>
    /// <returns>The result.</returns>
    public IEnumerable<UserRow> Filtered => string.IsNullOrWhiteSpace(Query)
        ? Users
        : Users.Where(u => (u.Username?.Contains(Query, StringComparison.OrdinalIgnoreCase) ?? false)
                        || (u.Email?.Contains(Query, StringComparison.OrdinalIgnoreCase) ?? false));

    /// <summary>
    /// Represents the public class.
    /// </summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// Represents the public class.
    /// </summary>
    /// <returns>The result.</returns>
    public NewUserModel NewUser { get; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="UserAdminViewModel"/> class.
    /// </summary>
    /// <param name="api">The api parameter.</param>
    /// <param name="logger">The logger parameter.</param>
    public UserAdminViewModel(ApiClient api, ILogger<UserAdminViewModel> logger)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Loads the async.
    /// </summary>
    /// <param name="ct">The ct parameter.</param>
    public async Task LoadAsync(CancellationToken ct = default)
    {
        Reset(); IsLoading = true; Notify();
        try
        {
            var items = await _api.Http.GetFromJsonAsync<List<UserRow>>("api/admin/users", ct) ?? [];
            Users = items;
            Ok();
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Loading the user list failed.");
            Fail("Users could not be loaded.");
        }
        finally { IsLoading = false; Notify(); }
    }

    /// <summary>
    /// reloads the async.
    /// </summary>
    public async Task ReloadAsync() => await LoadAsync();

    /// <summary>
    /// Creates the async.
    /// </summary>
    /// <param name="ct">The ct parameter.</param>
    public async Task CreateAsync(CancellationToken ct = default)
    {
        Reset();
        // Validate inputs (avoid null-forgiving)
        if (string.IsNullOrWhiteSpace(NewUser.Username)) { Fail("Username is required."); Notify(); return; }
        if (string.IsNullOrWhiteSpace(NewUser.Password)) { Fail("Password is required."); Notify(); return; }
        var username = NewUser.Username.Trim();
        var emailRaw = NewUser.Email;
        var email = string.IsNullOrWhiteSpace(emailRaw) ? null : emailRaw.Trim();
        var password = NewUser.Password;
        var isAdmin = NewUser.IsAdmin;

        IsBusy = true; Notify();
        try
        {
            var payload = new { username, email, password, isAdmin };
            var res = await _api.Http.PostAsJsonAsync("api/admin/users", payload, ct);
            if (!res.IsSuccessStatusCode)
            {
                Fail(await ReadErrorAsync(res) ?? "Create failed."); return;
            }
            var created = await res.Content.ReadFromJsonAsync<UserRow>(cancellationToken: ct);
            if (created is not null)
            {
                Users.Add(created);
                Users = Users.OrderBy(u => u.Username, StringComparer.OrdinalIgnoreCase).ToList();
            }
            // Reset form
            NewUser.Username = string.Empty;
            NewUser.Email = string.Empty;
            NewUser.Password = string.Empty;
            NewUser.IsAdmin = false;
            Ok("User created.");
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Creating user {Username} failed.", username);
            Fail("Create failed.");
        }
        finally { IsBusy = false; Notify(); }
    }

    /// <summary>
    /// Saves the async.
    /// </summary>
    /// <param name="user">The user parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    public async Task SaveAsync(UserRow user, CancellationToken ct = default)
    {
        Reset(); IsBusy = true; Notify();
        try
        {
            var res = await _api.Http.PutAsJsonAsync($"api/admin/users/{user.Id}", user, ct);
            if (!res.IsSuccessStatusCode) { Fail(await ReadErrorAsync(res) ?? "Save failed."); return; }
            Ok("Saved.");
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Saving user {UserId} failed.", user.Id);
            Fail("Save failed.");
        }
        finally { IsBusy = false; Notify(); }
    }

    /// <summary>
    /// Deletes the async.
    /// </summary>
    /// <param name="user">The user parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    public async Task DeleteAsync(UserRow user, CancellationToken ct = default)
    {
        Reset(); IsBusy = true; Notify();
        try
        {
            var res = await _api.Http.DeleteAsync($"api/admin/users/{user.Id}", ct);
            if (!res.IsSuccessStatusCode) { Fail("Delete failed."); return; }
            Users.Remove(user); Ok("Deleted.");
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Deleting user {UserId} failed.", user.Id);
            Fail("Delete failed.");
        }
        finally { IsBusy = false; Notify(); }
    }

    private async Task<string?> ReadErrorAsync(HttpResponseMessage res)
    {
        try
        {
            var obj = await res.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
            if (obj is not null && obj.TryGetValue("message", out var value) && value is string message)
                return message;
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException or HttpRequestException)
        {
            _logger.LogDebug(ex, "Error response with status {StatusCode} did not contain a readable message.", (int)res.StatusCode);
        }

        return null;
    }

    private void Ok(string? message = null) { IsError = false; Message = message; }
    private void Fail(string message) { IsError = true; Message = message; }
    private void Reset() { IsError = false; Message = null; }
    private void Notify() => OnChange?.Invoke();

    /// <summary>
    /// Represents the user row class.
    /// </summary>
    public class UserRow
    {
        /// <summary>
        /// Represents the public class.
        /// </summary>
        public string Id { get; set; } = string.Empty;
        /// <summary>
        /// Represents the public class.
        /// </summary>
        [Required] public string Username { get; set; } = string.Empty;
        /// <summary>
        /// Represents the public class.
        /// </summary>
        public string? Email { get; set; }
        /// <summary>
        /// Represents the public class.
        /// </summary>
        public bool IsAdmin { get; set; }
    }

    /// <summary>
    /// Represents the new user model class.
    /// </summary>
    public class NewUserModel
    {
        /// <summary>
        /// Represents the public class.
        /// </summary>
        [Required]
        public string? Username { get; set; }
        /// <summary>
        /// Represents the public class.
        /// </summary>
        [EmailAddress]
        public string? Email { get; set; }
        /// <summary>
        /// Represents the public class.
        /// </summary>
        [Required, MinLength(6)]
        public string Password { get; set; } = string.Empty;
        /// <summary>
        /// Represents the public class.
        /// </summary>
        public bool IsAdmin { get; set; }
    }
}
