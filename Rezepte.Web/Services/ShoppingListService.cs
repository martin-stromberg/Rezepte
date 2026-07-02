using Microsoft.EntityFrameworkCore;
using Rezepte.Web.Data;
using Rezepte.Web.Entities;

namespace Rezepte.Web.Services;

public interface IShoppingListService
{
    Task<List<ShoppingListGroup>> GetGroupsAsync(string userId, CancellationToken ct);
    Task<ShoppingListGroup> EnsureDefaultGroupAsync(string userId, CancellationToken ct);
    Task<(bool ok, string? error, ShoppingListGroup? group)> AddGroupAsync(string userId, string? name, CancellationToken ct);
    Task<(bool ok, string? error)> RenameGroupAsync(string userId, string groupId, string name, CancellationToken ct);
    Task<(bool ok, string? error)> DeleteGroupAsync(string userId, string groupId, CancellationToken ct);
    Task<(bool ok, string? error, ShoppingListItem? item)> AddItemAsync(string userId, string groupId, decimal amount, string? unit, string name, CancellationToken ct);
    Task<(bool ok, string? error)> UpdateItemAsync(string userId, string itemId, decimal amount, string? unit, string name, CancellationToken ct);
    Task<(bool ok, string? error)> SetItemCheckedAsync(string userId, string itemId, bool isChecked, CancellationToken ct);
    Task<(bool ok, string? error)> DeleteItemAsync(string userId, string itemId, CancellationToken ct);
    Task<List<ShoppingListRecipeIngredient>> GetRecipeIngredientsAsync(string userId, string recipeId, CancellationToken ct);
    Task<(bool ok, string? error, ShoppingListGroup? group)> AddRecipeIngredientsAsync(string userId, string recipeId, IReadOnlyCollection<string> ingredientIds, CancellationToken ct);
}

public sealed record ShoppingListRecipeIngredient(string Id, decimal Amount, string? Unit, string Name, string? StepTitle);

public class ShoppingListService(RezepteDbContext db) : IShoppingListService
{
    private const string DefaultGroupName = "Einkaufsliste";
    private readonly RezepteDbContext _db = db;

    public async Task<List<ShoppingListGroup>> GetGroupsAsync(string userId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return new List<ShoppingListGroup>();
        }

        await EnsureDefaultGroupAsync(userId, ct);

        return await _db.ShoppingListGroups
            .AsNoTracking()
            .Include(g => g.Items.OrderBy(i => i.OrderIndex).ThenBy(i => i.CreatedAt))
            .Where(g => g.UserId == userId)
            .OrderBy(g => g.OrderIndex)
            .ThenBy(g => g.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<ShoppingListGroup> EnsureDefaultGroupAsync(string userId, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        var existing = await _db.ShoppingListGroups
            .FirstOrDefaultAsync(g => g.UserId == userId, ct);
        if (existing is not null)
        {
            return existing;
        }

        var group = new ShoppingListGroup
        {
            UserId = userId,
            Name = DefaultGroupName,
            OrderIndex = 0
        };
        _db.ShoppingListGroups.Add(group);
        await _db.SaveChangesAsync(ct);
        return group;
    }

    public async Task<(bool ok, string? error, ShoppingListGroup? group)> AddGroupAsync(string userId, string? name, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(userId)) return (false, "Nicht angemeldet.", null);

        var trimmedName = string.IsNullOrWhiteSpace(name) ? "Neue Gruppe" : name.Trim();
        if (trimmedName.Length > 128) return (false, "Der Gruppenname darf maximal 128 Zeichen lang sein.", null);

        var nextOrder = await NextGroupOrderAsync(userId, ct);
        var group = new ShoppingListGroup
        {
            UserId = userId,
            Name = trimmedName,
            OrderIndex = nextOrder
        };
        _db.ShoppingListGroups.Add(group);
        await _db.SaveChangesAsync(ct);
        return (true, null, group);
    }

