namespace Rezepte.Web.Extensions;

public static class FormFileExtensions
{
    /// <summary>
    /// Copies the uploaded file into a seekable memory stream positioned at the beginning.
    /// </summary>
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
