using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
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
}
