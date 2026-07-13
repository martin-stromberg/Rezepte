namespace Rezepte.Import.Abstractions;

public sealed record ImportedImage
{
    public byte[] Data { get; init; } = [];
    public string? ContentType { get; init; }
    public string? FileName { get; init; }
}
