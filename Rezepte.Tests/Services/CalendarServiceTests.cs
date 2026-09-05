using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Rezepte.Web.Data;
using Rezepte.Web.Entities;
using Rezepte.Web.Services;
using Xunit;

namespace Rezepte.Tests.Services;

/// <summary>
/// Class representing the calendar service tests.
/// </summary>
public class CalendarServiceTests
{
    private const string UserA = "user-a";
    private const string UserB = "user-b";

    private static RezepteDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<RezepteDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new RezepteDbContext(options);
    }

    private static Mock<IRecipeService> CreateRecipeService(Recipe? recipe = null)
    {
        var mock = new Mock<IRecipeService>();
        mock.Setup(s => s.GetByIdAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(recipe);
        return mock;
    }

    /// <summary>
    /// Constructor should reject missing dependencies.
    /// </summary>
    [Fact]
    public void Constructor_ShouldRejectMissingDependencies()
    {
        using var db = CreateDb();
        var recipes = CreateRecipeService().Object;

        Assert.Throws<ArgumentNullException>(() => new CalendarService(null!, recipes));
        Assert.Throws<ArgumentNullException>(() => new CalendarService(db, null!));
    }

    /// <summary>
    /// Create event async should reject missing user.
    /// </summary>
    [Fact]
    public async Task CreateEventAsync_ShouldRejectMissingUser()
    {
        using var db = CreateDb();
        var sut = new CalendarService(db, CreateRecipeService().Object);

        var (ok, error, ev) = await sut.CreateEventAsync(" ", "recipe-1", DateTime.Today, TimeSpan.FromHours(12), 2, RecurrenceType.None, WeekDays.None, CancellationToken.None);

        ok.Should().BeFalse();
        error.Should().Be("Unauthorized");
        ev.Should().BeNull();
    }

    /// <summary>
    /// Create event async should reject non positive portions.
    /// </summary>
    /// <param name="portions">The portions parameter.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public async Task CreateEventAsync_ShouldRejectNonPositivePortions(int portions)
    {
        using var db = CreateDb();
        var sut = new CalendarService(db, CreateRecipeService().Object);

        var (ok, error, ev) = await sut.CreateEventAsync(UserA, "recipe-1", DateTime.Today, TimeSpan.FromHours(12), portions, RecurrenceType.None, WeekDays.None, CancellationToken.None);

        ok.Should().BeFalse();
        error.Should().Be("Portions must be > 0");
        ev.Should().BeNull();
    }

    /// <summary>
    /// Create event async should reject unknown recipe.
    /// </summary>
    [Fact]
    public async Task CreateEventAsync_ShouldRejectUnknownRecipe()
    {
        using var db = CreateDb();
        var sut = new CalendarService(db, CreateRecipeService().Object);

        var (ok, error, ev) = await sut.CreateEventAsync(UserA, "missing", DateTime.Today, TimeSpan.FromHours(12), 2, RecurrenceType.None, WeekDays.None, CancellationToken.None);

        ok.Should().BeFalse();
        error.Should().Be("Recipe not found");
        ev.Should().BeNull();
    }

    /// <summary>
    /// Create event async should persist event with normalized start date.
    /// </summary>
    [Fact]
    public async Task CreateEventAsync_ShouldPersistEventWithNormalizedStartDate()
    {
        using var db = CreateDb();
        var recipe = new Recipe { Id = "recipe-1", UserId = UserA, Title = "Suppe" };
        var sut = new CalendarService(db, CreateRecipeService(recipe).Object);

        var (ok, error, ev) = await sut.CreateEventAsync(
            UserA,
            "recipe-1",
            new DateTime(2025, 5, 4, 18, 45, 0),
            TimeSpan.FromHours(19),
            4,
            RecurrenceType.Weekly,
            WeekDays.Monday | WeekDays.Friday,
            CancellationToken.None);

        ok.Should().BeTrue();
        error.Should().BeNull();
        ev.Should().NotBeNull();
        ev!.StartDate.Should().Be(new DateTime(2025, 5, 4));
        ev.RecipeId.Should().Be("recipe-1");
        ev.Portions.Should().Be(4);
        ev.RecurrenceDays.Should().Be(WeekDays.Monday | WeekDays.Friday);

        var stored = await sut.GetEventAsync(UserA, ev.Id, CancellationToken.None);
        stored.Should().NotBeNull();
    }

    /// <summary>
    /// Create event async should allow event without recipe.
    /// </summary>
    [Fact]
    public async Task CreateEventAsync_ShouldAllowEventWithoutRecipe()
    {
        using var db = CreateDb();
        var recipes = CreateRecipeService();
        var sut = new CalendarService(db, recipes.Object);

        var (ok, _, ev) = await sut.CreateEventAsync(UserA, "  ", DateTime.Today, TimeSpan.FromHours(8), 1, RecurrenceType.None, WeekDays.None, CancellationToken.None);

        ok.Should().BeTrue();
        ev!.RecipeId.Should().BeNull();
        recipes.Verify(s => s.GetByIdAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Get event async should not return event of other user.
    /// </summary>
    [Fact]
    public async Task GetEventAsync_ShouldNotReturnEventOfOtherUser()
    {
        using var db = CreateDb();
        var sut = new CalendarService(db, CreateRecipeService().Object);
        var (_, _, ev) = await sut.CreateEventAsync(UserA, string.Empty, DateTime.Today, TimeSpan.Zero, 1, RecurrenceType.None, WeekDays.None, CancellationToken.None);

        var result = await sut.GetEventAsync(UserB, ev!.Id, CancellationToken.None);

        result.Should().BeNull();
    }

    /// <summary>
    /// Get events for user async should return only own events starting before end.
    /// </summary>
    [Fact]
    public async Task GetEventsForUserAsync_ShouldReturnOnlyOwnEventsStartingBeforeEnd()
    {
        using var db = CreateDb();
        var sut = new CalendarService(db, CreateRecipeService().Object);
        var start = new DateTime(2025, 5, 1);
        await sut.CreateEventAsync(UserA, string.Empty, start, TimeSpan.Zero, 1, RecurrenceType.None, WeekDays.None, CancellationToken.None);
        await sut.CreateEventAsync(UserA, string.Empty, start.AddMonths(2), TimeSpan.Zero, 1, RecurrenceType.None, WeekDays.None, CancellationToken.None);
        await sut.CreateEventAsync(UserB, string.Empty, start, TimeSpan.Zero, 1, RecurrenceType.None, WeekDays.None, CancellationToken.None);

        var events = await sut.GetEventsForUserAsync(UserA, start, start.AddDays(7), CancellationToken.None);

        events.Should().HaveCount(1);
        events.Single().StartDate.Should().Be(start);
    }

    /// <summary>
    /// Update event async should apply changes.
    /// </summary>
    [Fact]
    public async Task UpdateEventAsync_ShouldApplyChanges()
    {
        using var db = CreateDb();
        var sut = new CalendarService(db, CreateRecipeService().Object);
        var (_, _, ev) = await sut.CreateEventAsync(UserA, string.Empty, new DateTime(2025, 5, 1), TimeSpan.FromHours(9), 1, RecurrenceType.None, WeekDays.None, CancellationToken.None);

        var (ok, error) = await sut.UpdateEventAsync(
            UserA,
            ev!.Id,
            new DateTime(2025, 6, 2, 7, 30, 0),
            TimeSpan.FromHours(11),
            6,
            RecurrenceType.Weekly,
            WeekDays.Sunday,
            CancellationToken.None);

        ok.Should().BeTrue();
        error.Should().BeNull();

        var updated = await sut.GetEventAsync(UserA, ev.Id, CancellationToken.None);
        updated!.StartDate.Should().Be(new DateTime(2025, 6, 2));
        updated.TimeOfDay.Should().Be(TimeSpan.FromHours(11));
        updated.Portions.Should().Be(6);
        updated.Recurrence.Should().Be(RecurrenceType.Weekly);
        updated.RecurrenceDays.Should().Be(WeekDays.Sunday);
        updated.ModifiedAt.Should().NotBeNull();
    }

    /// <summary>
    /// Update event async should fail for foreign event.
    /// </summary>
    [Fact]
    public async Task UpdateEventAsync_ShouldFailForForeignEvent()
    {
        using var db = CreateDb();
        var sut = new CalendarService(db, CreateRecipeService().Object);
        var (_, _, ev) = await sut.CreateEventAsync(UserA, string.Empty, DateTime.Today, TimeSpan.Zero, 1, RecurrenceType.None, WeekDays.None, CancellationToken.None);

        var (ok, error) = await sut.UpdateEventAsync(UserB, ev!.Id, DateTime.Today, TimeSpan.Zero, 2, RecurrenceType.None, WeekDays.None, CancellationToken.None);

        ok.Should().BeFalse();
        error.Should().Be("Event not found");
    }

    /// <summary>
    /// Delete event async should remove own event only.
    /// </summary>
    [Fact]
    public async Task DeleteEventAsync_ShouldRemoveOwnEventOnly()
    {
        using var db = CreateDb();
        var sut = new CalendarService(db, CreateRecipeService().Object);
        var (_, _, ev) = await sut.CreateEventAsync(UserA, string.Empty, DateTime.Today, TimeSpan.Zero, 1, RecurrenceType.None, WeekDays.None, CancellationToken.None);

        var foreign = await sut.DeleteEventAsync(UserB, ev!.Id, CancellationToken.None);
        foreign.ok.Should().BeFalse();
        foreign.error.Should().Be("Event not found");

        var own = await sut.DeleteEventAsync(UserA, ev.Id, CancellationToken.None);
        own.ok.Should().BeTrue();
        own.error.Should().BeNull();
        (await sut.GetEventAsync(UserA, ev.Id, CancellationToken.None)).Should().BeNull();
    }

    /// <summary>
    /// Get occurrences async should return single occurrence inside range.
    /// </summary>
    [Fact]
    public async Task GetOccurrencesAsync_ShouldReturnSingleOccurrenceInsideRange()
    {
        using var db = CreateDb();
        var sut = new CalendarService(db, CreateRecipeService().Object);
        var day = new DateTime(2025, 5, 7);
        await sut.CreateEventAsync(UserA, string.Empty, day, TimeSpan.FromHours(18), 1, RecurrenceType.None, WeekDays.None, CancellationToken.None);

        var inRange = await sut.GetOccurrencesAsync(UserA, day, day.AddDays(1), CancellationToken.None);
        var beforeRange = await sut.GetOccurrencesAsync(UserA, day.AddDays(1), day.AddDays(2), CancellationToken.None);

        inRange.Should().HaveCount(1);
        inRange.Single().Occurrence.Should().Be(day.AddHours(18));
        beforeRange.Should().BeEmpty();
    }

    /// <summary>
    /// Get occurrences async should expand weekly recurrence ordered by date.
    /// </summary>
    [Fact]
    public async Task GetOccurrencesAsync_ShouldExpandWeeklyRecurrenceOrderedByDate()
    {
        using var db = CreateDb();
        var sut = new CalendarService(db, CreateRecipeService().Object);
        // 2025-05-05 is a Monday.
        var start = new DateTime(2025, 5, 5);
        await sut.CreateEventAsync(UserA, string.Empty, start, TimeSpan.FromHours(12), 2, RecurrenceType.Weekly, WeekDays.Monday | WeekDays.Wednesday, CancellationToken.None);

        var occurrences = (await sut.GetOccurrencesAsync(UserA, start, start.AddDays(10), CancellationToken.None)).ToList();

        occurrences.Select(o => o.Occurrence).Should().BeInAscendingOrder();
        // The start date is emitted as base occurrence and again as weekly hit, followed by every matching weekday.
        occurrences.Select(o => o.Occurrence.Date).Should().Equal(
            new DateTime(2025, 5, 5),
            new DateTime(2025, 5, 5),
            new DateTime(2025, 5, 7),
            new DateTime(2025, 5, 12),
            new DateTime(2025, 5, 14));
        occurrences.Should().OnlyContain(o => o.Occurrence.TimeOfDay == TimeSpan.FromHours(12));
    }

    /// <summary>
    /// Get occurrences async should expand weekly recurrence on tuesday and saturday.
    /// </summary>
    [Fact]
    public async Task GetOccurrencesAsync_ShouldExpandWeeklyRecurrenceOnTuesdayAndSaturday()
    {
        using var db = CreateDb();
        var sut = new CalendarService(db, CreateRecipeService().Object);
        // 2025-05-05 is a Monday.
        var start = new DateTime(2025, 5, 5);
        var (_, _, ev) = await sut.CreateEventAsync(UserA, string.Empty, start, TimeSpan.FromHours(12), 2, RecurrenceType.Weekly, WeekDays.Tuesday | WeekDays.Saturday, CancellationToken.None);

        ev!.RecurrenceDays.Should().Be(WeekDays.Tuesday | WeekDays.Saturday);
        ev.RecurrenceDays.HasFlag(WeekDays.Tuesday).Should().BeTrue();
        ev.RecurrenceDays.HasFlag(WeekDays.Saturday).Should().BeTrue();

        var occurrences = (await sut.GetOccurrencesAsync(UserA, start, start.AddDays(10), CancellationToken.None)).ToList();

        var dates = occurrences.Select(o => o.Occurrence.Date).ToList();
        dates.Should().Contain(new DateTime(2025, 5, 6));
        dates.Should().Contain(new DateTime(2025, 5, 10));
        dates.Should().Contain(new DateTime(2025, 5, 13));
        dates.Should().NotContain(new DateTime(2025, 5, 8));
    }

    /// <summary>
    /// Get occurrences async should ignore weekly recurrence without days.
    /// </summary>
    [Fact]
    public async Task GetOccurrencesAsync_ShouldIgnoreWeeklyRecurrenceWithoutDays()
    {
        using var db = CreateDb();
        var sut = new CalendarService(db, CreateRecipeService().Object);
        var start = new DateTime(2025, 5, 5);
        await sut.CreateEventAsync(UserA, string.Empty, start, TimeSpan.FromHours(12), 2, RecurrenceType.Weekly, WeekDays.None, CancellationToken.None);

        var occurrences = await sut.GetOccurrencesAsync(UserA, start, start.AddDays(20), CancellationToken.None);

        occurrences.Should().HaveCount(1);
    }
}
