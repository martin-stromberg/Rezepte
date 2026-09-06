namespace Rezepte.Import.Abstractions;

/// <summary>
/// Image imported as part of a recipe.
/// </summary>
public sealed record ImportedImage
{
    /// <summary>
    /// Raw image data.
    /// </summary>
    public byte[] Data { get; init; } = [];

    /// <summary>
    /// MIME content type of the image, if known.
    /// </summary>
    public string? ContentType { get; init; }

    /// <summary>
    /// Original file name of the image.
    /// </summary>
    public string? FileName { get; init; }
}
