using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Rezepte.Web.Extensions;
using Xunit;

namespace Rezepte.Tests.Extensions;

/// <summary>
/// Class representing the form file extensions tests.
/// </summary>
public class FormFileExtensionsTests
{
    /// <summary>
    /// Read to memory stream async should return rewound copy of uploaded file.
    /// </summary>
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
