using System;

namespace Rezepte.Web.Dtos
{
    public sealed record ScheduledRecipeDto(string Id, string Title, DateTime ScheduledFor, string? Description);
}