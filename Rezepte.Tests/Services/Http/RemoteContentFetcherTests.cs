using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using FluentAssertions;
using Moq;
using Moq.Protected;
using Rezepte.Web.Services.Http;
using Xunit;

namespace Rezepte.Tests.Services.Http;

public class RemoteContentFetcherTests
{
    [Theory]
    [InlineData("https://example.com/recipe.html", true)]
    [InlineData("http://example.com/recipe.html", true)]
    [InlineData("ftp://example.com/recipe.html", false)]
    [InlineData("/relative/path", false)]
    [InlineData(null, false)]
    public void TryCreateHttpUri_ShouldAcceptOnlyAbsoluteHttpUrls(string? url, bool expected)
    {
        RemoteContentFetcher.TryCreateHttpUri(url, out _).Should().Be(expected);
    }

    [Fact]
    public async Task FetchAsync_ShouldReturnSeekableContentAndFileNameFromUrl()
    {
        var fetcher = CreateFetcher(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(Encoding.UTF8.GetBytes("<html></html>"))
        });

        var result = await fetcher.FetchAsync(new Uri("https://example.com/recipes/dish.html"), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.FileName.Should().Be("dish.html");
        result.Content!.Position.Should().Be(0);
        new StreamReader(result.Content).ReadToEnd().Should().Be("<html></html>");
    }

    [Fact]
    public async Task FetchAsync_ShouldDecompressGzippedContent()
    {
        var compressed = new MemoryStream();
        using (var gzip = new GZipStream(compressed, CompressionMode.Compress, leaveOpen: true))
        {
            gzip.Write(Encoding.UTF8.GetBytes("compressed payload"));
        }

        var content = new ByteArrayContent(compressed.ToArray());
        content.Headers.ContentEncoding.Add("gzip");
        var fetcher = CreateFetcher(new HttpResponseMessage(HttpStatusCode.OK) { Content = content });

        var result = await fetcher.FetchAsync(new Uri("https://example.com/page.html"), CancellationToken.None);

        new StreamReader(result.Content!).ReadToEnd().Should().Be("compressed payload");
    }

    [Fact]
    public async Task FetchAsync_ShouldFallBackToContentDispositionFileName()
    {
        var content = new ByteArrayContent(Encoding.UTF8.GetBytes("payload"));
        content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") { FileName = "\"export.zip\"" };
        var fetcher = CreateFetcher(new HttpResponseMessage(HttpStatusCode.OK) { Content = content });

        var result = await fetcher.FetchAsync(new Uri("https://example.com/"), CancellationToken.None);

        result.FileName.Should().Be("export.zip");
    }

    [Fact]
    public async Task FetchAsync_ShouldReportFailureWithErrorBody()
    {
        var fetcher = CreateFetcher(new HttpResponseMessage(HttpStatusCode.Forbidden)
        {
            Content = new StringContent("blocked")
        });

        var result = await fetcher.FetchAsync(new Uri("https://example.com/page.html"), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        result.ErrorBody.Should().Be("blocked");
        result.Content.Should().BeNull();
    }

    private static RemoteContentFetcher CreateFetcher(HttpResponseMessage response)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(new HttpClient(handler.Object));

        return new RemoteContentFetcher(factory.Object);
    }
}
