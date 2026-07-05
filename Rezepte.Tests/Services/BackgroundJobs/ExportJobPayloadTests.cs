using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Moq;
using Rezepte.Web.Services.BackgroundJobs;
using Xunit;

namespace Rezepte.Tests.Services.BackgroundJobs;

public class ExportJobPayloadTests
{
    [Fact]
    public void FromJson_ShouldReadIncludeFlags()
    {
        var payload = ExportJobPayload.FromJson("""{"includeImages":true,"includePdf":true}""");

        payload.IncludeImages.Should().BeTrue();
        payload.IncludePdf.Should().BeTrue();
    }

    [Fact]
    public void FromJson_ShouldAcceptStringBooleans()
    {
        var payload = ExportJobPayload.FromJson("""{"includeImages":"true","includePdf":"false"}""");

        payload.IncludeImages.Should().BeTrue();
        payload.IncludePdf.Should().BeFalse();
    }

    [Fact]
    public void FromJson_ShouldUseDefaultsForEmptyPayload()
    {
        var payload = ExportJobPayload.FromJson(null);

        payload.IncludeImages.Should().BeFalse();
        payload.IncludePdf.Should().BeFalse();
    }

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