    public async Task<(bool ok, string? error)> RenameGroupAsync(string userId, string groupId, string name, CancellationToken ct)
    {
        var group = await FindGroupAsync(userId, groupId, ct);
        if (group is null) return (false, "Gruppe nicht gefunden.");
        if (string.IsNullOrWhiteSpace(name)) return (false, "Der Gruppenname darf nicht leer sein.");
        if (name.Trim().Length > 128) return (false, "Der Gruppenname darf maximal 128 Zeichen lang sein.");

        group.Name = name.Trim();
        group.ModifiedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return (true, null);
    }

    public async Task<(bool ok, string? error)> DeleteGroupAsync(string userId, string groupId, CancellationToken ct)
    {
        var group = await _db.ShoppingListGroups
            .Include(g => g.Items)
            .FirstOrDefaultAsync(g => g.Id == groupId && g.UserId == userId, ct);
        if (group is null) return (false, "Gruppe nicht gefunden.");

        _db.ShoppingListGroups.Remove(group);
        await _db.SaveChangesAsync(ct);
        await EnsureDefaultGroupAsync(userId, ct);
        return (true, null);
    }

    public async Task<(bool ok, string? error, ShoppingListItem? item)> AddItemAsync(string userId, string groupId, decimal amount, string? unit, string name, CancellationToken ct)
    {
        var group = await FindGroupAsync(userId, groupId, ct);
        if (group is null) return (false, "Gruppe nicht gefunden.", null);
        if (string.IsNullOrWhiteSpace(name)) return (false, "Die Zutat braucht eine Bezeichnung.", null);
        if (amount < 0) return (false, "Die Menge darf nicht negativ sein.", null);

        var trimmedName = name.Trim();
        if (trimmedName.Length > 200) return (false, "Die Bezeichnung darf maximal 200 Zeichen lang sein.", null);
        var trimmedUnit = string.IsNullOrWhiteSpace(unit) ? null : unit.Trim();
        if (trimmedUnit?.Length > 64) return (false, "Die Einheit darf maximal 64 Zeichen lang sein.", null);

        var nextOrder = await NextItemOrderAsync(groupId, ct);
        var item = new ShoppingListItem
        {
            GroupId = group.Id,
            Amount = amount,
            Unit = trimmedUnit,
            Name = trimmedName,
            OrderIndex = nextOrder
        };
        _db.ShoppingListItems.Add(item);
        await _db.SaveChangesAsync(ct);
        return (true, null, item);
    }

    public async Task<(bool ok, string? error)> UpdateItemAsync(string userId, string itemId, decimal amount, string? unit, string name, CancellationToken ct)
    {
        var item = await FindItemAsync(userId, itemId, ct);
        if (item is null) return (false, "Eintrag nicht gefunden.");
        if (string.IsNullOrWhiteSpace(name)) return (false, "Die Zutat braucht eine Bezeichnung.");
        if (amount < 0) return (false, "Die Menge darf nicht negativ sein.");

        var trimmedName = name.Trim();
        if (trimmedName.Length > 200) return (false, "Die Bezeichnung darf maximal 200 Zeichen lang sein.");
        var trimmedUnit = string.IsNullOrWhiteSpace(unit) ? null : unit.Trim();
        if (trimmedUnit?.Length > 64) return (false, "Die Einheit darf maximal 64 Zeichen lang sein.");

        item.Amount = amount;
        item.Unit = trimmedUnit;
        item.Name = trimmedName;
        item.ModifiedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return (true, null);
    }

    public async Task<(bool ok, string? error)> SetItemCheckedAsync(string userId, string itemId, bool isChecked, CancellationToken ct)
    {
        var item = await FindItemAsync(userId, itemId, ct);
        if (item is null) return (false, "Eintrag nicht gefunden.");

        item.IsChecked = isChecked;
        item.ModifiedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return (true, null);
    }

