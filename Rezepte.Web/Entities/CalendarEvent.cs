using System;
using System.ComponentModel.DataAnnotations;

namespace Rezepte.Web.Entities
{
    public enum RecurrenceType
    {
        None = 0,
        Weekly = 1
    }

    [Flags]
    public enum WeekDays
    {
        None = 0,
        Monday = 1,
        Tuesday = 2,
        Wednesday = 4,
        Thursday = 8,
        Friday = 16,
        Saturday = 32,
        Sunday = 64
    }

    public class CalendarEvent
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string UserId { get; set; } = null!;

        /// <summary>
        /// Date of the event (date portion will be used). For recurring events this is the start date.
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// Time of day for the event.
        /// </summary>
        public TimeSpan TimeOfDay { get; set; }

        /// <summary>
        /// Associated recipe (optional).
        /// </summary>
        public string? RecipeId { get; set; }

        /// <summary>
        /// Number of portions (copied from recipe at creation, editable).
        /// </summary>
        public int Portions { get; set; }

        public RecurrenceType Recurrence { get; set; } = RecurrenceType.None;

        /// <summary>
        /// Bitmask for weekdays when Recurrence == Weekly.
        /// </summary>
        public WeekDays RecurrenceDays { get; set; } = WeekDays.None;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ModifiedAt { get; set; }

        // Navigation (optional)
        public Rezepte.Web.Entities.Recipe? Recipe { get; set; }
    }
}