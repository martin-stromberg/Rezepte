using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using msTools.Updater;
using Rezepte.Web.Configuration;
using Rezepte.Web.Services;
using Rezepte.Web.Services.Updates;
using Xunit;

namespace Rezepte.Tests.Services;

public sealed class ApplicationUpdatePreInstallHandlerTests
{
    [Fact]
    public async Task RunPreInstallBackupAsync_ShouldAwaitScopedBackupService()
    {
        var services = new ServiceCollection();
        var backup = new RecordingBackupService();
        services.AddScoped<IUpdateBackupService>(_ => backup);
        await using var provider = services.BuildServiceProvider();
        var sut = new ApplicationUpdatePreInstallHandler(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<ApplicationUpdatePreInstallHandler>.Instance);

        await sut.RunPreInstallBackupAsync();

        backup.Calls.Should().Be(1);
        backup.Completed.Should().BeTrue();
    }

    [Fact]
    public async Task RunPreInstallBackupAsync_ShouldPropagateBackupFailure()
    {
        var services = new ServiceCollection();
        services.AddScoped<IUpdateBackupService, FailingBackupService>();
        await using var provider = services.BuildServiceProvider();
        var sut = new ApplicationUpdatePreInstallHandler(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<ApplicationUpdatePreInstallHandler>.Instance);

        var act = () => sut.RunPreInstallBackupAsync();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("backup failed");
    }

    [Fact]
    public async Task ApplicationUpdateHostedService_ShouldAllowInstall_WhenPreInstallBackupSucceeds()
    {
        var events = new AutoUpdateEvents();
        var handler = new RecordingPreInstallHandler();
        var sut = new ApplicationUpdateHostedService(
            events,
            handler,
            Options.Create(new ApplicationUpdateOptions { Enabled = true }),
            NullLogger<ApplicationUpdateHostedService>.Instance);

        await sut.StartAsync(CancellationToken.None);

        var canceled = events.RaiseBeforeInstall(this, new FileInfo("update.zip"));

        canceled.Should().BeFalse();
        handler.Calls.Should().Be(1);
    }

    [Fact]
    public async Task ApplicationUpdateHostedService_ShouldCancelInstall_WhenPreInstallBackupFails()
    {
        var events = new AutoUpdateEvents();
        var handler = new FailingPreInstallHandler();
        var sut = new ApplicationUpdateHostedService(
            events,
            handler,
            Options.Create(new ApplicationUpdateOptions { Enabled = true }),
            NullLogger<ApplicationUpdateHostedService>.Instance);

        await sut.StartAsync(CancellationToken.None);

        var canceled = events.RaiseBeforeInstall(this, new FileInfo("update.zip"));

        canceled.Should().BeTrue();
    }

    private sealed class RecordingBackupService : IUpdateBackupService
    {
        public int Calls { get; private set; }
        public bool Completed { get; private set; }

        public async Task<UpdateBackupResult> CreateBackupAsync(CancellationToken ct = default)
        {
            Calls++;
            await Task.Delay(10, ct);
            Completed = true;
            return new UpdateBackupResult("backup.zip", 1);
        }
    }

    private sealed class FailingBackupService : IUpdateBackupService
    {
        public Task<UpdateBackupResult> CreateBackupAsync(CancellationToken ct = default)
            => throw new InvalidOperationException("backup failed");
    }

    private sealed class RecordingPreInstallHandler : IApplicationUpdatePreInstallHandler
    {
        public int Calls { get; private set; }

        public Task RunPreInstallBackupAsync(CancellationToken ct = default)
        {
            Calls++;
            return Task.CompletedTask;
        }
    }

    private sealed class FailingPreInstallHandler : IApplicationUpdatePreInstallHandler
    {
        public Task RunPreInstallBackupAsync(CancellationToken ct = default)
            => throw new InvalidOperationException("backup failed");
    }
}
