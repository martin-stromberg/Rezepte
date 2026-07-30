using System.IO.Compression;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Rezepte.Web.Data;
using Rezepte.Web.Entities;
using Rezepte.Web.Services;
using Rezepte.Web.Services.BackgroundJobs;
using Xunit;

namespace Rezepte.Tests.Services;

public sealed class ExportServiceSystemBackupTests
{
    [Fact]
    public async Task ExportAllAsync_ShouldIncludeSystemBackupDataAndCompleteRecipeFields()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<RezepteDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new RezepteDbContext(options);
        await db.Database.EnsureCreatedAsync();
        SeedSystemBackupData(db);
        await db.SaveChangesAsync();

        var sut = new ExportService(db, NullLogger<ExportService>.Instance);

        await using var zip = await sut.ExportAllAsync("system-update-backup", includeImages: false, includePdf: false);
        var root = ReadExportRoot(zip);

        root.Recipes.Should().ContainSingle(r => r.Id == "recipe-main")
            .Which.Should().Match<ExportRecipeDto>(r =>
                r.Uri == "https://example.invalid/recipe" &&
                r.Portions == 4 &&
                r.SideDishes != null &&
                r.SideDishes.Single().SideDishRecipeId == "recipe-side");
        root.SystemData.Should().NotBeNull();
        root.SystemData!.CalendarEvents.Should().ContainSingle(e => e.Id == "calendar-1");
        root.SystemData.ShoppingListGroups.Should().ContainSingle(g => g.Id == "shopping-group-1");
        root.SystemData.ShoppingListItems.Should().ContainSingle(i => i.Id == "shopping-item-1");
        root.SystemData.UserSettings.Should().ContainSingle(s => s.UserId == "user-1");
        root.SystemData.AppSettings.Should().ContainSingle(s => s.Key == "AiEnabled");
        root.SystemData.PluginSettings.Should().ContainSingle(p => p.PluginId == "plugin-1");
        root.SystemData.PluginSources.Should().ContainSingle(p => p.Id == "source-1");
        root.SystemData.PluginSourceReleases.Should().ContainSingle(r => r.Id == "release-1");
        root.SystemData.AiRequestLogs.Should().ContainSingle(l => l.Id == "ai-log-1");
        root.SystemData.BackgroundJobs.Should().ContainSingle(j => j.Id == Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    }

    [Fact]
    public async Task RestoreFromZipAsync_ShouldRoundtripSystemBackupDataAndCompleteRecipeFields()
    {
        await using var sourceConnection = new SqliteConnection("DataSource=:memory:");
        await sourceConnection.OpenAsync();
        var sourceOptions = new DbContextOptionsBuilder<RezepteDbContext>()
            .UseSqlite(sourceConnection)
            .Options;

        await using var sourceDb = new RezepteDbContext(sourceOptions);
        await sourceDb.Database.EnsureCreatedAsync();
        SeedSystemBackupData(sourceDb);
        await sourceDb.SaveChangesAsync();

        var exportService = new ExportService(sourceDb, NullLogger<ExportService>.Instance);
        await using var zip = await exportService.ExportAllAsync("system-update-backup", includeImages: false, includePdf: false);

        await using var targetConnection = new SqliteConnection("DataSource=:memory:");
        await targetConnection.OpenAsync();
        var targetOptions = new DbContextOptionsBuilder<RezepteDbContext>()
            .UseSqlite(targetConnection)
            .Options;

        await using var targetDb = new RezepteDbContext(targetOptions);
        await targetDb.Database.EnsureCreatedAsync();
        targetDb.Users.Add(new Rezepte.Web.Entities.User
        {
            Id = "target-admin",
            Username = "target-admin",
            Email = "target-admin@example.invalid",
            PasswordHash = "hash",
            IsAdmin = true
        });
        await targetDb.SaveChangesAsync();

        var restoreService = new ExportService(targetDb, NullLogger<ExportService>.Instance);
        await restoreService.RestoreFromZipAsync(zip, "target-admin");
        zip.Seek(0, SeekOrigin.Begin);
        await restoreService.RestoreFromZipAsync(zip, "target-admin");

        var recipe = await targetDb.Recipes
            .AsNoTracking()
            .Include(r => r.SideDishes)
            .SingleAsync(r => r.Id == "recipe-main");
        recipe.Uri.Should().Be("https://example.invalid/recipe");
        recipe.Portions.Should().Be(4);
        recipe.SideDishes.Should().ContainSingle(sd =>
            sd.Id == "side-dish-1" &&
            sd.SideDishRecipeId == "recipe-side" &&
            sd.OrderIndex == 1);

        var calendarEvent = await targetDb.CalendarEvents.AsNoTracking().SingleAsync(e => e.Id == "calendar-1");
        calendarEvent.UserId.Should().Be("user-1");
        calendarEvent.RecipeId.Should().Be("recipe-main");
        calendarEvent.Portions.Should().Be(4);
        calendarEvent.Recurrence.Should().Be(RecurrenceType.Weekly);
        calendarEvent.RecurrenceDays.Should().Be(WeekDays.Thursday);

        var shoppingGroup = await targetDb.ShoppingListGroups.AsNoTracking().SingleAsync(g => g.Id == "shopping-group-1");
        shoppingGroup.UserId.Should().Be("user-1");
        shoppingGroup.RecipeId.Should().Be("recipe-main");
        shoppingGroup.Name.Should().Be("Groceries");

        var shoppingItem = await targetDb.ShoppingListItems.AsNoTracking().SingleAsync(i => i.Id == "shopping-item-1");
        shoppingItem.GroupId.Should().Be("shopping-group-1");
        shoppingItem.Name.Should().Be("Tomato");
        shoppingItem.IsChecked.Should().BeTrue();

        (await targetDb.UserSettings.AsNoTracking().CountAsync()).Should().Be(1);
        (await targetDb.AppSettings.AsNoTracking().CountAsync()).Should().Be(1);
        (await targetDb.PluginSettings.AsNoTracking().CountAsync()).Should().Be(1);
        (await targetDb.PluginSources.AsNoTracking().CountAsync()).Should().Be(1);
        (await targetDb.PluginSourceReleases.AsNoTracking().CountAsync()).Should().Be(1);
        (await targetDb.AiRequestLogs.AsNoTracking().CountAsync()).Should().Be(1);
        (await targetDb.BackgroundJobs.AsNoTracking().CountAsync()).Should().Be(1);
    }

