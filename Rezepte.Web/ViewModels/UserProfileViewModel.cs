using System.Net;
using System.Net.Http.Json;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Rezepte.Web.Contracts;

namespace Rezepte.Web.ViewModels;

public class UserProfileViewModel
{
    private readonly HttpClient _http;
    private readonly ILogger<UserProfileViewModel> _logger;
    public event Action? OnChange;

    public bool IsLoading { get; private set; } = true;
    public bool IsBusy { get; private set; }
    public bool IsError { get; private set; }
    public string? Message { get; private set; }

    public ProfileModel Profile { get; private set; } = new();
    public PasswordModel Password { get; set; } = new();

    public UserProfileViewModel(ApiClient apiClient, ILogger<UserProfileViewModel> logger)
    {
        _http = apiClient.Http;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Loading the user profile failed.");
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
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Saving the user profile failed.");
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
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Changing the password failed.");
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
        catch (Exception ex) when (ex is JsonException or NotSupportedException or HttpRequestException)
        {
            _logger.LogDebug(ex, "Error response with status {StatusCode} did not contain a readable message.", (int)res.StatusCode);
        }
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
