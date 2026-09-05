using FluentAssertions;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Rezepte.Tests.TestSupport;
using Moq;
using Rezepte.Web.Controllers;
using Rezepte.Web.Services;
using Xunit;

namespace Rezepte.Tests.Controllers;

public sealed class AdminExportCleanupControllerTests
{
    private readonly Mock<IExportCleanupService> _cleanup = new();
    private readonly FixedTimeProvider _time = new(new DateTimeOffset(2026, 9, 5, 12, 0, 0, TimeSpan.Zero));

    private AdminExportCleanupController CreateSut()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "admin-1")], "Test"));
        return new AdminExportCleanupController(_cleanup.Object, _time, NullLogger<AdminExportCleanupController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } }
        };
    }

    [Fact]
    public async Task GetSettings_ShouldReturnFormattedTime()
    {
        _cleanup.Setup(c => c.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExportCleanupSettings(new TimeOnly(4, 15), null));

        var result = await CreateSut().GetSettings();

        var dto = result.Result.Should().BeOfType<OkObjectResult>().Subject.Value
            .Should().BeOfType<AdminExportCleanupController.ExportCleanupSettingsDto>().Subject;
        dto.CleanupTime.Should().Be("04:15");
        dto.LastRunAt.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("25:00")]
    [InlineData("3 Uhr")]
    public async Task UpdateSettings_ShouldRejectInvalidTime(string? value)
    {
        var result = await CreateSut().UpdateSettings(new AdminExportCleanupController.UpdateExportCleanupSettingsRequest(value));

        result.Result.Should().BeOfType<BadRequestObjectResult>();
        _cleanup.Verify(c => c.SetCleanupTimeAsync(It.IsAny<TimeOnly>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateSettings_ShouldPersistParsedTime()
    {
        _cleanup.Setup(c => c.SetCleanupTimeAsync(new TimeOnly(23, 45), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExportCleanupSettings(new TimeOnly(23, 45), null));

        var result = await CreateSut().UpdateSettings(new AdminExportCleanupController.UpdateExportCleanupSettingsRequest("23:45"));

        var dto = result.Result.Should().BeOfType<OkObjectResult>().Subject.Value
            .Should().BeOfType<AdminExportCleanupController.ExportCleanupSettingsDto>().Subject;
        dto.CleanupTime.Should().Be("23:45");
    }

    [Fact]
    public async Task Run_ShouldTriggerCleanupWithCurrentTime()
    {
        _cleanup.Setup(c => c.RunCleanupAsync(_time.GetLocalNow(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExportCleanupResult(2, 3, _time.GetLocalNow()));

        var result = await CreateSut().Run();

        var dto = result.Result.Should().BeOfType<OkObjectResult>().Subject.Value
            .Should().BeOfType<AdminExportCleanupController.ExportCleanupRunDto>().Subject;
        dto.DeletedFiles.Should().Be(2);
        dto.DeletedRecords.Should().Be(3);
    }
}
