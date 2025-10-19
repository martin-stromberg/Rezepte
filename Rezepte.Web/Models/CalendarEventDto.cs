namespace Rezepte.Web.Models;

public sealed record CalendarEventDto(
    string? EventId,
    string? RecipeId,
    DateTime StartDate,
    TimeSpan TimeOfDay,
    int Portions,
    Rezepte.Web.Entities.RecurrenceType Recurrence,
    Rezepte.Web.Entities.WeekDays RecurrenceDays
);