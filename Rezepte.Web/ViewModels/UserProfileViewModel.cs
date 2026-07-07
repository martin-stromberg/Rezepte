using System.Net;
using System.Net.Http.Json;
using System.ComponentModel.DataAnnotations;
using Rezepte.Web.Contracts;

namespace Rezepte.Web.ViewModels;

public class UserProfileViewModel
{
    private readonly HttpClient _http;
    public event Action? OnChange;

    public bool IsLoading { get; private set; } = true;
    public bool IsBusy { get; private set; }
    public bool IsError { get; private set; }
    public string? Message { get; private set; }

    public ProfileModel Profile { get; private set; } = new();
    public PasswordModel Password { get; set; } = new();

    public UserProfileViewModel(ApiClient apiClient)
    {
        _http = apiClient.Http;
    }

    public async Task LoadAsync(CancellationToken ct = default)
    {
        ResetMessage();
        IsLoading = true; Notify();
        try
        {
            var res = await _http.GetAsync("api/users/me", ct);
            if (res.StatusCode == HttpStatusCode.Unauthorized)
            {
                Fail("Not signed in.");
                return;
            }
            if (!res.IsSuccessStatusCode)
            {
                var msg = await ReadErrorAsync(res) ?? "Profile could not be loaded.";
                Fail(msg);
                return;
            }

            var me = await res.Content.ReadFromJsonAsync<UserProfileDto>(cancellationToken: ct);
            if (me is null)
            {
                Fail("User not found.");
                return;
            }

            // Update the existing instance instead of replacing it so bindings remain intact.
            Profile.Id = me.Id;
            Profile.Username = me.Username;
            Profile.Email = me.Email ?? string.Empty;

            Ok(null);
        }
        catch
        {
            Fail("Profile could not be loaded.");
        }
        finally
        {
            IsLoading = false; Notify();
        }
    }

    public async Task SaveProfileAsync(CancellationToken ct = default)
    {
        ResetMessage();
        IsBusy = true; Notify();
        try
        {
            var username = (Profile.Username ?? string.Empty).Trim();
            var email = string.IsNullOrWhiteSpace(Profile.Email) ? null : Profile.Email!.Trim();
            var payload = new UpdateProfileRequest(username, email);
            var res = await _http.PutAsJsonAsync("api/users/me", payload, ct);
            if (!res.IsSuccessStatusCode)
            {
                var msg = await ReadErrorAsync(res) ?? "Profile could not be saved.";
                Fail(msg);
                return;
            }
            var updated = await res.Content.ReadFromJsonAsync<UserProfileDto>(cancellationToken: ct);
            if (updated is not null)
            {
                Profile.Username = updated.Username;
                Profile.Email = updated.Email ?? string.Empty;
            }
            Ok("Profile saved.");
        }
        catch
        {
            Fail("Profile could not be saved.");
        }
        finally
        {
            IsBusy = false; Notify();
        }
    }

    public async Task ChangePasswordAsync(CancellationToken ct = default)
    {
        ResetMessage();
        if (Password.NewPassword != Password.ConfirmPassword)
        {
            Fail("The new passwords do not match.");
            return;
        }

        IsBusy = true; Notify();
        try
        {
            var payload = new ChangePasswordRequest(Password.CurrentPassword, Password.NewPassword);
            var res = await _http.PostAsJsonAsync("api/users/me/change-password", payload, ct);
            if (!res.IsSuccessStatusCode)
            {
                var msg = await ReadErrorAsync(res) ?? "Password could not be changed.";
                Fail(msg);
                return;
            }
            // Clear fields while keeping the instance for binding.
            Password.CurrentPassword = string.Empty;
            Password.NewPassword = string.Empty;
            Password.ConfirmPassword = string.Empty;
            Ok("Password changed.");
        }
        catch
        {
            Fail("Password could not be changed.");
        }
        finally
        {
            IsBusy = false; Notify();
        }
    }

    private async Task<string?> ReadErrorAsync(HttpResponseMessage res)
    {
        try
        {
            var obj = await res.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
            if (obj is not null && obj.TryGetValue("message", out var v) && v is string s)
                return s;
        }
        catch { /* ignore */ }
        return null;
    }

    private void Ok(string? message) { IsError = false; Message = message; Notify(); }
    private void Fail(string message) { IsError = true; Message = message; Notify(); }
    private void ResetMessage() { IsError = false; Message = null; Notify(); }
    private void Notify() => OnChange?.Invoke();

    public class ProfileModel
    {
        public string Id { get; set; } = string.Empty;

        [Required]
        public string? Username { get; set; }

        [EmailAddress]
        [DataType(DataType.EmailAddress)]
        public string? Email { get; set; } = string.Empty;
    }

    public class PasswordModel
    {
        [Required]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required, MinLength(6)]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}