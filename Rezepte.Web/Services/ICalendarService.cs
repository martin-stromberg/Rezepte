using Rezepte.Web.Entities;
using Rezepte.Web.Services;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Rezepte.Web.Services
{
    public interface ICalendarService
    {
        Task<CalendarEvent?> GetEventAsync(string userId, string eventId, CancellationToken ct);
        Task<IEnumerable<CalendarEvent>> GetEventsForUserAsync(string userId, DateTime from, DateTime to, CancellationToken ct);
        Task<(bool ok, string? error, CalendarEvent? ev)> CreateEventAsync(string userId, string? recipeId, DateTime startDate, TimeSpan timeOfDay, int portions, RecurrenceType recurrence, WeekDays recurrenceDays, CancellationToken ct);
        Task<(bool ok, string? error)> UpdateEventAsync(string userId, string eventId, DateTime startDate, TimeSpan timeOfDay, int portions, RecurrenceType recurrence, WeekDays recurrenceDays, CancellationToken ct);
        Task<(bool ok, string? error)> DeleteEventAsync(string userId, string eventId, CancellationToken ct);

        /// <summary>
        /// Expands recurring events into occurrences between 'from' and 'to' (inclusive).
        /// Returns a tuple (Event, OccurrenceDateTime).
        /// </summary>
        Task<IEnumerable<(CalendarEvent Ev, DateTime Occurrence)>> GetOccurrencesAsync(string userId, DateTime from, DateTime to, CancellationToken ct);
    }
}