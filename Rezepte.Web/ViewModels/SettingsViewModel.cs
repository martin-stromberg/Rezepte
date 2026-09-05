using Microsoft.AspNetCore.Components.Authorization;

namespace Rezepte.Web.ViewModels;

/// <summary>
/// Provides the authorization-dependent state for the settings page.
/// The navigation items themselves are defined by the page component.
/// </summary>
public class SettingsViewModel
{
    /// <summary>
    /// Describes a single navigation entry of the settings page.
    /// </summary>
    public sealed class Item
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Item"/> class.
        /// </summary>
        /// <param name="title">The display title of the entry.</param>
        /// <param name="icon">The icon of the entry.</param>
        /// <param name="visible">Whether the entry is visible for the current user.</param>
        /// <param name="componentType">The component type rendered for the entry.</param>
        public Item(string title, string icon, bool visible, Type componentType)
        {
            Title = title;
            Icon = icon;
            Visible = visible;
            ComponentType = componentType;
        }

        /// <summary>
        /// Gets the display title of the entry.
        /// </summary>
        public string Title { get; }

        /// <summary>
        /// Gets the icon of the entry.
        /// </summary>
        public string Icon { get; }

        /// <summary>
        /// Gets the component type rendered for the entry.
        /// </summary>
        public Type ComponentType { get; }

        /// <summary>
        /// Gets a value indicating whether the entry is visible for the current user.
        /// </summary>
        public bool Visible { get; }
    }

    private readonly AuthenticationStateProvider _authenticationStateProvider;

    /// <summary>
    /// Gets a value indicating whether the current user is in the Admin role.
    /// </summary>
    public bool IsAdmin { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsViewModel"/> class.
    /// </summary>
    /// <param name="authenticationStateProvider">The authentication state provider.</param>
    public SettingsViewModel(AuthenticationStateProvider authenticationStateProvider)
    {
        _authenticationStateProvider = authenticationStateProvider;
    }

    /// <summary>
    /// Loads the current authentication state and determines the admin flag.
    /// </summary>
    /// <returns>A task that completes when the state has been loaded.</returns>
    public async Task InitializeAsync()
    {
        var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
        IsAdmin = authState.User?.IsInRole("Admin") ?? false;
    }
}
