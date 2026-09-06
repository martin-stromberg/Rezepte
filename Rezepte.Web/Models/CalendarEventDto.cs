namespace Rezepte.Web.Models;

/// <summary>
/// calendars the event dto.
/// </summary>
/// <param name="EventId">The event id parameter.</param>
/// <param name="RecipeId">The recipe id parameter.</param>
/// <param name="StartDate">The start date parameter.</param>
/// <param name="TimeOfDay">The time of day parameter.</param>
/// <param name="Portions">The portions parameter.</param>
/// <param name="Recurrence">The recurrence parameter.</param>
/// <param name="RecurrenceDays">The recurrence days parameter.</param>
/// <returns>The result.</returns>
public sealed record CalendarEventDto(
    string? EventId,
    string? RecipeId,
    DateTime StartDate,
    TimeSpan TimeOfDay,
    int Portions,
    Rezepte.Web.Entities.RecurrenceType Recurrence,
    Rezepte.Web.Entities.WeekDays RecurrenceDays
);
