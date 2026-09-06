using Microsoft.EntityFrameworkCore;
using Rezepte.Web.Data;

namespace Rezepte.Web.Extensions;

/// <summary>
/// Represents the web application extensions class.
/// </summary>
public static class WebApplicationExtensions
{
    /// <summary>
    /// Applies the database migrations async.
    /// </summary>
    /// <param name="app">The app parameter.</param>
    public static async Task ApplyDatabaseMigrationsAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RezepteDbContext>();
        var hasMigrations = db.Database.GetMigrations().Any();
        if (hasMigrations)
        {
            await db.Database.MigrateAsync();
        }
        else
        {
            await db.Database.EnsureCreatedAsync();
        }
    }
}
