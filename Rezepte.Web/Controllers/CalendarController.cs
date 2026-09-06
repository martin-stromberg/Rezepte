using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rezepte.Web.Entities;
using Rezepte.Web.Services;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Rezepte.Web.Controllers
{
    /// <summary>
    /// Represents the calendar controller class.
    /// </summary>
    [ApiController]
    [Route("api/calendar")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public sealed class CalendarController : ApiControllerBase
    {
        private readonly ICalendarService _calendar;
        /// <summary>
        /// Initializes a new instance of the <see cref="CalendarController"/> class.
        /// </summary>
        /// <param name="calendar">The calendar parameter.</param>
        public CalendarController(ICalendarService calendar) => _calendar = calendar;

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <param name="from">The from parameter.</param>
        /// <param name="to">The to parameter.</param>
        /// <param name="ct">The ct parameter.</param>
        /// <returns>The result.</returns>
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
        {
            var f = from ?? DateTime.UtcNow.Date.AddDays(-7);
            var t = to ?? DateTime.UtcNow.Date.AddDays(30);
            var occ = await _calendar.GetOccurrencesAsync(CurrentUserId, f, t, ct);
            var result = occ.Select(o => new
            {
                id = o.Ev.Id,
                recipeId = o.Ev.RecipeId,
                occurrence = o.Occurrence,
                portions = o.Ev.Portions,
                recurrence = o.Ev.Recurrence,
                recurrenceDays = o.Ev.RecurrenceDays
            });
            return Ok(new { items = result });
        }

        /// <summary>
        /// Creates the value.
        /// </summary>
        /// <param name="dto">The dto parameter.</param>
        /// <param name="ct">The ct parameter.</param>
        /// <returns>The result.</returns>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCalendarEventDto dto, CancellationToken ct)
        {
            if (dto == null) return BadRequest();
            var (ok, error, ev) = await _calendar.CreateEventAsync(CurrentUserId, dto.RecipeId, dto.StartDate.Date, dto.TimeOfDay, dto.Portions, dto.Recurrence, dto.RecurrenceDays, ct);
            if (!ok) return BadRequest(new { error });
            return Ok(ev);
        }

        /// <summary>
        /// Updates the value.
        /// </summary>
        /// <param name="id">The id parameter.</param>
        /// <param name="dto">The dto parameter.</param>
        /// <param name="ct">The ct parameter.</param>
        /// <returns>The result.</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateCalendarEventDto dto, CancellationToken ct)
        {
            if (dto == null) return BadRequest();
            var (ok, error) = await _calendar.UpdateEventAsync(CurrentUserId, id, dto.StartDate.Date, dto.TimeOfDay, dto.Portions, dto.Recurrence, dto.RecurrenceDays, ct);
            if (!ok) return BadRequest(new { error });
            return NoContent();
        }

        /// <summary>
        /// Deletes the value.
        /// </summary>
        /// <param name="id">The id parameter.</param>
        /// <param name="ct">The ct parameter.</param>
        /// <returns>The result.</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id, CancellationToken ct)
        {
            var (ok, error) = await _calendar.DeleteEventAsync(CurrentUserId, id, ct);
            if (!ok) return BadRequest(new { error });
            return NoContent();
        }

        /// <summary>
        /// Creates the calendar event dto.
        /// </summary>
        /// <param name="RecipeId">The recipe id parameter.</param>
        /// <param name="StartDate">The start date parameter.</param>
        /// <param name="TimeOfDay">The time of day parameter.</param>
        /// <param name="Portions">The portions parameter.</param>
        /// <param name="Recurrence">The recurrence parameter.</param>
        /// <param name="RecurrenceDays">The recurrence days parameter.</param>
        /// <returns>The result.</returns>
        public sealed record CreateCalendarEventDto(string? RecipeId, DateTime StartDate, TimeSpan TimeOfDay, int Portions, RecurrenceType Recurrence, WeekDays RecurrenceDays);
        /// <summary>
        /// Updates the calendar event dto.
        /// </summary>
        /// <param name="StartDate">The start date parameter.</param>
        /// <param name="TimeOfDay">The time of day parameter.</param>
        /// <param name="Portions">The portions parameter.</param>
        /// <param name="Recurrence">The recurrence parameter.</param>
        /// <param name="RecurrenceDays">The recurrence days parameter.</param>
        /// <returns>The result.</returns>
        public sealed record UpdateCalendarEventDto(DateTime StartDate, TimeSpan TimeOfDay, int Portions, RecurrenceType Recurrence, WeekDays RecurrenceDays);
    }
}
