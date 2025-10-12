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
        _api = api;
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
        catch { Fail("Benutzer konnten nicht geladen werden."); }
        finally { IsLoading = false; Notify(); }
    }

    public async Task ReloadAsync() => await LoadAsync();

    public async Task CreateAsync(CancellationToken ct = default)
    {
        Reset(); IsBusy = true; Notify();
        try
        {
            var payload = new { username = NewUser.Username!.Trim(), email = string.IsNullOrWhiteSpace(NewUser.Email) ? null : NewUser.Email!.Trim(), password = NewUser.Password, isAdmin = NewUser.IsAdmin };
            var res = await _api.Http.PostAsJsonAsync("api/admin/users", payload, ct);
            if (!res.IsSuccessStatusCode)
            {
                Fail("Anlegen fehlgeschlagen."); return;
            }
            var created = await res.Content.ReadFromJsonAsync<UserRow>(cancellationToken: ct);
            if (created is not null)
            {
                Users.Add(created);
                // sortieren optional
                Users = Users.OrderBy(u => u.Username, StringComparer.OrdinalIgnoreCase).ToList();
            }
            // Formular zurücksetzen
            NewUser.Username = string.Empty;
            NewUser.Email = string.Empty;
            NewUser.Password = string.Empty;
            NewUser.IsAdmin = false;
            Ok("Benutzer angelegt.");
        }
        catch { Fail("Anlegen fehlgeschlagen."); }
        finally { IsBusy = false; Notify(); }
    }

    public async Task SaveAsync(UserRow user, CancellationToken ct = default)
    {
        Reset(); IsBusy = true; Notify();
        try
        {
            var res = await _api.Http.PutAsJsonAsync($"api/admin/users/{user.Id}", user, ct);
            if (!res.IsSuccessStatusCode) { Fail("Speichern fehlgeschlagen."); return; }
            Ok("Gespeichert.");
        }
        catch { Fail("Speichern fehlgeschlagen."); }
        finally { IsBusy = false; Notify(); }
    }

    public async Task DeleteAsync(UserRow user, CancellationToken ct = default)
    {
        Reset(); IsBusy = true; Notify();
        try
        {
            var res = await _api.Http.DeleteAsync($"api/admin/users/{user.Id}", ct);
            if (!res.IsSuccessStatusCode) { Fail("Löschen fehlgeschlagen."); return; }
            Users.Remove(user); Ok("Gelöscht.");
        }
        catch { Fail("Löschen fehlgeschlagen."); }
        finally { IsBusy = false; Notify(); }
    }

    private void Ok(string? message = null) { IsError = false; Message = message; }
    private void Fail(string message) { IsError = true; Message = message; }
    private void Reset() { IsError = false; Message = null; }
    private void Notify() => OnChange?.Invoke();

    public class UserRow
    {
        public string Id { get; set; } = string.Empty;
        [Required, MinLength(3)] public string Username { get; set; } = string.Empty;
        public string? Email { get; set; }
        public bool IsAdmin { get; set; }
    }

    public class NewUserModel
    {
        [Required, MinLength(3)]
        public string? Username { get; set; }
        [EmailAddress]
        public string? Email { get; set; }
        [Required, MinLength(6)]
        public string Password { get; set; } = string.Empty;
        public bool IsAdmin { get; set; }
    }
}
