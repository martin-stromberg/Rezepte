using System;

namespace Rezepte.Web.Dtos
{
    /// <summary>
    /// scheduleds the recipe dto.
    /// </summary>
    /// <param name="Id">The id parameter.</param>
    /// <param name="Title">The title parameter.</param>
    /// <param name="ScheduledFor">The scheduled for parameter.</param>
    /// <param name="Description">The description parameter.</param>
    /// <returns>The result.</returns>
    public sealed record ScheduledRecipeDto(string Id, string Title, DateTime ScheduledFor, string? Description);
}
