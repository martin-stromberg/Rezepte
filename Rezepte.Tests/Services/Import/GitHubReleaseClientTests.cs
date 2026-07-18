using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Rezepte.Web.Configuration;
using Rezepte.Web.Services.Import.Plugins;
using Xunit;

namespace Rezepte.Tests.Services.Import;

public sealed class GitHubReleaseClientTests
{
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
