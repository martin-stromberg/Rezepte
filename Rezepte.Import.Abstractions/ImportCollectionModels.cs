namespace Rezepte.Import.Abstractions;

/// <summary>
/// Preview of a recipe collection that can be imported.
/// </summary>
/// <param name="Id">Identifier of the collection.</param>
/// <param name="Title">Optional title of the collection.</param>
/// <param name="SourceUri">Optional URI the collection was loaded from.</param>
/// <param name="Items">Items contained in the collection.</param>
/// <returns>A new instance of the <see cref="ImportCollectionPreview"/> record.</returns>
public sealed record ImportCollectionPreview(
    string Id,
    string? Title,
    string? SourceUri,
    IReadOnlyList<ImportCollectionItem> Items);

/// <summary>
/// Single item inside an importable recipe collection.
/// </summary>
/// <param name="Id">Identifier of the item.</param>
/// <param name="Title">Title of the recipe.</param>
/// <param name="Url">URL of the recipe.</param>
/// <param name="ThumbnailUrl">Optional thumbnail URL.</param>
/// <param name="Description">Optional description.</param>
/// <returns>A new instance of the <see cref="ImportCollectionItem"/> record.</returns>
public sealed record ImportCollectionItem(
    string Id,
    string Title,
    string Url,
    string? ThumbnailUrl = null,
    string? Description = null);

/// <summary>
/// Selection of collection items that should be imported.
/// </summary>
/// <param name="Items">Selected items.</param>
/// <returns>A new instance of the <see cref="ImportCollectionSelection"/> record.</returns>
public sealed record ImportCollectionSelection(
    IReadOnlyList<ImportCollectionSelectionItem> Items);

/// <summary>
/// Reference to a selected collection item and its import target.
/// </summary>
/// <param name="ItemId">Identifier of the selected item.</param>
/// <param name="Url">URL of the selected item.</param>
/// <param name="TargetCookbookId">Identifier of the target cookbook.</param>
/// <returns>A new instance of the <see cref="ImportCollectionSelectionItem"/> record.</returns>
public sealed record ImportCollectionSelectionItem(
    string ItemId,
    string Url,
    string TargetCookbookId);

/// <summary>
/// Status of a single collection item during import.
/// </summary>
/// <param name="ItemId">Identifier of the item.</param>
/// <param name="Title">Title of the item.</param>
/// <param name="Url">URL of the item.</param>
/// <param name="TargetCookbookId">Identifier of the target cookbook.</param>
/// <param name="State">Current import state.</param>
/// <param name="Error">Optional error message.</param>
/// <param name="RecipeId">Identifier of the created recipe, if any.</param>
/// <returns>A new instance of the <see cref="ImportCollectionItemStatus"/> record.</returns>
public sealed record ImportCollectionItemStatus(
    string ItemId,
    string Title,
    string Url,
    string TargetCookbookId,
    ImportCollectionItemState State,
    string? Error = null,
    string? RecipeId = null);

/// <summary>
/// States of a collection item import operation.
/// </summary>
public enum ImportCollectionItemState
{
    /// <summary>
    /// The item is waiting to be imported.
    /// </summary>
    Pending,

    /// <summary>
    /// The item is currently being imported.
    /// </summary>
    Importing,

    /// <summary>
    /// The item was imported successfully.
    /// </summary>
    Succeeded,

    /// <summary>
    /// The item import failed.
    /// </summary>
    Failed
}
