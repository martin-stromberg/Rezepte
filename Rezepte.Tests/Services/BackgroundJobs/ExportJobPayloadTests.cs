using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Moq;
using Rezepte.Web.Services.BackgroundJobs;
using Xunit;

namespace Rezepte.Tests.Services.BackgroundJobs;

/// <summary>
/// Class representing the export job payload tests.
/// </summary>
public class ExportJobPayloadTests
{
    /// <summary>
    /// From json should read include flags.
    /// </summary>
    [Fact]
    public void FromJson_ShouldReadIncludeFlags()
    {
        var payload = ExportJobPayload.FromJson("""{"includeImages":true,"includePdf":true}""");

        payload.IncludeImages.Should().BeTrue();
        payload.IncludePdf.Should().BeTrue();
    }

    /// <summary>
    /// From json should accept string booleans.
    /// </summary>
    [Fact]
    public void FromJson_ShouldAcceptStringBooleans()
    {
        var payload = ExportJobPayload.FromJson("""{"includeImages":"true","includePdf":"false"}""");

        payload.IncludeImages.Should().BeTrue();
        payload.IncludePdf.Should().BeFalse();
    }

    /// <summary>
    /// From json should use defaults for empty payload.
    /// </summary>
    [Fact]
    public void FromJson_ShouldUseDefaultsForEmptyPayload()
    {
        var payload = ExportJobPayload.FromJson(null);

        payload.IncludeImages.Should().BeFalse();
        payload.IncludePdf.Should().BeFalse();
    }

    /// <summary>
    /// File store should reject path traversal file name.
    /// </summary>
    [Fact]
    public void FileStore_ShouldRejectPathTraversalFileName()
    {
        var env = new Mock<IWebHostEnvironment>();
        env.SetupGet(e => e.ContentRootPath).Returns(Path.GetTempPath());
        var sut = new ExportJobFileStore(env.Object);

        var action = () => sut.GetPathForFileName("../export.zip");

        action.Should().Throw<InvalidOperationException>();
    }
}
