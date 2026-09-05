using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Rezepte.Tests.TestSupport;
using Moq;
using Rezepte.Web.Services;
using Xunit;

namespace Rezepte.Tests.Services;

/// <summary>
/// Class representing the export cleanup hosted service tests.
/// </summary>
public sealed class ExportCleanupHostedServiceTests
{
    private readonly Mock<IExportCleanupService> _cleanup = new();
    private readonly FixedTimeProvider _time = new(new DateTimeOffset(2026, 9, 5, 12, 0, 0, TimeSpan.Zero));

    private ExportCleanupHostedService CreateSut()
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => _cleanup.Object);
        return new ExportCleanupHostedService(services.BuildServiceProvider(), _time, NullLogger<ExportCleanupHostedService>.Instance);
    }

    /// <summary>
    /// Run if due async should run cleanup when due.
    /// </summary>
    [Fact]
    public async Task RunIfDueAsync_ShouldRunCleanup_WhenDue()
    {
        _cleanup.Setup(c => c.IsCleanupDueAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _cleanup.Setup(c => c.RunCleanupAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExportCleanupResult(0, 0, _time.GetLocalNow()));

        await CreateSut().RunIfDueAsync(CancellationToken.None);

        _cleanup.Verify(c => c.RunCleanupAsync(_time.GetLocalNow(), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Run if due async should skip when not due.
    /// </summary>
    [Fact]
    public async Task RunIfDueAsync_ShouldSkip_WhenNotDue()
    {
        _cleanup.Setup(c => c.IsCleanupDueAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        await CreateSut().RunIfDueAsync(CancellationToken.None);

        _cleanup.Verify(c => c.RunCleanupAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Run if due async should swallow cleanup errors.
    /// </summary>
    [Fact]
    public async Task RunIfDueAsync_ShouldSwallowCleanupErrors()
    {
        _cleanup.Setup(c => c.IsCleanupDueAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _cleanup.Setup(c => c.RunCleanupAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("disk"));

        var act = () => CreateSut().RunIfDueAsync(CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    /// <summary>
    /// Start async should check immediately on startup.
    /// </summary>
    [Fact]
    public async Task StartAsync_ShouldCheckImmediatelyOnStartup()
    {
        _cleanup.Setup(c => c.IsCleanupDueAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var sut = CreateSut();

        await sut.StartAsync(CancellationToken.None);
        await Task.Delay(100);
        await sut.StopAsync(CancellationToken.None);

        _cleanup.Verify(c => c.IsCleanupDueAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }
}
