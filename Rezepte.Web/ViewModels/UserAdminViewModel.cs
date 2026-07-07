using System.Net.Http.Json;
using System.ComponentModel.DataAnnotations;

namespace Rezepte.Web.ViewModels;

public class UserAdminViewModel
{
    private readonly ApiClient _api;
    public event Action? OnChange;

    public bool IsLoading { get; private set; } = true;
    public bool IsBusy { get; private set; }
    public bool IsError { get; private set; }
    public string? Message { get; private set; }

    public List<UserRow> Users { get; private set; } = [];
    public IEnumerable<UserRow> Filtered => string.IsNullOrWhiteSpace(Query)
        ? Users
        : Users.Where(u => (u.Username?.Contains(Query, StringComparison.OrdinalIgnoreCase) ?? false)
                        || (u.Email?.Contains(Query, StringComparison.OrdinalIgnoreCase) ?? false));

    public string Query { get; set; } = string.Empty;

    public NewUserModel NewUser { get; } = new();

    public UserAdminViewModel(ApiClient api)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
    }

    public async Task LoadAsync(CancellationToken ct = default)
    {
        Reset(); IsLoading = true; Notify();
        try
        {
            var items = await _api.Http.GetFromJsonAsync<List<UserRow>>("api/admin/users", ct) ?? [];
            Users = items;
            Ok();
        }
        catch { Fail("Users could not be loaded."); }
        finally { IsLoading = false; Notify(); }
    }

    public async Task ReloadAsync() => await LoadAsync();

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
        catch { Fail("Create failed."); }
        finally { IsBusy = false; Notify(); }
    }

    public async Task SaveAsync(UserRow user, CancellationToken ct = default)
    {
        Reset(); IsBusy = true; Notify();
        try
        {
            var res = await _api.Http.PutAsJsonAsync($"api/admin/users/{user.Id}", user, ct);
            if (!res.IsSuccessStatusCode) { Fail(await ReadErrorAsync(res) ?? "Save failed."); return; }
            Ok("Saved.");
        }
        catch { Fail("Save failed."); }
        finally { IsBusy = false; Notify(); }
    }

    public async Task DeleteAsync(UserRow user, CancellationToken ct = default)
    {
        Reset(); IsBusy = true; Notify();
        try
        {
            var res = await _api.Http.DeleteAsync($"api/admin/users/{user.Id}", ct);
            if (!res.IsSuccessStatusCode) { Fail("Delete failed."); return; }
            Users.Remove(user); Ok("Deleted.");
        }
        catch { Fail("Delete failed."); }
        finally { IsBusy = false; Notify(); }
    }

    private static async Task<string?> ReadErrorAsync(HttpResponseMessage res)
    {
        try
        {
            var obj = await res.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
            if (obj is not null && obj.TryGetValue("message", out var value) && value is string message)
                return message;
        }
        catch { }

        return null;
    }

    private void Ok(string? message = null) { IsError = false; Message = message; }
    private void Fail(string message) { IsError = true; Message = message; }
    private void Reset() { IsError = false; Message = null; }
    private void Notify() => OnChange?.Invoke();

    public class UserRow
    {
        public string Id { get; set; } = string.Empty;
        [Required] public string Username { get; set; } = string.Empty;
        public string? Email { get; set; }
        public bool IsAdmin { get; set; }
    }

    public class NewUserModel
    {
        [Required]
        public string? Username { get; set; }
        [EmailAddress]
        public string? Email { get; set; }
        [Required, MinLength(6)]
        public string Password { get; set; } = string.Empty;
        public bool IsAdmin { get; set; }
    }
}
