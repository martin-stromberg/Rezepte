using Microsoft.AspNetCore.Components.Authorization;
using Rezepte.Web.Components.Settings;
using System.Runtime.InteropServices;

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

    public IReadOnlyList<Item> Items { get; }
    public Item SelectedItem { get; private set; }
    public bool Visible { get; }

    public SettingsViewModel(AuthenticationStateProvider authenticationStateProvider)
    {
        // Achtung: synchronous block / kurz und bewusst — Auth-State ist zur Initialisierung benötigt.
        var authState = authenticationStateProvider.GetAuthenticationStateAsync().GetAwaiter().GetResult();
        var isAdmin = authState.User?.IsInRole("Admin") ?? false;

        Items = new List<Item>
        {
            new Item("Profil", "👤", true, typeof(UserProfile)),
            new Item("Einstellungen", "⚙️", true, typeof(AiSettings)), // Neu: Einstellungen-Bereich (userbezogene Optionen)
            new Item("Benutzer", "👥", isAdmin, typeof(UserAdmin)),
            new Item("Datenexport", "📤", true, typeof(Rezepte.Web.Components.Settings.ExportData)),
            new Item("Sicherung", "💾", isAdmin, typeof(Rezepte.Web.Components.Settings.BackupRestore)),
            new Item("Nutzungsstatistiken", "📊", true, typeof(Rezepte.Web.Components.Settings.UsageStats))
        };

        SelectedItem = Items.First();
    }

    public void Select(Item item) => SelectedItem = item;
}