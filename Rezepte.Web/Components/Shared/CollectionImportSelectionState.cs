namespace Rezepte.Web.Components.Shared;

public sealed class CollectionImportSelectionState
{
    private readonly HashSet<string> selectedItemIds = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> targetCookbookIdsByItem = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> itemStates = new(StringComparer.Ordinal);
    private readonly HashSet<string> itemIds = new(StringComparer.Ordinal);

    public IReadOnlySet<string> SelectedItemIds => selectedItemIds;
    public IReadOnlyDictionary<string, string> TargetCookbookIdsByItem => targetCookbookIdsByItem;
    public string BulkTargetCookbookId { get; private set; } = string.Empty;
    public string? DefaultTargetCookbookId { get; private set; }

    public void Reset(string? defaultTargetCookbookId)
    {
        selectedItemIds.Clear();
        targetCookbookIdsByItem.Clear();
        itemStates.Clear();
        itemIds.Clear();
        DefaultTargetCookbookId = string.IsNullOrWhiteSpace(defaultTargetCookbookId) ? null : defaultTargetCookbookId;
        BulkTargetCookbookId = DefaultTargetCookbookId ?? string.Empty;
    }

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

    public void EnsureDefaultTargets(IEnumerable<string> ids)
    {
        foreach (var itemId in ids.Where(id => !string.IsNullOrWhiteSpace(id)))
        {
            itemIds.Add(itemId);
            EnsureDefaultTarget(itemId);
        }
    }

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

    public bool IsItemDisabled(string itemId, bool readOnly)
    {
        return readOnly || (itemStates.TryGetValue(itemId, out var state) && state is "Importing" or "Succeeded" or "Failed");
    }

    public string GetTargetCookbook(string itemId)
    {
        return targetCookbookIdsByItem.TryGetValue(itemId, out var cookbookId) ? cookbookId : string.Empty;
    }

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

    public void SetBulkTargetCookbook(string? cookbookId)
    {
        BulkTargetCookbookId = cookbookId ?? string.Empty;
    }

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
