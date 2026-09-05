using Microsoft.AspNetCore.Components.Authorization;
using Rezepte.Web.Components.Settings;

namespace Rezepte.Web.ViewModels;

/// <summary>
/// Represents the settings view model class.
/// </summary>
public class SettingsViewModel
{
    /// <summary>
    /// items the value.
    /// </summary>
    /// <param name="title">The title parameter.</param>
    /// <param name="icon">The icon parameter.</param>
    /// <param name="visible">The visible parameter.</param>
    /// <param name="componentType">The component type parameter.</param>
    /// <returns>The result.</returns>
    public sealed class Item(string title, string icon, bool visible, Type componentType)
    {
        /// <summary>
        /// Represents the public class.
        /// </summary>
        public string Title { get; } = title;
        /// <summary>
        /// Represents the public class.
        /// </summary>
        public string Icon { get; } = icon;
        /// <summary>
        /// Represents the public class.
        /// </summary>
        public Type ComponentType { get; } = componentType;
        /// <summary>
        /// Represents the public class.
        /// </summary>
        public bool Visible { get; } = visible;
    }

    private readonly AuthenticationStateProvider _authenticationStateProvider;

    /// <summary>
    /// arrays the value.
    /// </summary>
    /// <typeparam name="Item">The item type parameter.</typeparam>
    /// <typeparam>...</typeparam>
    /// <typeparam>...</typeparam>
    /// <typeparam>...</typeparam>
    /// <returns>The result.</returns>
    public IReadOnlyList<Item> Items { get; private set; } = Array.Empty<Item>();
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public Item? SelectedItem { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsViewModel"/> class.
    /// </summary>
    /// <param name="authenticationStateProvider">The authentication state provider parameter.</param>
    public SettingsViewModel(AuthenticationStateProvider authenticationStateProvider)
    {
        _authenticationStateProvider = authenticationStateProvider;
    }

    /// <summary>
    /// Initializes the async.
    /// </summary>
    public async Task InitializeAsync()
    {
        var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
        var isAdmin = authState.User?.IsInRole("Admin") ?? false;

        Items = new List<Item>
        {
            new Item("Profil", "👤", true, typeof(UserProfile)),
            new Item("Einstellungen", "⚙️", true, typeof(AiSettings)),
            new Item("Benutzer", "👥", isAdmin, typeof(UserAdmin)),
            new Item("Plugins", "🔌", isAdmin, typeof(PluginSettings)),
            new Item("Updates", "⬆️", isAdmin, typeof(ApplicationUpdates)),
            new Item("security.txt", "🔒", isAdmin, typeof(SecurityTxtSettings)),
            new Item("Datenexport", "📤", true, typeof(Rezepte.Web.Components.Settings.ExportData)),
            new Item("Sicherung", "💾", isAdmin, typeof(Rezepte.Web.Components.Settings.BackupRestore)),
            new Item("Nutzungsstatistiken", "📊", true, typeof(Rezepte.Web.Components.Settings.UsageStats))
        };

        SelectedItem = Items.First();
    }

    /// <summary>
    /// Selects the value.
    /// </summary>
    /// <param name="item">The item parameter.</param>
    public void Select(Item item) => SelectedItem = item;
}
