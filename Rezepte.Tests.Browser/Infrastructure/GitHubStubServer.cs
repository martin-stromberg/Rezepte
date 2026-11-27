using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace Rezepte.Tests.Browser.Infrastructure;

/// <summary>
/// Minimal local HTTP stub for the GitHub releases API used by the plugin update check browser
/// tests. Serves a configurable response for <c>GET /repos/{owner}/{repo}/releases/latest</c>.
/// Implemented on <see cref="TcpListener"/> instead of <see cref="HttpListener"/>, because
/// <see cref="HttpListener"/> requires an URL ACL reservation or administrative privileges on
/// Windows, which makes tests fail on non-admin developer machines.
/// </summary>
public sealed class GitHubStubServer : IDisposable
{
    private static readonly TimeSpan ShutdownWaitTimeout = TimeSpan.FromSeconds(5);

    private readonly TcpListener _listener;
    private readonly CancellationTokenSource _shutdown = new();
    private readonly Task _acceptLoop;
    private readonly ConcurrentBag<Task> _handlerTasks = new();

    // Written by the test thread, read by handler threads. volatile guarantees that a handler
    // never sees a stale body/status combination; the body is always written before the status.
    private volatile int _latestReleaseStatusCode = 404;
    private volatile string _latestReleaseBody = """{"message":"Not Found"}""";
    private volatile byte[]? _assetDownloadBody;
    private int _requestCount;

    private GitHubStubServer(TcpListener listener)
    {
        _listener = listener;
        BaseAddress = $"http://127.0.0.1:{((IPEndPoint)listener.LocalEndpoint).Port}";
        _acceptLoop = Task.Run(AcceptLoopAsync);
    }

    /// <summary>
    /// Gets the base URL of the stub server (e.g. <c>http://127.0.0.1:51234</c>).
    /// </summary>
    public string BaseAddress { get; }

    /// <summary>
    /// Gets the number of HTTP requests the stub has handled so far.
    /// </summary>
    public int RequestCount => _requestCount;

    /// <summary>
    /// Starts a stub server on a free loopback port.
    /// </summary>
    /// <returns>The running <see cref="GitHubStubServer"/> instance.</returns>
    public static GitHubStubServer Start()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return new GitHubStubServer(listener);
    }

    /// <summary>
    /// Configures the stub to return the given release for every <c>releases/latest</c> request.
    /// The asset download URL points at the stub itself so a test that exercises the download
    /// path only needs to configure the binary body via <see cref="RespondWithAssetDownload"/>.
    /// </summary>
    /// <param name="releaseId">The GitHub release id to report.</param>
    /// <param name="tagName">The release tag name to report.</param>
    /// <param name="assetId">The asset id to report.</param>
    /// <param name="assetName">The asset file name; must end in <c>.zip</c> to be picked up.</param>
    public void RespondWithRelease(long releaseId, string tagName, long assetId, string assetName)
    {
        _latestReleaseBody = JsonSerializer.Serialize(new
        {
            id = releaseId,
            tag_name = tagName,
            assets = new[]
            {
                new
                {
                    id = assetId,
                    name = assetName,
                    url = $"{BaseAddress}/assets/{assetId}/download"
                }
            }
        });
        _latestReleaseStatusCode = 200;
    }

    /// <summary>
    /// Configures the binary body served for every <c>/assets/{id}/download</c> request, so the
    /// update check can stream a real package file through the download path.
    /// </summary>
    /// <param name="body">The raw asset bytes, typically a ZIP archive.</param>
    public void RespondWithAssetDownload(byte[] body)
    {
        _assetDownloadBody = body;
    }

    /// <summary>
    /// Configures the stub to return 404 for every <c>releases/latest</c> request, simulating a
    /// repository without a published release. A previously configured asset download body is
    /// cleared so no test inherits a package configured by another test.
    /// </summary>
    public void RespondNotFound()
    {
        _latestReleaseBody = """{"message":"Not Found"}""";
        _latestReleaseStatusCode = 404;
        _assetDownloadBody = null;
    }

    /// <summary>
    /// Stops the listener and the accept loop.
    /// </summary>
    public void Dispose()
    {
        _shutdown.Cancel();
        _listener.Stop();
        var stopwatch = Stopwatch.StartNew();
        try
        {
            // Wait for the accept loop first: a client accepted between _listener.Stop()
            // and the loop's exit still enqueues its handler into _handlerTasks, so a
            // snapshot taken earlier would miss it. Once the loop has exited the bag is
            // final, and all in-flight handlers are awaited within the same total
            // budget; a handler that keeps running after teardown would hit disposed state.
            if (_acceptLoop.Wait(ShutdownWaitTimeout))
            {
                var remaining = ShutdownWaitTimeout - stopwatch.Elapsed;
                if (remaining > TimeSpan.Zero)
                {
                    Task.WaitAll(_handlerTasks.ToArray(), remaining);
                }
            }
        }
        catch (AggregateException)
        {
            // The accept loop ends with a cancellation; teardown must not fail the test run.
        }

        _shutdown.Dispose();
    }

    private async Task AcceptLoopAsync()
    {
        while (!_shutdown.IsCancellationRequested)
        {
            TcpClient client;
            try
            {
                client = await _listener.AcceptTcpClientAsync(_shutdown.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (SocketException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }

            _handlerTasks.Add(HandleClientAsync(client));
        }
    }

    private async Task HandleClientAsync(TcpClient client)
    {
        using (client)
        {
            try
            {
                var stream = client.GetStream();
                var buffer = new byte[8192];
                var request = new StringBuilder();
                while (!request.ToString().Contains("\r\n\r\n", StringComparison.Ordinal))
                {
                    var read = await stream.ReadAsync(buffer, _shutdown.Token).ConfigureAwait(false);
                    if (read == 0)
                    {
                        return;
                    }

                    request.Append(Encoding.ASCII.GetString(buffer, 0, read));
                }

                Interlocked.Increment(ref _requestCount);
                var requestLine = request.ToString().Split("\r\n", 2, StringSplitOptions.None)[0];
                var parts = requestLine.Split(' ');
                var path = parts.Length > 1 ? parts[1] : "/";

                var isLatestRelease = path.EndsWith("/releases/latest", StringComparison.Ordinal);
                var isAssetDownload = path.StartsWith("/assets/", StringComparison.Ordinal)
                    && path.EndsWith("/download", StringComparison.Ordinal);

                int statusCode;
                string contentType;
                byte[] bodyBytes;
                if (isAssetDownload && _assetDownloadBody is not null)
                {
                    statusCode = 200;
                    contentType = "application/octet-stream";
                    bodyBytes = _assetDownloadBody;
                }
                else
                {
                    statusCode = isLatestRelease ? _latestReleaseStatusCode : 404;
                    contentType = "application/json; charset=utf-8";
                    var body = isLatestRelease ? _latestReleaseBody : """{"message":"Not Found"}""";
                    bodyBytes = Encoding.UTF8.GetBytes(body);
                }

                var reason = statusCode == 200 ? "OK" : "Not Found";
                var header = $"HTTP/1.1 {statusCode} {reason}\r\n"
                    + $"Content-Type: {contentType}\r\n"
                    + $"Content-Length: {bodyBytes.Length}\r\n"
                    + "Connection: close\r\n\r\n";
                await stream.WriteAsync(Encoding.ASCII.GetBytes(header), _shutdown.Token).ConfigureAwait(false);
                await stream.WriteAsync(bodyBytes, _shutdown.Token).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is IOException or SocketException or ObjectDisposedException or OperationCanceledException)
            {
                // A client that aborts mid-request must not take the stub down.
            }
        }
    }
}