    public async Task<(bool ok, string? error)> DeleteItemAsync(string userId, string itemId, CancellationToken ct)
    {
        var item = await FindItemAsync(userId, itemId, ct);
        if (item is null) return (false, "Eintrag nicht gefunden.");

        _db.ShoppingListItems.Remove(item);
        await _db.SaveChangesAsync(ct);
        return (true, null);
    }

    public async Task<List<ShoppingListRecipeIngredient>> GetRecipeIngredientsAsync(string userId, string recipeId, CancellationToken ct)
    {
        var recipe = await _db.Recipes
            .AsNoTracking()
            .Include(r => r.Steps)
                .ThenInclude(s => s.Ingredients)
            .FirstOrDefaultAsync(r => r.Id == recipeId && r.UserId == userId, ct);
        if (recipe is null) return new List<ShoppingListRecipeIngredient>();

        return recipe.Steps
            .OrderBy(s => s.StepIndex)
            .SelectMany(s => s.Ingredients
                .OrderBy(i => i.Name)
                .Select(i => new ShoppingListRecipeIngredient(i.Id, i.Amount, i.Unit, i.Name, s.Title)))
            .ToList();
    }

    public async Task<(bool ok, string? error, ShoppingListGroup? group)> AddRecipeIngredientsAsync(string userId, string recipeId, IReadOnlyCollection<string> ingredientIds, CancellationToken ct)
    {
        if (ingredientIds.Count == 0) return (false, "Bitte mindestens eine Zutat auswaehlen.", null);

        var recipe = await _db.Recipes
            .Include(r => r.Steps)
                .ThenInclude(s => s.Ingredients)
            .FirstOrDefaultAsync(r => r.Id == recipeId && r.UserId == userId, ct);
        if (recipe is null) return (false, "Rezept nicht gefunden.", null);

        var selected = recipe.Steps
            .OrderBy(s => s.StepIndex)
            .SelectMany(s => s.Ingredients)
            .Where(i => ingredientIds.Contains(i.Id))
            .ToList();
        if (selected.Count == 0) return (false, "Keine passenden Zutaten gefunden.", null);

        var group = new ShoppingListGroup
        {
            UserId = userId,
            Name = recipe.Title,
            RecipeId = recipe.Id,
            OrderIndex = await NextGroupOrderAsync(userId, ct)
        };
        var itemOrder = 0;
        foreach (var ingredient in selected)
        {
            group.Items.Add(new ShoppingListItem
            {
                Amount = ingredient.Amount,
                Unit = ingredient.Unit,
                Name = ingredient.Name,
                OrderIndex = itemOrder++
            });
        }

        _db.ShoppingListGroups.Add(group);
        await _db.SaveChangesAsync(ct);
        return (true, null, group);
    }

    private async Task<ShoppingListGroup?> FindGroupAsync(string userId, string groupId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(groupId)) return null;
        return await _db.ShoppingListGroups.FirstOrDefaultAsync(g => g.Id == groupId && g.UserId == userId, ct);
    }

    private async Task<ShoppingListItem?> FindItemAsync(string userId, string itemId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(itemId)) return null;
        return await _db.ShoppingListItems
            .Include(i => i.Group)
            .FirstOrDefaultAsync(i => i.Id == itemId && i.Group != null && i.Group.UserId == userId, ct);
    }

    private async Task<int> NextGroupOrderAsync(string userId, CancellationToken ct)
    {
        var max = await _db.ShoppingListGroups
            .Where(g => g.UserId == userId)
            .Select(g => (int?)g.OrderIndex)
            .MaxAsync(ct);
        return (max ?? -1) + 1;
    }

    private async Task<int> NextItemOrderAsync(string groupId, CancellationToken ct)
    {
        var max = await _db.ShoppingListItems
            .Where(i => i.GroupId == groupId)
            .Select(i => (int?)i.OrderIndex)
            .MaxAsync(ct);
        return (max ?? -1) + 1;
    }
}
