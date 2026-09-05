namespace Rezepte.Web.Components.Shared;

/// <summary>
/// Represents the collection import selection state class.
/// </summary>
public sealed class CollectionImportSelectionState
{
    private readonly HashSet<string> selectedItemIds = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> targetCookbookIdsByItem = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> itemStates = new(StringComparer.Ordinal);
    private readonly HashSet<string> itemIds = new(StringComparer.Ordinal);

    /// <summary>
    /// Represents the public class.
    /// </summary>
    public IReadOnlySet<string> SelectedItemIds => selectedItemIds;
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public IReadOnlyDictionary<string, string> TargetCookbookIdsByItem => targetCookbookIdsByItem;
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public string BulkTargetCookbookId { get; private set; } = string.Empty;
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public string? DefaultTargetCookbookId { get; private set; }

    /// <summary>
    /// Resets the value.
    /// </summary>
    /// <param name="defaultTargetCookbookId">The default target cookbook id parameter.</param>
    public void Reset(string? defaultTargetCookbookId)
    {
        selectedItemIds.Clear();
        targetCookbookIdsByItem.Clear();
        itemStates.Clear();
        itemIds.Clear();
        DefaultTargetCookbookId = string.IsNullOrWhiteSpace(defaultTargetCookbookId) ? null : defaultTargetCookbookId;
        BulkTargetCookbookId = DefaultTargetCookbookId ?? string.Empty;
    }

    /// <summary>
    /// Initializes the items.
    /// </summary>
    /// <param name="ids">The ids parameter.</param>
    /// <param name="readOnly">The read only parameter.</param>
    public void InitializeItems(IEnumerable<string> ids, bool readOnly)
    {
        foreach (var itemId in ids.Where(id => !string.IsNullOrWhiteSpace(id)))
        {
            itemIds.Add(itemId);

            if (!readOnly)
            {
                selectedItemIds.Add(itemId);
            }

            EnsureDefaultTarget(itemId);
        }
    }

    /// <summary>
    /// Ensures the default targets.
    /// </summary>
    /// <param name="ids">The ids parameter.</param>
    public void EnsureDefaultTargets(IEnumerable<string> ids)
    {
        foreach (var itemId in ids.Where(id => !string.IsNullOrWhiteSpace(id)))
        {
            itemIds.Add(itemId);
            EnsureDefaultTarget(itemId);
        }
    }

    /// <summary>
    /// Replaces the item states.
    /// </summary>
    /// <param name="states">The states parameter.</param>
    public void ReplaceItemStates(IEnumerable<(string ItemId, string? State)> states)
    {
        itemStates.Clear();

        foreach (var (itemId, state) in states)
        {
            if (!string.IsNullOrWhiteSpace(itemId) && !string.IsNullOrWhiteSpace(state))
            {
                itemStates[itemId] = state;
            }
        }
    }

    /// <summary>
    /// Determines whether item disabled.
    /// </summary>
    /// <param name="itemId">The item id parameter.</param>
    /// <param name="readOnly">The read only parameter.</param>
    /// <returns>The result.</returns>
    public bool IsItemDisabled(string itemId, bool readOnly)
    {
        return readOnly || (itemStates.TryGetValue(itemId, out var state) && state is "Importing" or "Succeeded" or "Failed");
    }

    /// <summary>
    /// Gets the target cookbook.
    /// </summary>
    /// <param name="itemId">The item id parameter.</param>
    /// <returns>The result.</returns>
    public string GetTargetCookbook(string itemId)
    {
        return targetCookbookIdsByItem.TryGetValue(itemId, out var cookbookId) ? cookbookId : string.Empty;
    }

    /// <summary>
    /// Toggles the item.
    /// </summary>
    /// <param name="itemId">The item id parameter.</param>
    /// <param name="selected">The selected parameter.</param>
    public void ToggleItem(string itemId, bool selected)
    {
        if (selected)
        {
            selectedItemIds.Add(itemId);
            EnsureDefaultTarget(itemId);
        }
        else
        {
            selectedItemIds.Remove(itemId);
        }
    }

    /// <summary>
    /// Sets the target cookbook.
    /// </summary>
    /// <param name="itemId">The item id parameter.</param>
    /// <param name="cookbookId">The cookbook id parameter.</param>
    public void SetTargetCookbook(string itemId, string? cookbookId)
    {
        if (string.IsNullOrWhiteSpace(cookbookId))
        {
            targetCookbookIdsByItem.Remove(itemId);
        }
        else
        {
            targetCookbookIdsByItem[itemId] = cookbookId;
        }
    }

    /// <summary>
    /// Sets the bulk target cookbook.
    /// </summary>
    /// <param name="cookbookId">The cookbook id parameter.</param>
    public void SetBulkTargetCookbook(string? cookbookId)
    {
        BulkTargetCookbookId = cookbookId ?? string.Empty;
    }

    /// <summary>
    /// Applies the bulk target cookbook.
    /// </summary>
    /// <param name="readOnly">The read only parameter.</param>
    public void ApplyBulkTargetCookbook(bool readOnly)
    {
        if (string.IsNullOrWhiteSpace(BulkTargetCookbookId))
        {
            return;
        }

        foreach (var itemId in selectedItemIds.ToArray())
        {
            if (!IsItemDisabled(itemId, readOnly))
            {
                targetCookbookIdsByItem[itemId] = BulkTargetCookbookId;
            }
        }
    }

    /// <summary>
    /// Selects the all.
    /// </summary>
    /// <param name="readOnly">The read only parameter.</param>
    public void SelectAll(bool readOnly)
    {
        foreach (var itemId in itemIds)
        {
            if (IsItemDisabled(itemId, readOnly))
            {
                continue;
            }

            selectedItemIds.Add(itemId);

            if (!string.IsNullOrWhiteSpace(BulkTargetCookbookId))
            {
                targetCookbookIdsByItem[itemId] = BulkTargetCookbookId;
            }
            else
            {
                EnsureDefaultTarget(itemId);
            }
        }
    }

    /// <summary>
    /// clears the selection.
    /// </summary>
    /// <param name="readOnly">The read only parameter.</param>
    public void ClearSelection(bool readOnly)
    {
        foreach (var itemId in itemIds)
        {
            if (!IsItemDisabled(itemId, readOnly))
            {
                selectedItemIds.Remove(itemId);
            }
        }
    }

    /// <summary>
    /// Determines whether submit.
    /// </summary>
    /// <param name="activeSessionId">The active session id parameter.</param>
    /// <param name="readOnly">The read only parameter.</param>
    /// <returns>The result.</returns>
    public bool CanSubmit(string? activeSessionId, bool readOnly)
    {
        return activeSessionId is not null
            && !readOnly
            && selectedItemIds.Count > 0
            && selectedItemIds.All(id => !string.IsNullOrWhiteSpace(GetTargetCookbook(id)));
    }

    private void EnsureDefaultTarget(string itemId)
    {
        if (!targetCookbookIdsByItem.ContainsKey(itemId) && !string.IsNullOrWhiteSpace(DefaultTargetCookbookId))
        {
            targetCookbookIdsByItem[itemId] = DefaultTargetCookbookId;
        }
    }
}
