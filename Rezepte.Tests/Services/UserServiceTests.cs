using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Rezepte.Web.Data;
using Rezepte.Web.Security;
using Rezepte.Web.Services;
using Rezepte.Web.Services.BackgroundJobs;
using Rezepte.Web.Services.Validation;
using Xunit;

namespace Rezepte.Tests.Services;

/// <summary>
/// Class representing the user service tests.
/// </summary>
public class UserServiceTests
{
    private static RezepteDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<RezepteDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new RezepteDbContext(options);
    }

    private static UserService CreateSut(RezepteDbContext db)
    {
        var queue = new Mock<IBackgroundJobQueue>();
        return new UserService(db, new UsernameValidator(), queue.Object);
    }

    /// <summary>
    /// Register async should create first user as admin when no users exist.
    /// </summary>
    [Fact]
    public async Task RegisterAsync_ShouldCreateFirstUserAsAdmin_WhenNoUsersExist()
    {
        using var db = CreateDb();
        var sut = CreateSut(db);

        var (ok, error, user) = await sut.RegisterAsync("alice", "password123", false, CancellationToken.None);

        ok.Should().BeTrue();
        error.Should().BeNull();
        user.Should().NotBeNull();
        user!.IsAdmin.Should().BeTrue();
    }

    /// <summary>
    /// Register async should fail when username already exists.
    /// </summary>
    [Fact]
    public async Task RegisterAsync_ShouldFail_WhenUsernameAlreadyExists()
    {
        using var db = CreateDb();
        var sut = CreateSut(db);
        await sut.RegisterAsync("bob", "pw1", false, CancellationToken.None);

        var (ok, error, user) = await sut.RegisterAsync("bob", "pw2", false, CancellationToken.None);

        ok.Should().BeFalse();
        error.Should().NotBeNull();
        user.Should().BeNull();
    }

    /// <summary>
    /// Login async should return user when password valid.
    /// </summary>
    [Fact]
    public async Task LoginAsync_ShouldReturnUser_WhenPasswordValid()
    {
        using var db = CreateDb();
        var sut = CreateSut(db);
        await sut.RegisterAsync("carol", "secret!", false, CancellationToken.None);

        var user = await sut.LoginAsync("carol", "secret!", CancellationToken.None);

        user.Should().NotBeNull();
        user!.Username.Should().Be("carol");
    }

    /// <summary>
    /// Login async should return null when password invalid.
    /// </summary>
    [Fact]
    public async Task LoginAsync_ShouldReturnNull_WhenPasswordInvalid()
    {
        using var db = CreateDb();
        var sut = CreateSut(db);
        await sut.RegisterAsync("dave", "right", false, CancellationToken.None);

        var user = await sut.LoginAsync("dave", "wrong", CancellationToken.None);

        user.Should().BeNull();
    }

    /// <summary>
    /// Update profile async should update username and email when valid.
    /// </summary>
    [Fact]
    public async Task UpdateProfileAsync_ShouldUpdateUsernameAndEmail_WhenValid()
    {
        using var db = CreateDb();
        var sut = CreateSut(db);
        var (_, _, user) = await sut.RegisterAsync("edgar", "pw", false, CancellationToken.None);
        user.Should().NotBeNull();

        var (ok, error, updated) = await sut.UpdateProfileAsync(user!.Id, "edward", "ed@example.com", CancellationToken.None);

        ok.Should().BeTrue();
        error.Should().BeNull();
        updated!.Username.Should().Be("edward");
        updated.Email.Should().Be("ed@example.com");
    }

    /// <summary>
    /// Register async should fail when username is reserved.
    /// </summary>
    [Fact]
    public async Task RegisterAsync_ShouldFail_WhenUsernameIsReserved()
    {
        using var db = CreateDb();
        var sut = CreateSut(db);

        var (ok, error, user) = await sut.RegisterAsync("admin", "password123", false, CancellationToken.None);

        ok.Should().BeFalse();
        error.Should().Be(UsernameValidator.ReservedMessage);
        user.Should().BeNull();
    }

    /// <summary>
    /// Update profile async should fail when username is invalid.
    /// </summary>
    [Fact]
    public async Task UpdateProfileAsync_ShouldFail_WhenUsernameIsInvalid()
    {
        using var db = CreateDb();
        var sut = CreateSut(db);
        var (_, _, user) = await sut.RegisterAsync("profileUser", "pw", false, CancellationToken.None);

        var (ok, error, updated) = await sut.UpdateProfileAsync(user!.Id, "support_team", null, CancellationToken.None);

        ok.Should().BeFalse();
        error.Should().Be(UsernameValidator.GenericBlockedMessage);
        updated.Should().BeNull();
    }

    /// <summary>
    /// Update user async should fail when username is invalid.
    /// </summary>
    [Fact]
    public async Task UpdateUserAsync_ShouldFail_WhenUsernameIsInvalid()
    {
        using var db = CreateDb();
        var sut = CreateSut(db);
        var (_, _, user) = await sut.RegisterAsync("adminUser", "pw", false, CancellationToken.None);

        var (ok, error) = await sut.UpdateUserAsync(user!.Id, "example.com", null, false, CancellationToken.None);

        ok.Should().BeFalse();
        error.Should().Be(UsernameValidator.IpOrDomainMessage);
    }

    /// <summary>
    /// Change password async should change when current password matches.
    /// </summary>
    [Fact]
    public async Task ChangePasswordAsync_ShouldChange_WhenCurrentPasswordMatches()
    {
        using var db = CreateDb();
        var sut = CreateSut(db);
        var (_, _, user) = await sut.RegisterAsync("frank", "oldpass", false, CancellationToken.None);

        var (ok, error) = await sut.ChangePasswordAsync(user!.Id, "oldpass", "newpass", CancellationToken.None);

        ok.Should().BeTrue();
        error.Should().BeNull();
        // verify old password no longer works
        var okUser = await sut.LoginAsync("frank", "newpass", CancellationToken.None);
        okUser.Should().NotBeNull();
        var badUser = await sut.LoginAsync("frank", "oldpass", CancellationToken.None);
        badUser.Should().BeNull();
    }

    /// <summary>
    /// Login async should rehash password when stored hash outdated.
    /// </summary>
    [Fact]
    public async Task LoginAsync_ShouldRehashPassword_WhenStoredHashOutdated()
    {
        using var db = CreateDb();
        var sut = CreateSut(db);
        await sut.RegisterAsync("rehashUser", "secret!", false, CancellationToken.None);

        // Simulate a legacy hash with the minimum accepted iteration count.
        var entity = await db.Users.SingleAsync(u => u.Username == "rehashUser");
        entity.PasswordHash = PasswordHasher.Hash("secret!", PasswordHasher.MinIterations);
        await db.SaveChangesAsync();

        var user = await sut.LoginAsync("rehashUser", "secret!", CancellationToken.None);

        user.Should().NotBeNull();
        var reloaded = await db.Users.AsNoTracking().SingleAsync(u => u.Username == "rehashUser");
        reloaded.PasswordHash.Should().StartWith($"{PasswordHasher.CurrentIterations}.");
    }

    /// <summary>
    /// Login async should not rehash when stored hash current.
    /// </summary>
    [Fact]
    public async Task LoginAsync_ShouldNotRehash_WhenStoredHashCurrent()
    {
        using var db = CreateDb();
        var sut = CreateSut(db);
        await sut.RegisterAsync("currentUser", "secret!", false, CancellationToken.None);
        var originalHash = (await db.Users.AsNoTracking().SingleAsync(u => u.Username == "currentUser")).PasswordHash;

        var user = await sut.LoginAsync("currentUser", "secret!", CancellationToken.None);

        user.Should().NotBeNull();
        var reloaded = await db.Users.AsNoTracking().SingleAsync(u => u.Username == "currentUser");
        reloaded.PasswordHash.Should().Be(originalHash);
    }

    /// <summary>
    /// Login async should return null when stored hash violates policy.
    /// </summary>
    [Fact]
    public async Task LoginAsync_ShouldReturnNull_WhenStoredHashViolatesPolicy()
    {
        using var db = CreateDb();
        var sut = CreateSut(db);
        await sut.RegisterAsync("weakHashUser", "secret!", false, CancellationToken.None);

        var entity = await db.Users.SingleAsync(u => u.Username == "weakHashUser");
        var salt = Convert.ToHexString(new byte[PasswordHasher.SaltLengthBytes]);
        var hash = Convert.ToHexString(new byte[PasswordHasher.HashLengthBytes]);
        entity.PasswordHash = $"{PasswordHasher.MinIterations - 1}.{salt}.{hash}";
        await db.SaveChangesAsync();

        var user = await sut.LoginAsync("weakHashUser", "secret!", CancellationToken.None);

        user.Should().BeNull();
    }

    /// <summary>
    /// Change password async should return error when stored hash malformed.
    /// </summary>
    [Fact]
    public async Task ChangePasswordAsync_ShouldReturnError_WhenStoredHashMalformed()
    {
        using var db = CreateDb();
        var sut = CreateSut(db);
        var (_, _, user) = await sut.RegisterAsync("malformedUser", "oldpass", false, CancellationToken.None);

        var entity = await db.Users.SingleAsync(u => u.Username == "malformedUser");
        entity.PasswordHash = "not-a-valid-hash";
        await db.SaveChangesAsync();

        var (ok, error) = await sut.ChangePasswordAsync(user!.Id, "oldpass", "newpass", CancellationToken.None);

        ok.Should().BeFalse();
        error.Should().NotBeNull();
    }

    /// <summary>
    /// Update user async should set admin flag.
    /// </summary>
    [Fact]
    public async Task UpdateUserAsync_ShouldSetAdminFlag()
    {
        using var db = CreateDb();
        var sut = CreateSut(db);
        var (_, _, user) = await sut.RegisterAsync("gary", "pw", false, CancellationToken.None);

        var (ok, error) = await sut.UpdateUserAsync(user!.Id, user.Username, user.Email, true, CancellationToken.None);

        ok.Should().BeTrue();
        error.Should().BeNull();
        var reloaded = await sut.GetByIdAsync(user.Id, CancellationToken.None);
        reloaded!.IsAdmin.Should().BeTrue();
    }

    /// <summary>
    /// Delete async should remove user.
    /// </summary>
    [Fact]
    public async Task DeleteAsync_ShouldRemoveUser()
    {
        using var db = CreateDb();
        var sut = CreateSut(db);
        var (_, _, user) = await sut.RegisterAsync("henry", "pw", false, CancellationToken.None);

        var (ok, error) = await sut.DeleteAsync(user!.Id, CancellationToken.None);

        ok.Should().BeTrue();
        error.Should().BeNull();
        var any = await sut.HasAnyUsersAsync(CancellationToken.None);
        any.Should().BeFalse();
    }

    /// <summary>
    /// Register async should enqueue demo data job when create demo data is true.
    /// </summary>
    [Fact]
    public async Task RegisterAsync_ShouldEnqueueDemoDataJob_WhenCreateDemoDataIsTrue()
    {
        using var db = CreateDb();
        var queue = new Mock<IBackgroundJobQueue>();
        queue.Setup(q => q.EnqueueAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());
        var sut = new UserService(db, new UsernameValidator(), queue.Object);

        var (_, _, user) = await sut.RegisterAsync("demoUser", "password123", true, CancellationToken.None);

        user.Should().NotBeNull();
        queue.Verify(q => q.EnqueueAsync(
            "seed-demo-data",
            It.Is<string>(payload => payload.Contains(user!.Id, StringComparison.Ordinal)),
            user.Id,
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Register async should not enqueue demo data job when create demo data is false.
    /// </summary>
    [Fact]
    public async Task RegisterAsync_ShouldNotEnqueueDemoDataJob_WhenCreateDemoDataIsFalse()
    {
        using var db = CreateDb();
        var queue = new Mock<IBackgroundJobQueue>();
        var sut = new UserService(db, new UsernameValidator(), queue.Object);

        await sut.RegisterAsync("plainUser", "password123", false, CancellationToken.None);

        queue.Verify(q => q.EnqueueAsync(
            "seed-demo-data",
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
