using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Rezepte.Web.Extensions;
using Xunit;

namespace Rezepte.Tests.Extensions;

public class FormFileExtensionsTests
{
    [Fact]
    public async Task ReadToMemoryStreamAsync_ShouldReturnRewoundCopyOfUploadedFile()
    {
        var bytes = Encoding.UTF8.GetBytes("uploaded content");
        var file = new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", "recipes.zip");

        await using var stream = await file.ReadToMemoryStreamAsync(CancellationToken.None);

        stream.Position.Should().Be(0);
        stream.ToArray().Should().Equal(bytes);
    }
}
