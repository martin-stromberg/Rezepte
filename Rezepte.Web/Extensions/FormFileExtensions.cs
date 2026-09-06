namespace Rezepte.Web.Extensions;

/// <summary>
/// Represents the form file extensions class.
/// </summary>
public static class FormFileExtensions
{
    /// <summary>
    /// Copies the uploaded file into a seekable memory stream positioned at the beginning.
    /// </summary>
    /// <param name="file">The file parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <param>...</param>
    /// <param>...</param>
    /// <param>...</param>
    /// <returns>The result.</returns>
    public static async Task<MemoryStream> ReadToMemoryStreamAsync(this IFormFile file, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(file);

        var ms = new MemoryStream();
        try
        {
            await file.CopyToAsync(ms, ct).ConfigureAwait(false);
            ms.Seek(0, SeekOrigin.Begin);
            return ms;
        }
        catch
        {
            await ms.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }
}
