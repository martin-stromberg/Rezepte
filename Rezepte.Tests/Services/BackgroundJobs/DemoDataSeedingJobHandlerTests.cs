using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Rezepte.Web.Services;
using Rezepte.Web.Services.BackgroundJobs;
using Rezepte.Web.Services.BackgroundJobs.Handlers;
using Xunit;

namespace Rezepte.Tests.Services.BackgroundJobs;

/// <summary>
/// Class representing the demo data seeding job handler tests.
/// </summary>
public class DemoDataSeedingJobHandlerTests
{
    private static IServiceProvider BuildProvider(
        Mock<IUserService>? userService = null,
        Mock<IDemoDataSeeder>? demoDataSeeder = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton(userService?.Object ?? new Mock<IUserService>().Object);
        services.AddSingleton(demoDataSeeder?.Object ?? new Mock<IDemoDataSeeder>().Object);
        return services.BuildServiceProvider();
    }

    private static BackgroundJob CreateJob(string userId)
    {
        return new BackgroundJob
        {
            PayloadJson = new DemoDataSeedPayload(userId).ToJson()
        };
    }

    /// <summary>
    /// Handle async should call seed for user async with the payload user id.
    /// </summary>
    [Fact]
    public async Task HandleAsync_WithValidJob_CallsSeedForUserAsync()
    {
        const string userId = "user1";
        var userService = new Mock<IUserService>();
        userService.Setup(s => s.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Rezepte.Web.Services.User(userId, "test", string.Empty, string.Empty, false, DateTime.UtcNow));
        var demoDataSeeder = new Mock<IDemoDataSeeder>();

        var provider = BuildProvider(userService, demoDataSeeder);
        var handler = new DemoDataSeedingJobHandler();

        await handler.HandleAsync(CreateJob(userId), provider, CancellationToken.None);

        demoDataSeeder.Verify(s => s.SeedForUserAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Handle async should throw when user does not exist and not call the seeder.
    /// </summary>
    [Fact]
    public async Task HandleAsync_WhenUserDoesNotExist_ThrowsInvalidOperationException()
    {
        const string userId = "user2";
        var userService = new Mock<IUserService>();
        userService.Setup(s => s.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync((Rezepte.Web.Services.User?)null);
        var demoDataSeeder = new Mock<IDemoDataSeeder>();

        var provider = BuildProvider(userService, demoDataSeeder);
        var handler = new DemoDataSeedingJobHandler();

        var action = () => handler.HandleAsync(CreateJob(userId), provider, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
        demoDataSeeder.Verify(s => s.SeedForUserAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Handle async should throw when the payload has no user id and not call the seeder.
    /// </summary>
    [Fact]
    public async Task HandleAsync_WhenUserIdEmpty_ThrowsInvalidOperationException()
    {
        var demoDataSeeder = new Mock<IDemoDataSeeder>();
        var provider = BuildProvider(demoDataSeeder: demoDataSeeder);
        var handler = new DemoDataSeedingJobHandler();

        var action = () => handler.HandleAsync(CreateJob(string.Empty), provider, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
        demoDataSeeder.Verify(s => s.SeedForUserAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Handle async should propagate the exception thrown by the seeder.
    /// </summary>
    [Fact]
    public async Task HandleAsync_WhenSeederThrows_PropagatesException()
    {
        const string userId = "user3";
        var userService = new Mock<IUserService>();
        userService.Setup(s => s.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Rezepte.Web.Services.User(userId, "test", string.Empty, string.Empty, false, DateTime.UtcNow));
        var demoDataSeeder = new Mock<IDemoDataSeeder>();
        demoDataSeeder.Setup(s => s.SeedForUserAsync(userId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Seeding failed."));

        var provider = BuildProvider(userService, demoDataSeeder);
        var handler = new DemoDataSeedingJobHandler();

        var action = () => handler.HandleAsync(CreateJob(userId), provider, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("Seeding failed.");
    }
}
