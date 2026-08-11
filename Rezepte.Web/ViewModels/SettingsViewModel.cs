using Microsoft.AspNetCore.Components.Authorization;
using Rezepte.Web.Components.Settings;

namespace Rezepte.Web.ViewModels;

public class SettingsViewModel
{
    public sealed class Item(string title, string icon, bool visible, Type componentType)
    {
        public string Title { get; } = title;
        public string Icon { get; } = icon;
        public Type ComponentType { get; } = componentType;
        public bool Visible { get; } = visible;
    }

    private readonly AuthenticationStateProvider _authenticationStateProvider;

    public IReadOnlyList<Item> Items { get; private set; } = Array.Empty<Item>();
    public Item? SelectedItem { get; private set; }

    public SettingsViewModel(AuthenticationStateProvider authenticationStateProvider)
    {
        _authenticationStateProvider = authenticationStateProvider;
    }

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

    public void Select(Item item) => SelectedItem = item;
}
