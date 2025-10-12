using Rezepte.Web.Components.Settings;

namespace Rezepte.Web.ViewModels;

public class SettingsViewModel
{
    public sealed class Item(string title, string icon, Type componentType)
    {
        public string Title { get; } = title;
        public string Icon { get; } = icon;
        public Type ComponentType { get; } = componentType;
    }

    public IReadOnlyList<Item> Items { get; }
    public Item SelectedItem { get; private set; }

    public SettingsViewModel()
    {
        Items =
        [
            new Item("Profil", "👤", typeof(UserProfile))
            // Weitere Punkte können hier ergänzt werden
        ];
        SelectedItem = Items[0];
    }

    public void Select(Item item) => SelectedItem = item;
}