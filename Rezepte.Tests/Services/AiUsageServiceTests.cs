using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Rezepte.Web.Data;
using Rezepte.Web.Entities;
using Rezepte.Web.Services;
using Xunit;

namespace Rezepte.Tests.Services;

public class AiUsageServiceTests
{
    private static RezepteDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<RezepteDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new RezepteDbContext(options);
    }

    [Fact]
    public async Task TryRecordRequestAsync_counts_only_request_entries_and_blocks_when_limit_reached()
    {
        using var db = CreateDb();
        var settings = new SettingsService(db);
        var logger = NullLogger<AiUsageService>.Instance;
        var sut = new AiUsageService(db, settings, logger);

        // set limit = 2 per hour
        await settings.SetGlobalMaxRequestsPerHourAsync(2, CancellationToken.None);

        // first two requests allowed
        var ok1 = await sut.TryRecordRequestAsync("u1", "Svc.A", CancellationToken.None);
        var ok2 = await sut.TryRecordRequestAsync("u1", "Svc.A", CancellationToken.None);
        ok1.Should().BeTrue();
        ok2.Should().BeTrue();

        // third request should be blocked
        var ok3 = await sut.TryRecordRequestAsync("u1", "Svc.A", CancellationToken.None);
        ok3.Should().BeFalse();

        // ensure DB contains exactly 2 Request-type logs
        var reqCount = await db.Set<AiRequestLog>().CountAsync(l => l.Type == AiRequestLogType.Request);
        reqCount.Should().Be(2);
    }

    [Fact]
    public async Task Success_entries_are_not_counted_against_limits()
    {
        using var db = CreateDb();
        var settings = new SettingsService(db);
        var logger = NullLogger<AiUsageService>.Instance;
        var sut = new AiUsageService(db, settings, logger);

        // limit = 1 per hour
        await settings.SetGlobalMaxRequestsPerHourAsync(1, CancellationToken.None);

        // add a Success log (should not count)
        await sut.RecordRequestAsync("u2", "Svc.B.Success", AiRequestLogType.Success, CancellationToken.None);

        // TryRecord should allow one request (since only Success exists)
        var allowed = await sut.TryRecordRequestAsync("u2", "Svc.B", CancellationToken.None);
        allowed.Should().BeTrue();

        // Now another TryRecord should be blocked
        var blocked = await sut.TryRecordRequestAsync("u2", "Svc.B", CancellationToken.None);
        blocked.Should().BeFalse();

        // verify DB counts: 1 Success + 1 Request
        var successCount = await db.Set<AiRequestLog>().CountAsync(l => l.Type == AiRequestLogType.Success);
        var requestCount = await db.Set<AiRequestLog>().CountAsync(l => l.Type == AiRequestLogType.Request);
        successCount.Should().Be(1);
        requestCount.Should().Be(1);
    }

    [Fact]
    public async Task When_disableOnLimit_reached_global_ai_is_disabled()
    {
        using var db = CreateDb();
        var settings = new SettingsService(db);
        var logger = NullLogger<AiUsageService>.Instance;
        var sut = new AiUsageService(db, settings, logger);

        // configure: limit = 1, disable-on-limit = true
        await settings.SetGlobalMaxRequestsPerHourAsync(1, CancellationToken.None);
        await settings.SetGlobalDisableOnLimitReachedAsync(true, CancellationToken.None);
        // ensure global ai initially enabled
        await settings.SetGlobalAiEnabledAsync(true, CancellationToken.None);
        (await settings.GetGlobalAiEnabledAsync(CancellationToken.None)).Should().BeTrue();

        // first request allowed
        var ok1 = await sut.TryRecordRequestAsync("u3", "Svc.C", CancellationToken.None);
        ok1.Should().BeTrue();

        // second request blocked -> should trigger disable
        var ok2 = await sut.TryRecordRequestAsync("u3", "Svc.C", CancellationToken.None);
        ok2.Should().BeFalse();

        // global ai should now be disabled
        (await settings.GetGlobalAiEnabledAsync(CancellationToken.None)).Should().BeFalse();
    }
}