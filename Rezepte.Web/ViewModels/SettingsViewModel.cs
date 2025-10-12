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
        var isAdmin = authenticationStateProvider.GetAuthenticationStateAsync().Result.User.IsInRole("Admin");
        Items =
        [
            new Item("Profil", "👤", true, typeof(UserProfile)),
            new Item("Benutzer", "👥", isAdmin, typeof(UserAdmin))
        ];
        SelectedItem = Items[0];
    }

    public void Select(Item item) => SelectedItem = item;
}