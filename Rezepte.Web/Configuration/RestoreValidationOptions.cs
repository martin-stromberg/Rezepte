namespace Rezepte.Web.Configuration;

public sealed class RestoreValidationOptions
{
    /// <summary>
    /// Maximum total upload size in bytes for a restore ZIP archive.
    /// </summary>
    public long MaxUploadFileSizeBytes { get; set; } = 1_500_000_000; // 1.5 GB

    /// <summary>
    /// Maximum number of archive entries allowed in the restore ZIP.
    /// </summary>
    public int MaxArchiveEntries { get; set; } = 10_000;

    /// <summary>
    /// Maximum total uncompressed size of all archive entries combined.
    /// </summary>
    public long MaxTotalUncompressedBytes { get; set; } = 2_000_000_000; // 2 GB

    /// <summary>
    /// Maximum uncompressed size of the recipes.json entry.
    /// </summary>
    public long MaxRecipesJsonUncompressedBytes { get; set; } = 52_428_800; // 50 MB

    /// <summary>
    /// Maximum uncompressed size of a single image entry.
    /// </summary>
    public long MaxImageUncompressedBytes { get; set; } = 52_428_800; // 50 MB

    /// <summary>
    /// Maximum total size of all image entries combined.
    /// </summary>
    public long MaxTotalImageBytes { get; set; } = 1_500_000_000; // 1.5 GB

    /// <summary>
    /// Maximum allowed compression ratio for any single archive entry.
    /// </summary>
    public double MaxCompressionRatio { get; set; } = 100.0;
}
