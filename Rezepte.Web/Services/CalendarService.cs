using Microsoft.EntityFrameworkCore;
using Rezepte.Web.Data;
using Rezepte.Web.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Rezepte.Web.Services
{
    /// <summary>
    /// Represents the calendar service class.
    /// </summary>
    public sealed class CalendarService : ICalendarService
    {
        private readonly RezepteDbContext _db;
        private readonly IRecipeService _recipeService;

        /// <summary>
        /// Initializes a new instance of the <see cref="CalendarService"/> class.
        /// </summary>
        /// <param name="db">The db parameter.</param>
        /// <param name="recipeService">The recipe service parameter.</param>
        public CalendarService(RezepteDbContext db, IRecipeService recipeService)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _recipeService = recipeService ?? throw new ArgumentNullException(nameof(recipeService));
        }

        /// <summary>
        /// Gets the event async.
        /// </summary>
        /// <param name="userId">The user id parameter.</param>
        /// <param name="eventId">The event id parameter.</param>
        /// <param name="ct">The ct parameter.</param>
        /// <returns>The result.</returns>
        public async Task<CalendarEvent?> GetEventAsync(string userId, string eventId, CancellationToken ct)
        {
            return await _db.Set<CalendarEvent>().AsNoTracking().FirstOrDefaultAsync(e => e.Id == eventId && e.UserId == userId, ct);
        }

        /// <summary>
        /// Gets the events for user async.
        /// </summary>
        /// <param name="userId">The user id parameter.</param>
        /// <param name="from">The from parameter.</param>
        /// <param name="to">The to parameter.</param>
        /// <param name="ct">The ct parameter.</param>
        /// <returns>The result.</returns>
        public async Task<IEnumerable<CalendarEvent>> GetEventsForUserAsync(string userId, DateTime from, DateTime to, CancellationToken ct)
        {
            // return stored events that either have a start in range or are recurring (caller may expand)
            return await _db.Set<CalendarEvent>()
                .AsNoTracking()
                .Where(e => e.UserId == userId && (e.StartDate <= to))
                .ToListAsync(ct);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Task"/> class.
        /// </summary>
        /// <param name="userId">The user id parameter.</param>
        /// <param name="recipeId">The recipe id parameter.</param>
        /// <param name="startDate">The start date parameter.</param>
        /// <param name="timeOfDay">The time of day parameter.</param>
        /// <param name="portions">The portions parameter.</param>
        /// <param name="recurrence">The recurrence parameter.</param>
        /// <param name="recurrenceDays">The recurrence days parameter.</param>
        /// <param name="ct">The ct parameter.</param>
        /// <param>...</param>
        /// <param>...</param>
        /// <param>...</param>
        /// <returns>The result.</returns>
        /// <param name="ok">The ok parameter.</param>
        /// <param name="error">The error parameter.</param>
        /// <param name="ev">The ev parameter.</param>
        public async Task<(bool ok, string? error, CalendarEvent? ev)> CreateEventAsync(string userId, string? recipeId, DateTime startDate, TimeSpan timeOfDay, int portions, RecurrenceType recurrence, WeekDays recurrenceDays, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(userId)) return (false, "Unauthorized", null);
            if (portions <= 0) return (false, "Portions must be > 0", null);

            // If recipeId provided, try to get recipe and default portions if not provided
            if (!string.IsNullOrWhiteSpace(recipeId))
            {
                var recipe = await _recipeService.GetByIdAsync(userId, recipeId, ct);
                if (recipe == null) return (false, "Recipe not found", null);
                // if portions was not set by caller (0), copy recipe.Portions (but we require >0 above)
            }

            var ev = new CalendarEvent
            {
                UserId = userId,
                RecipeId = string.IsNullOrWhiteSpace(recipeId) ? null : recipeId,
                StartDate = startDate.Date,
                TimeOfDay = timeOfDay,
                Portions = portions,
                Recurrence = recurrence,
                RecurrenceDays = recurrenceDays,
                CreatedAt = DateTime.UtcNow
            };

            _db.Add(ev);
            await _db.SaveChangesAsync(ct);
            return (true, null, ev);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Task"/> class.
        /// </summary>
        /// <param name="userId">The user id parameter.</param>
        /// <param name="eventId">The event id parameter.</param>
        /// <param name="startDate">The start date parameter.</param>
        /// <param name="timeOfDay">The time of day parameter.</param>
        /// <param name="portions">The portions parameter.</param>
        /// <param name="recurrence">The recurrence parameter.</param>
        /// <param name="recurrenceDays">The recurrence days parameter.</param>
        /// <param name="ct">The ct parameter.</param>
        /// <param>...</param>
        /// <param>...</param>
        /// <param>...</param>
        /// <returns>The result.</returns>
        /// <param name="ok">The ok parameter.</param>
        /// <param name="error">The error parameter.</param>
        public async Task<(bool ok, string? error)> UpdateEventAsync(string userId, string eventId, DateTime startDate, TimeSpan timeOfDay, int portions, RecurrenceType recurrence, WeekDays recurrenceDays, CancellationToken ct)
        {
            var ev = await _db.Set<CalendarEvent>().FirstOrDefaultAsync(e => e.Id == eventId && e.UserId == userId, ct);
            if (ev == null) return (false, "Event not found");
            ev.StartDate = startDate.Date;
            ev.TimeOfDay = timeOfDay;
            ev.Portions = portions;
            ev.Recurrence = recurrence;
            ev.RecurrenceDays = recurrenceDays;
            ev.ModifiedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return (true, null);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Task"/> class.
        /// </summary>
        /// <param name="userId">The user id parameter.</param>
        /// <param name="eventId">The event id parameter.</param>
        /// <param name="ct">The ct parameter.</param>
        /// <param>...</param>
        /// <param>...</param>
        /// <param>...</param>
        /// <returns>The result.</returns>
        /// <param name="ok">The ok parameter.</param>
        /// <param name="error">The error parameter.</param>
        public async Task<(bool ok, string? error)> DeleteEventAsync(string userId, string eventId, CancellationToken ct)
        {
            var ev = await _db.Set<CalendarEvent>().FirstOrDefaultAsync(e => e.Id == eventId && e.UserId == userId, ct);
            if (ev == null) return (false, "Event not found");
            _db.Remove(ev);
            await _db.SaveChangesAsync(ct);
            return (true, null);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Task"/> class.
        /// </summary>
        /// <param name="userId">The user id parameter.</param>
        /// <param name="from">The from parameter.</param>
        /// <param name="to">The to parameter.</param>
        /// <param name="ct">The ct parameter.</param>
        /// <param>...</param>
        /// <param>...</param>
        /// <param>...</param>
        /// <returns>The result.</returns>
        /// <param name="Ev">The ev parameter.</param>
        /// <param name="Occurrence">The occurrence parameter.</param>
        public async Task<IEnumerable<(CalendarEvent Ev, DateTime Occurrence)>> GetOccurrencesAsync(string userId, DateTime from, DateTime to, CancellationToken ct)
        {
            var events = await GetEventsForUserAsync(userId, from, to, ct);
            var occurrences = new List<(CalendarEvent Ev, DateTime Occurrence)>();

            foreach (var ev in events)
            {
                // Single occurrence: if StartDate between range
                var baseDateTime = ev.StartDate.Date + ev.TimeOfDay;
                if (baseDateTime >= from && baseDateTime <= to)
                {
                    occurrences.Add((ev, baseDateTime));
                }

                if (ev.Recurrence == RecurrenceType.Weekly && ev.RecurrenceDays != WeekDays.None)
                {
                    // expand weekly occurrences between from..to
                    var cursor = ev.StartDate.Date;
                    // start from the later of ev.StartDate or from.Date
                    var start = ev.StartDate.Date > from.Date ? ev.StartDate.Date : from.Date;
                    for (var d = start; d <= to.Date; d = d.AddDays(1))
                    {
                        var weekdayFlag = DayToWeekDays(d.DayOfWeek);
                        if (ev.RecurrenceDays.HasFlag(weekdayFlag))
                        {
                            var occ = d + ev.TimeOfDay;
                            if (occ >= from && occ <= to)
                            {
                                occurrences.Add((ev, occ));
                            }
                        }
                    }
                }
            }

            // order by occurrence datetime
            return occurrences.OrderBy(o => o.Occurrence).ToList();
        }

        private static WeekDays DayToWeekDays(DayOfWeek d) => d switch
        {
            DayOfWeek.Monday => WeekDays.Monday,
            DayOfWeek.Tuesday => WeekDays.Tuesday,
            DayOfWeek.Wednesday => WeekDays.Wednesday,
            DayOfWeek.Thursday => WeekDays.Thursday,
            DayOfWeek.Friday => WeekDays.Friday,
            DayOfWeek.Saturday => WeekDays.Saturday,
            DayOfWeek.Sunday => WeekDays.Sunday,
            _ => WeekDays.None
        };
    }
}
