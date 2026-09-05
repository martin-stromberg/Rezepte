using Rezepte.Web.Entities;
using Rezepte.Web.Services;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Rezepte.Web.Services
{
    /// <summary>
    /// Defines the icalendar service interface.
    /// </summary>
    public interface ICalendarService
    {
        /// <summary>
        /// Gets the event async.
        /// </summary>
        /// <param name="userId">The user id parameter.</param>
        /// <param name="eventId">The event id parameter.</param>
        /// <param name="ct">The ct parameter.</param>
        /// <returns>The result.</returns>
        Task<CalendarEvent?> GetEventAsync(string userId, string eventId, CancellationToken ct);
        /// <summary>
        /// Gets the events for user async.
        /// </summary>
        /// <param name="userId">The user id parameter.</param>
        /// <param name="from">The from parameter.</param>
        /// <param name="to">The to parameter.</param>
        /// <param name="ct">The ct parameter.</param>
        /// <returns>The result.</returns>
        Task<IEnumerable<CalendarEvent>> GetEventsForUserAsync(string userId, DateTime from, DateTime to, CancellationToken ct);
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
        Task<(bool ok, string? error, CalendarEvent? ev)> CreateEventAsync(string userId, string? recipeId, DateTime startDate, TimeSpan timeOfDay, int portions, RecurrenceType recurrence, WeekDays recurrenceDays, CancellationToken ct);
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
        Task<(bool ok, string? error)> UpdateEventAsync(string userId, string eventId, DateTime startDate, TimeSpan timeOfDay, int portions, RecurrenceType recurrence, WeekDays recurrenceDays, CancellationToken ct);
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
        Task<(bool ok, string? error)> DeleteEventAsync(string userId, string eventId, CancellationToken ct);

        /// <summary>
        /// Expands recurring events into occurrences between 'from' and 'to' (inclusive).
        /// Returns a tuple (Event, OccurrenceDateTime).
        /// </summary>
        /// <param name="userId">The user id parameter.</param>
        /// <param name="from">The from parameter.</param>
        /// <param name="to">The to parameter.</param>
        /// <param name="ct">The ct parameter.</param>
        /// <param>...</param>
        /// <param>...</param>
        /// <param>...</param>
        /// <returns>The result.</returns>
        Task<IEnumerable<(CalendarEvent Ev, DateTime Occurrence)>> GetOccurrencesAsync(string userId, DateTime from, DateTime to, CancellationToken ct);
    }
}