    private static void SeedSystemBackupData(RezepteDbContext db)
    {
        db.Users.Add(new Rezepte.Web.Entities.User
        {
            Id = "user-1",
            Username = "admin",
            Email = "admin@example.invalid",
            PasswordHash = "hash",
            IsAdmin = true
        });
        db.Cookbooks.Add(new Cookbook
        {
            Id = "cookbook-1",
            UserId = "user-1",
            Name = "Cookbook"
        });

        var recipe = new Recipe
        {
            Id = "recipe-main",
            UserId = "user-1",
            Title = "Recipe",
            Portions = 4
        };
        typeof(Recipe).GetProperty(nameof(Recipe.Uri))!.SetValue(recipe, "https://example.invalid/recipe");
        db.Recipes.Add(recipe);
        db.Recipes.Add(new Recipe
        {
            Id = "recipe-side",
            UserId = "user-1",
            Title = "Side"
        });
        db.RecipeSideDishes.Add(new RecipeSideDish
        {
            Id = "side-dish-1",
            RecipeId = "recipe-main",
            SideDishRecipeId = "recipe-side",
            OrderIndex = 1
        });
        db.CalendarEvents.Add(new CalendarEvent
        {
            Id = "calendar-1",
            UserId = "user-1",
            RecipeId = "recipe-main",
            StartDate = new DateTime(2026, 7, 30),
            TimeOfDay = TimeSpan.FromHours(12),
            Portions = 4,
            Recurrence = RecurrenceType.Weekly,
            RecurrenceDays = WeekDays.Thursday
        });
        db.ShoppingListGroups.Add(new ShoppingListGroup
        {
            Id = "shopping-group-1",
            UserId = "user-1",
            Name = "Groceries",
            RecipeId = "recipe-main"
        });
        db.ShoppingListItems.Add(new ShoppingListItem
        {
            Id = "shopping-item-1",
            GroupId = "shopping-group-1",
            Amount = 2,
            Unit = "pcs",
            Name = "Tomato",
            IsChecked = true
        });
        db.UserSettings.Add(new UserSetting
        {
            UserId = "user-1",
            AiEnabled = false,
            GoogleVisionEnabled = true,
            GeminiEnabled = false,
            RequireAiConfirmation = true
        });
        db.AppSettings.Add(new AppSetting
        {
            Key = "AiEnabled",
            Value = "true"
        });
        db.PluginSettings.Add(new PluginSetting
        {
            PluginId = "plugin-1",
            DisplayName = "Plugin",
            AssemblyName = "Plugin.dll",
            TypeName = "Plugin.Type",
            Status = "Loaded"
        });
        db.PluginSources.Add(new PluginSource
        {
            Id = "source-1",
            RepositoryUrl = "https://github.com/owner/repo",
            Owner = "owner",
            Repository = "repo",
            Enabled = true,
            TrustConfirmed = true
        });
        db.PluginSourceReleases.Add(new PluginSourceRelease
        {
            Id = "release-1",
            PluginSourceId = "source-1",
            ReleaseTag = "v1",
            GitHubReleaseId = 10,
            AssetId = 20,
            AssetName = "plugin.zip",
            Status = "Installed"
        });
        db.AiRequestLogs.Add(new AiRequestLog
        {
            Id = "ai-log-1",
            UserId = "user-1",
            Service = "Gemini",
            Type = AiRequestLogType.Success
        });
        db.BackgroundJobs.Add(new BackgroundJob
        {
            Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            JobType = "ExportAll",
            InitiatorUserId = "user-1",
            Status = BackgroundJobStatus.Succeeded,
            Progress = 100,
            ResultMessage = "done"
        });
    }

    private static ExportRootDto ReadExportRoot(Stream zip)
    {
        using var archive = new ZipArchive(zip, ZipArchiveMode.Read, leaveOpen: true);
        var entry = archive.GetEntry("recipes.json");
        entry.Should().NotBeNull();

        using var entryStream = entry!.Open();
        var root = JsonSerializer.Deserialize<ExportRootDto>(entryStream, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        root.Should().NotBeNull();
        return root!;
    }
}
