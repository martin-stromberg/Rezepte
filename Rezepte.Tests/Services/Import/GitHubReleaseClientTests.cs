using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Rezepte.Web.Configuration;
using Rezepte.Web.Services.Import.Plugins;
using Xunit;

namespace Rezepte.Tests.Services.Import;

/// <summary>
/// Class representing the git hub release client tests.
/// </summary>
public sealed class GitHubReleaseClientTests
{
    /// <summary>
    /// Get latest release async should use asset api url for download.
    /// </summary>
    [Fact]
    public async Task GetLatestReleaseAsync_ShouldUseAssetApiUrlForDownload()
    {
        var handler = new QueueHandler(Json(HttpStatusCode.OK, """
            {
              "id": 1,
              "tag_name": "v1",
              "assets": [
                {
                  "id": 2,
                  "name": "release.zip",
                  "url": "https://api.github.test/repos/owner/repo/releases/assets/2",
                  "browser_download_url": "https://github.com/owner/repo/releases/download/v1/release.zip"
                }
              ]
            }
            """));
        var sut = CreateSut(handler);

        var release = await sut.GetLatestReleaseAsync(new GitHubRepository("owner", "repo", "https://github.com/owner/repo"), "secret");

        release.Should().NotBeNull();
        release!.Assets.Should().ContainSingle().Which.DownloadUrl.Should().Be("https://api.github.test/repos/owner/repo/releases/assets/2");
    }

    /// <summary>
    /// Download asset async should request asset api url as octet stream.
    /// </summary>
    [Fact]
    public async Task DownloadAssetAsync_ShouldRequestAssetApiUrlAsOctetStream()
    {
        var handler = new QueueHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent([1, 2, 3])
        });
        var sut = CreateSut(handler);
        var targetPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".zip");

        try
        {
            await sut.DownloadAssetAsync(new GitHubReleaseAsset(2, "release.zip", "https://api.github.test/repos/owner/repo/releases/assets/2"), targetPath, "secret");

            File.ReadAllBytes(targetPath).Should().Equal([1, 2, 3]);
            var request = handler.Requests.Should().ContainSingle().Subject;
            request.RequestUri!.ToString().Should().Be("https://api.github.test/repos/owner/repo/releases/assets/2");
            request.Headers.Accept.Select(a => a.MediaType).Should().Contain("application/octet-stream");
            request.Headers.Authorization!.Parameter.Should().Be("secret");
        }
        finally
        {
            if (File.Exists(targetPath))
            {
                File.Delete(targetPath);
            }
        }
    }

    /// <summary>
    /// Get latest release async should retry retry after rate limit and then succeed.
    /// </summary>
    [Fact]
    public async Task GetLatestReleaseAsync_ShouldRetryRetryAfterRateLimitAndThenSucceed()
    {
        var handler = new QueueHandler(
            RateLimited(HttpStatusCode.TooManyRequests, retryAfterSeconds: 0),
            Json(HttpStatusCode.OK, """{"id":1,"tag_name":"v1","assets":[]}"""));
        var sut = CreateSut(handler);

        var release = await sut.GetLatestReleaseAsync(new GitHubRepository("owner", "repo", "https://github.com/owner/repo"), "secret");

        release.Should().NotBeNull();
        release!.TagName.Should().Be("v1");
        handler.Requests.Should().HaveCount(2);
        handler.Requests.Select(r => r.Headers.Authorization?.Parameter).Should().OnlyContain(parameter => parameter == "secret");
    }

    /// <summary>
    /// Download asset async should throw dedicated rate limit exception for rate limited forbidden.
    /// </summary>
    [Fact]
    public async Task DownloadAssetAsync_ShouldThrowDedicatedRateLimitExceptionForRateLimitedForbidden()
    {
        var handler = new QueueHandler(
            RateLimited(HttpStatusCode.Forbidden, retryAfterSeconds: 0),
            RateLimited(HttpStatusCode.Forbidden, retryAfterSeconds: 0));
        var sut = CreateSut(handler);
        var targetPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".zip");

        var act = () => sut.DownloadAssetAsync(new GitHubReleaseAsset(1, "plugin.zip", "https://example.invalid/plugin.zip"), targetPath, null);

        await act.Should().ThrowAsync<GitHubRateLimitException>();
        File.Exists(targetPath).Should().BeFalse();
    }

    private static GitHubReleaseClient CreateSut(HttpMessageHandler handler)
    {
        var client = new HttpClient(handler);
        var options = Options.Create(new PluginUpdateOptions
        {
            GitHubApiBaseUrl = "https://api.github.test",
            UserAgent = "Rezepte.Tests"
        });
        return new GitHubReleaseClient(client, options);
    }

    private static HttpResponseMessage Json(HttpStatusCode statusCode, string json)
        => new(statusCode) { Content = new StringContent(json) };

    private static HttpResponseMessage RateLimited(HttpStatusCode statusCode, int retryAfterSeconds)
    {
        var response = new HttpResponseMessage(statusCode);
        response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(retryAfterSeconds));
        response.Headers.Add("X-RateLimit-Remaining", "0");
        return response;
    }

    private sealed class QueueHandler(params HttpResponseMessage[] responses) : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses = new(responses);
        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(CloneForAssertion(request));
            return Task.FromResult(_responses.Dequeue());
        }

        private static HttpRequestMessage CloneForAssertion(HttpRequestMessage request)
        {
            var clone = new HttpRequestMessage(request.Method, request.RequestUri);
            foreach (var header in request.Headers)
            {
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            return clone;
        }
    }
}
