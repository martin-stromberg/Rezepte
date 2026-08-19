using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Rezepte.Web.Data;
using Rezepte.Web.Services;
using Rezepte.Web.Entities;
using Xunit;

namespace Rezepte.Tests.Services;

public class SettingsServiceTests
{
    private static RezepteDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<RezepteDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new RezepteDbContext(options);
    }

    [Fact]
    public async Task GetUserAiEnabledAsync_ShouldReturnTrueByDefault_WhenNoSettingExists()
    {
        using var db = CreateDb();
        var sut = new SettingsService(db, new SecurityTxtSettingsService(db));

        var result = await sut.GetUserAiEnabledAsync("user-1", CancellationToken.None);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task SetUserAiEnabledAsync_ShouldPersistValue_WhenSettingInitially()
    {
        using var db = CreateDb();
        var sut = new SettingsService(db, new SecurityTxtSettingsService(db));

        await sut.SetUserAiEnabledAsync("user-2", false, CancellationToken.None);

        var read = await sut.GetUserAiEnabledAsync("user-2", CancellationToken.None);
        read.Should().BeFalse();
    }

    [Fact]
    public async Task SetUserAiEnabledAsync_ShouldUpdateExistingValue()
    {
        using var db = CreateDb();
        var sut = new SettingsService(db, new SecurityTxtSettingsService(db));

        await sut.SetUserAiEnabledAsync("user-2", false, CancellationToken.None);
        await sut.SetUserAiEnabledAsync("user-2", true, CancellationToken.None);

        var read = await sut.GetUserAiEnabledAsync("user-2", CancellationToken.None);
        read.Should().BeTrue();
    }

    [Fact]
    public async Task GetGlobalAiEnabledAsync_ShouldReturnTrueByDefault_WhenNoSettingExists()
    {
        using var db = CreateDb();
        var sut = new SettingsService(db, new SecurityTxtSettingsService(db));

        var result = await sut.GetGlobalAiEnabledAsync(CancellationToken.None);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task SetGlobalAiEnabledAsync_ShouldPersistValue_WhenSettingInitially()
    {
        using var db = CreateDb();
        var sut = new SettingsService(db, new SecurityTxtSettingsService(db));

        await sut.SetGlobalAiEnabledAsync(false, CancellationToken.None);

        var read = await sut.GetGlobalAiEnabledAsync(CancellationToken.None);
        read.Should().BeFalse();
    }

    [Fact]
    public async Task SetGlobalAiEnabledAsync_ShouldUpdateExistingValue()
    {
        using var db = CreateDb();
        var sut = new SettingsService(db, new SecurityTxtSettingsService(db));

        await sut.SetGlobalAiEnabledAsync(false, CancellationToken.None);
        await sut.SetGlobalAiEnabledAsync(true, CancellationToken.None);

        var read = await sut.GetGlobalAiEnabledAsync(CancellationToken.None);
        read.Should().BeTrue();
    }

    [Fact]
    public async Task UserAndGlobalSettings_ShouldBeIndependent()
    {
        using var db = CreateDb();
        var sut = new SettingsService(db, new SecurityTxtSettingsService(db));

        // set global false, user true -> effective: user still stored true, but callers should check global separately
        await sut.SetGlobalAiEnabledAsync(false, CancellationToken.None);
        await sut.SetUserAiEnabledAsync("user-3", true, CancellationToken.None);

        var global = await sut.GetGlobalAiEnabledAsync(CancellationToken.None);
        var user = await sut.GetUserAiEnabledAsync("user-3", CancellationToken.None);

        global.Should().BeFalse();
        user.Should().BeTrue();
    }

    [Fact]
    public async Task GetUserShoppingListEditModeAsync_ShouldReturnFalseByDefault_WhenNoSettingExists()
    {
        using var db = CreateDb();
        var sut = new SettingsService(db, new SecurityTxtSettingsService(db));

        (await sut.GetUserShoppingListEditModeAsync("user-1", CancellationToken.None)).Should().BeFalse();
    }

    [Fact]
    public async Task SetUserShoppingListEditModeAsync_ShouldPersistValue_PerUser()
    {
        using var db = CreateDb();
        var sut = new SettingsService(db, new SecurityTxtSettingsService(db));

        await sut.SetUserShoppingListEditModeAsync("user-1", true, CancellationToken.None);
        await sut.SetUserShoppingListEditModeAsync("user-2", false, CancellationToken.None);

        (await sut.GetUserShoppingListEditModeAsync("user-1", CancellationToken.None)).Should().BeTrue();
        (await sut.GetUserShoppingListEditModeAsync("user-2", CancellationToken.None)).Should().BeFalse();
    }

    [Fact]
    public async Task ShoppingListEditMode_ShouldUpdateExistingValue()
    {
        using var db = CreateDb();
        var sut = new SettingsService(db, new SecurityTxtSettingsService(db));

        await sut.SetUserShoppingListEditModeAsync("user-1", true, CancellationToken.None);
        await sut.SetUserShoppingListEditModeAsync("user-1", false, CancellationToken.None);

        (await sut.GetUserShoppingListEditModeAsync("user-1", CancellationToken.None)).Should().BeFalse();
    }

    [Fact]
    public async Task GetSecurityTxtSettingsAsync_ShouldReturnDefaults_WhenNoSettingsExist()
    {
        using var db = CreateDb();
        var sut = new SettingsService(db, new SecurityTxtSettingsService(db));

        var result = await sut.GetSecurityTxtSettingsAsync(CancellationToken.None);

        result.Enabled.Should().BeFalse();
        result.Contact.Should().BeNull();
        result.Expires.Should().BeNull();
        result.Encryption.Should().BeNull();
        result.Acknowledgments.Should().BeNull();
        result.PreferredLanguages.Should().BeNull();
        result.Canonical.Should().BeNull();
        result.Policy.Should().BeNull();
        result.Hiring.Should().BeNull();
    }

    [Fact]
    public async Task SetSecurityTxtSettingsAsync_ShouldPersistAllFields()
    {
        using var db = CreateDb();
        var sut = new SettingsService(db, new SecurityTxtSettingsService(db));
        var expires = new DateTimeOffset(2030, 6, 1, 0, 0, 0, TimeSpan.Zero);

        var settings = new Rezepte.Web.Dtos.SecurityTxtSettings(
            Enabled: true,
            Contact: "mailto:security@example.com",
            Expires: expires,
            Encryption: "https://example.com/pgp.txt",
            Acknowledgments: "https://example.com/thanks",
            PreferredLanguages: "de, en",
            Canonical: "https://example.com/security.txt",
            Policy: "https://example.com/policy",
            Hiring: "https://example.com/jobs");

        await sut.SetSecurityTxtSettingsAsync(settings, CancellationToken.None);
        var read = await sut.GetSecurityTxtSettingsAsync(CancellationToken.None);

        read.Enabled.Should().BeTrue();
        read.Contact.Should().Be("mailto:security@example.com");
        read.Expires.Should().Be(expires);
        read.Encryption.Should().Be("https://example.com/pgp.txt");
        read.Acknowledgments.Should().Be("https://example.com/thanks");
        read.PreferredLanguages.Should().Be("de, en");
        read.Canonical.Should().Be("https://example.com/security.txt");
        read.Policy.Should().Be("https://example.com/policy");
        read.Hiring.Should().Be("https://example.com/jobs");
    }

    [Fact]
    public async Task SetSecurityTxtSettingsAsync_ShouldOverwriteExistingValues()
    {
        using var db = CreateDb();
        var sut = new SettingsService(db, new SecurityTxtSettingsService(db));
        var expiresV1 = new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var expiresV2 = new DateTimeOffset(2031, 12, 31, 0, 0, 0, TimeSpan.Zero);

        var first = new Rezepte.Web.Dtos.SecurityTxtSettings(
            Enabled: true,
            Contact: "mailto:first@example.com",
            Expires: expiresV1,
            Encryption: null,
            Acknowledgments: null,
            PreferredLanguages: null,
            Canonical: null,
            Policy: null,
            Hiring: null);
        await sut.SetSecurityTxtSettingsAsync(first, CancellationToken.None);

        var second = new Rezepte.Web.Dtos.SecurityTxtSettings(
            Enabled: false,
            Contact: "mailto:second@example.com",
            Expires: expiresV2,
            Encryption: null,
            Acknowledgments: null,
            PreferredLanguages: null,
            Canonical: null,
            Policy: null,
            Hiring: null);
        await sut.SetSecurityTxtSettingsAsync(second, CancellationToken.None);

        var read = await sut.GetSecurityTxtSettingsAsync(CancellationToken.None);

        read.Enabled.Should().BeFalse();
        read.Contact.Should().Be("mailto:second@example.com");
        read.Expires.Should().Be(expiresV2);
    }

    [Fact]
    public async Task SetSecurityTxtSettingsAsync_ShouldClearNullableFields_WhenPassedNull()
    {
        using var db = CreateDb();
        var sut = new SettingsService(db, new SecurityTxtSettingsService(db));
        var expires = new DateTimeOffset(2030, 6, 1, 0, 0, 0, TimeSpan.Zero);

        var withValues = new Rezepte.Web.Dtos.SecurityTxtSettings(
            Enabled: true,
            Contact: "mailto:security@example.com",
            Expires: expires,
            Encryption: "https://example.com/pgp.txt",
            Acknowledgments: "https://example.com/thanks",
            PreferredLanguages: "de",
            Canonical: "https://example.com/ignored",
            Policy: "https://example.com/policy",
            Hiring: "https://example.com/jobs");
        await sut.SetSecurityTxtSettingsAsync(withValues, CancellationToken.None);

        var cleared = new Rezepte.Web.Dtos.SecurityTxtSettings(
            Enabled: true,
            Contact: "mailto:security@example.com",
            Expires: expires,
            Encryption: null,
            Acknowledgments: null,
            PreferredLanguages: null,
            Canonical: null,
            Policy: null,
            Hiring: null);
        await sut.SetSecurityTxtSettingsAsync(cleared, CancellationToken.None);

        var read = await sut.GetSecurityTxtSettingsAsync(CancellationToken.None);

        read.Encryption.Should().BeNull();
        read.Acknowledgments.Should().BeNull();
        read.PreferredLanguages.Should().BeNull();
        read.Canonical.Should().BeNull();
        read.Policy.Should().BeNull();
        read.Hiring.Should().BeNull();
    }

    [Fact]
    public async Task SetSecurityTxtSettingsAsync_ShouldPersistCanonical_WhenProvided()
    {
        using var db = CreateDb();
        var sut = new SettingsService(db, new SecurityTxtSettingsService(db));
        var expires = DateTimeOffset.UtcNow.AddDays(2);

        await sut.SetSecurityTxtSettingsAsync(new Rezepte.Web.Dtos.SecurityTxtSettings(
            Enabled: true,
            Contact: "mailto:security@example.com",
            Expires: expires,
            Encryption: null,
            Acknowledgments: null,
            PreferredLanguages: null,
            Canonical: "https://example.com/should-be-stored",
            Policy: null,
            Hiring: null), CancellationToken.None);

        (await db.Set<AppSetting>().FindAsync("SecurityTxt.Canonical"))!.Value.Should().Be("https://example.com/should-be-stored");
        (await sut.GetSecurityTxtSettingsAsync(CancellationToken.None)).Canonical.Should().Be("https://example.com/should-be-stored");
    }

    [Fact]
    public async Task GetSecurityTxtSettingsAsync_ShouldReturnNullExpires_WhenValueIsInvalidDateString()
    {
        using var db = CreateDb();
        var sut = new SettingsService(db, new SecurityTxtSettingsService(db));

        var settings = new Rezepte.Web.Dtos.SecurityTxtSettings(
            Enabled: true,
            Contact: "mailto:security@example.com",
            Expires: new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero),
            Encryption: null,
            Acknowledgments: null,
            PreferredLanguages: null,
            Canonical: null,
            Policy: null,
            Hiring: null);
        await sut.SetSecurityTxtSettingsAsync(settings, CancellationToken.None);

        var kv = await db.Set<Rezepte.Web.Entities.AppSetting>().FindAsync(new object[] { "SecurityTxt.Expires" });
        kv!.Value = "not-a-date";
        await db.SaveChangesAsync();

        var read = await sut.GetSecurityTxtSettingsAsync(CancellationToken.None);

        read.Expires.Should().BeNull();
    }

    [Fact]
    public async Task SetGlobalGoogleVisionEnabledAsync_ShouldPersistValue()
    {
        using var db = CreateDb();
        var sut = new SettingsService(db, new SecurityTxtSettingsService(db));

        await sut.SetGlobalGoogleVisionEnabledAsync(false, CancellationToken.None);

        var read = await sut.GetGlobalGoogleVisionEnabledAsync(CancellationToken.None);
        read.Should().BeFalse();
    }

    [Fact]
    public async Task SetGlobalGoogleVisionEnabledAsync_ShouldUpdateExistingValue()
    {
        using var db = CreateDb();
        var sut = new SettingsService(db, new SecurityTxtSettingsService(db));

        await sut.SetGlobalGoogleVisionEnabledAsync(false, CancellationToken.None);
        await sut.SetGlobalGoogleVisionEnabledAsync(true, CancellationToken.None);

        var read = await sut.GetGlobalGoogleVisionEnabledAsync(CancellationToken.None);
        read.Should().BeTrue();
    }

    [Fact]
    public async Task GetGlobalGoogleVisionEnabledAsync_ShouldReturnTrueByDefault_WhenNoSettingExists()
    {
        using var db = CreateDb();
        var sut = new SettingsService(db, new SecurityTxtSettingsService(db));

        var result = await sut.GetGlobalGoogleVisionEnabledAsync(CancellationToken.None);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task SetGlobalGeminiEnabledAsync_ShouldPersistValue()
    {
        using var db = CreateDb();
        var sut = new SettingsService(db, new SecurityTxtSettingsService(db));

        await sut.SetGlobalGeminiEnabledAsync(false, CancellationToken.None);

        var read = await sut.GetGlobalGeminiEnabledAsync(CancellationToken.None);
        read.Should().BeFalse();
    }

    [Fact]
    public async Task SetGlobalGeminiEnabledAsync_ShouldUpdateExistingValue()
    {
        using var db = CreateDb();
        var sut = new SettingsService(db, new SecurityTxtSettingsService(db));

        await sut.SetGlobalGeminiEnabledAsync(false, CancellationToken.None);
        await sut.SetGlobalGeminiEnabledAsync(true, CancellationToken.None);

        var read = await sut.GetGlobalGeminiEnabledAsync(CancellationToken.None);
        read.Should().BeTrue();
    }

    [Fact]
    public async Task GetGlobalGeminiEnabledAsync_ShouldReturnTrueByDefault_WhenNoSettingExists()
    {
        using var db = CreateDb();
        var sut = new SettingsService(db, new SecurityTxtSettingsService(db));

        var result = await sut.GetGlobalGeminiEnabledAsync(CancellationToken.None);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task SetGlobalMaxRequestsPerHourAsync_ShouldPersistValue()
    {
        using var db = CreateDb();
        var sut = new SettingsService(db, new SecurityTxtSettingsService(db));

        await sut.SetGlobalMaxRequestsPerHourAsync(100, CancellationToken.None);

        var read = await sut.GetGlobalMaxRequestsPerHourAsync(CancellationToken.None);
        read.Should().Be(100);
    }

    [Fact]
    public async Task SetGlobalMaxRequestsPerHourAsync_ShouldClearValue_WhenPassedNull()
    {
        using var db = CreateDb();
        var sut = new SettingsService(db, new SecurityTxtSettingsService(db));

        await sut.SetGlobalMaxRequestsPerHourAsync(100, CancellationToken.None);
        await sut.SetGlobalMaxRequestsPerHourAsync(null, CancellationToken.None);

        var read = await sut.GetGlobalMaxRequestsPerHourAsync(CancellationToken.None);
        read.Should().BeNull();
    }

    [Fact]
    public async Task GetGlobalMaxRequestsPerHourAsync_ShouldReturnNull_WhenNoSettingExists()
    {
        using var db = CreateDb();
        var sut = new SettingsService(db, new SecurityTxtSettingsService(db));

        var result = await sut.GetGlobalMaxRequestsPerHourAsync(CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task SetGlobalMaxRequestsPerDayAsync_ShouldPersistValue()
    {
        using var db = CreateDb();
        var sut = new SettingsService(db, new SecurityTxtSettingsService(db));

        await sut.SetGlobalMaxRequestsPerDayAsync(500, CancellationToken.None);

        var read = await sut.GetGlobalMaxRequestsPerDayAsync(CancellationToken.None);
        read.Should().Be(500);
    }

    [Fact]
    public async Task SetGlobalMaxRequestsPerDayAsync_ShouldClearValue_WhenPassedNull()
    {
        using var db = CreateDb();
        var sut = new SettingsService(db, new SecurityTxtSettingsService(db));

        await sut.SetGlobalMaxRequestsPerDayAsync(500, CancellationToken.None);
        await sut.SetGlobalMaxRequestsPerDayAsync(null, CancellationToken.None);

        var read = await sut.GetGlobalMaxRequestsPerDayAsync(CancellationToken.None);
        read.Should().BeNull();
    }

    [Fact]
    public async Task GetGlobalMaxRequestsPerDayAsync_ShouldReturnNull_WhenNoSettingExists()
    {
        using var db = CreateDb();
        var sut = new SettingsService(db, new SecurityTxtSettingsService(db));

        var result = await sut.GetGlobalMaxRequestsPerDayAsync(CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task SetGlobalDisableOnLimitReachedAsync_ShouldPersistValue()
    {
        using var db = CreateDb();
        var sut = new SettingsService(db, new SecurityTxtSettingsService(db));

        await sut.SetGlobalDisableOnLimitReachedAsync(true, CancellationToken.None);

        var read = await sut.GetGlobalDisableOnLimitReachedAsync(CancellationToken.None);
        read.Should().BeTrue();
    }

    [Fact]
    public async Task SetGlobalDisableOnLimitReachedAsync_ShouldUpdateExistingValue()
    {
        using var db = CreateDb();
        var sut = new SettingsService(db, new SecurityTxtSettingsService(db));

        await sut.SetGlobalDisableOnLimitReachedAsync(true, CancellationToken.None);
        await sut.SetGlobalDisableOnLimitReachedAsync(false, CancellationToken.None);

        var read = await sut.GetGlobalDisableOnLimitReachedAsync(CancellationToken.None);
        read.Should().BeFalse();
    }

    [Fact]
    public async Task GetGlobalDisableOnLimitReachedAsync_ShouldReturnFalseByDefault_WhenNoSettingExists()
    {
        using var db = CreateDb();
        var sut = new SettingsService(db, new SecurityTxtSettingsService(db));

        var result = await sut.GetGlobalDisableOnLimitReachedAsync(CancellationToken.None);

        result.Should().BeFalse();
    }
}

