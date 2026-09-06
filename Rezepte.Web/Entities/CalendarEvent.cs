using System;
using System.ComponentModel.DataAnnotations;

namespace Rezepte.Web.Entities
{
    /// <summary>
    /// Defines the recurrence type values.
    /// </summary>
    public enum RecurrenceType
    {
        /// <summary>
        /// Defines the week days values.
        /// </summary>
        None = 0,
        /// <summary>
        /// Defines the week days values.
        /// </summary>
        Weekly = 1
    }

    /// <summary>
    /// Defines the week days values.
    /// </summary>
    [Flags]
    public enum WeekDays
    {
        /// <summary>
        /// Represents the calendar event class.
        /// </summary>
        None = 0,
        /// <summary>
        /// Represents the calendar event class.
        /// </summary>
        Monday = 1,
        /// <summary>
        /// Represents the calendar event class.
        /// </summary>
        Tuesday = 2,
        /// <summary>
        /// Represents the calendar event class.
        /// </summary>
        Wednesday = 4,
        /// <summary>
        /// Represents the calendar event class.
        /// </summary>
        Thursday = 8,
        /// <summary>
        /// Represents the calendar event class.
        /// </summary>
        Friday = 16,
        /// <summary>
        /// Represents the calendar event class.
        /// </summary>
        Saturday = 32,
        /// <summary>
        /// Represents the calendar event class.
        /// </summary>
        Sunday = 64
    }

    /// <summary>
    /// Represents the calendar event class.
    /// </summary>
    public class CalendarEvent
    {
        /// <summary>
        /// guids the value.
        /// </summary>
        /// <returns>The result.</returns>
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Represents the public class.
        /// </summary>
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

        /// <summary>
        /// Represents the public class.
        /// </summary>
        public RecurrenceType Recurrence { get; set; } = RecurrenceType.None;

        /// <summary>
        /// Bitmask for weekdays when Recurrence == Weekly.
        /// </summary>
        public WeekDays RecurrenceDays { get; set; } = WeekDays.None;

        /// <summary>
        /// Represents the public class.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        /// <summary>
        /// Represents the public class.
        /// </summary>
        public DateTime? ModifiedAt { get; set; }

        // Navigation (optional)
        /// <summary>
        /// Represents the public class.
        /// </summary>
        public Rezepte.Web.Entities.Recipe? Recipe { get; set; }
    }
}
