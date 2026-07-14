namespace Rezepte.Import.Abstractions;

public sealed record ImportCollectionPreview(
    string Id,
    string? Title,
    string? SourceUri,
    IReadOnlyList<ImportCollectionItem> Items);

public sealed record ImportCollectionItem(
    string Id,
    string Title,
    string Url,
    string? ThumbnailUrl = null,
    string? Description = null);

public sealed record ImportCollectionSelection(
    IReadOnlyList<ImportCollectionSelectionItem> Items);

public sealed record ImportCollectionSelectionItem(
    string ItemId,
    string Url,
    string TargetCookbookId);

public sealed record ImportCollectionItemStatus(
    string ItemId,
    string Title,
    string Url,
    string TargetCookbookId,
    ImportCollectionItemState State,
    string? Error = null,
    string? RecipeId = null);

public enum ImportCollectionItemState
{
    Pending,
    Importing,
    Succeeded,
    Failed
}
