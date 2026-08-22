using System.IO.Compression;
using System.Net;

namespace Rezepte.Web.Services.Http;

public sealed record RemoteContentResult(
    bool Success,
    MemoryStream? Content,
    string FileName,
    HttpStatusCode StatusCode,
    string? ErrorBody)
{
    public static RemoteContentResult Failed(HttpStatusCode statusCode, string? errorBody) =>
        new(false, null, string.Empty, statusCode, errorBody);
}

public interface IRemoteContentFetcher
{
    /// <summary>
    /// Downloads the resource behind <paramref name="uri"/> into a seekable memory stream,
    /// transparently decompressing encoded responses and inferring a file name.
    /// </summary>
    Task<RemoteContentResult> FetchAsync(Uri uri, CancellationToken ct);
}

public sealed class RemoteContentFetcher(IHttpClientFactory httpClientFactory) : IRemoteContentFetcher
{
    private const string DefaultFileName = "import-from-url";
    private const string BrowserUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/141.0.0.0 Safari/537.36 Edg/141.0.0.0";
    private const string BrowserAccept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7";
    private const string BrowserAcceptLanguage = "de,de-DE;q=0.9,en;q=0.8,en-GB;q=0.7,en-US;q=0.6";
    private static readonly Uri Referrer = new("https://www.bing.com/");
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(30);

    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    public async Task<RemoteContentResult> FetchAsync(Uri uri, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(uri);

        var client = _httpClientFactory.CreateClient();
        client.Timeout = RequestTimeout;

        // Browser-like headers reduce the chance of a 403 from remote hosts.
        client.DefaultRequestHeaders.UserAgent.ParseAdd(BrowserUserAgent);
        client.DefaultRequestHeaders.Accept.ParseAdd(BrowserAccept);
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd(BrowserAcceptLanguage);
        client.DefaultRequestHeaders.Referrer = Referrer;

        using var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            return RemoteContentResult.Failed(response.StatusCode, errorBody);
        }

        var content = new MemoryStream();
        try
        {
            await using var remoteStream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            await using var source = Decompress(remoteStream, response.Content.Headers.ContentEncoding);
            await source.CopyToAsync(content, ct).ConfigureAwait(false);
            content.Seek(0, SeekOrigin.Begin);
        }
        catch
        {
            await content.DisposeAsync().ConfigureAwait(false);
            throw;
        }

        return new RemoteContentResult(true, content, InferFileName(uri, response), response.StatusCode, null);
    }

    private static Stream Decompress(Stream remoteStream, IEnumerable<string> contentEncodings)
    {
        var encodings = contentEncodings
            .Select(e => e?.Trim().ToLowerInvariant())
            .Where(e => !string.IsNullOrEmpty(e))
            .ToArray();

        // Encodings are applied in the listed order, so decompression must reverse that order.
        var source = remoteStream;
        for (var i = encodings.Length - 1; i >= 0; i--)
        {
            switch (encodings[i])
            {
                case "br":
                case "brotli":
                    source = new BrotliStream(source, CompressionMode.Decompress, leaveOpen: true);
                    break;
                case "gzip":
                    source = new GZipStream(source, CompressionMode.Decompress, leaveOpen: true);
                    break;
                case "deflate":
                    source = new DeflateStream(source, CompressionMode.Decompress, leaveOpen: true);
                    break;
                default:
                    // Unknown encoding: fall back to the raw stream.
                    return remoteStream;
            }
        }

        return source;
    }

    private static string InferFileName(Uri uri, HttpResponseMessage response)
    {
        var fileName = Path.GetFileName(uri.LocalPath);
        if (!string.IsNullOrWhiteSpace(fileName)) return fileName;

        var contentDisposition = response.Content.Headers.ContentDisposition;
        if (!string.IsNullOrWhiteSpace(contentDisposition?.FileNameStar))
            return contentDisposition.FileNameStar.Trim('"');
        if (!string.IsNullOrWhiteSpace(contentDisposition?.FileName))
            return contentDisposition.FileName.Trim('"');

        return DefaultFileName;
    }
}
