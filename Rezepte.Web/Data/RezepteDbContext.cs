using Microsoft.EntityFrameworkCore;
using Rezepte.Web.Entities;
using Rezepte.Web.Services.BackgroundJobs;
using System.Collections.Generic;

namespace Rezepte.Web.Data;

public class RezepteDbContext(DbContextOptions<RezepteDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Cookbook> Cookbooks => Set<Cookbook>();
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<RecipeStep> RecipeSteps => Set<RecipeStep>();
    public DbSet<RecipeCookbook> RecipeCookbooks => Set<RecipeCookbook>();
    public DbSet<RecipeSideDish> RecipeSideDishes => Set<RecipeSideDish>();
    public DbSet<RecipeIngredient> RecipeIngredients => Set<RecipeIngredient>();
    public DbSet<RecipeImage> RecipeImages { get; set; } = null!;
    public DbSet<AiRequestLog> AiRequestLogs => Set<AiRequestLog>();

    public DbSet<UserSetting> UserSettings => Set<UserSetting>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();

    // Calendar events
    public DbSet<CalendarEvent> CalendarEvents => Set<CalendarEvent>();
    public DbSet<ShoppingListGroup> ShoppingListGroups => Set<ShoppingListGroup>();
    public DbSet<ShoppingListItem> ShoppingListItems => Set<ShoppingListItem>();
    public DbSet<BackgroundJob> BackgroundJobs => Set<BackgroundJob>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<User>(b =>
        {
            b.HasKey(u => u.Id);
            b.Property(u => u.Username).IsRequired().HasMaxLength(64);
            b.Property(u => u.Email).HasMaxLength(256);
            b.Property(u => u.PasswordHash).IsRequired();
            b.Property(u => u.IsAdmin).HasDefaultValue(false);
            b.HasIndex(u => u.Username).IsUnique();
            b.HasIndex(u => u.Email).IsUnique(false);
        });

        modelBuilder.Entity<Cookbook>(b =>
        {
            b.HasKey(c => c.Id);
            b.Property(c => c.UserId).IsRequired().HasMaxLength(64);
            b.Property(c => c.Name).IsRequired().HasMaxLength(128);
            b.Property(c => c.Description).HasMaxLength(1024);
            b.Property(c => c.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Real CLR-Eigenschaft OrderIndex konfigurieren (nicht als Shadow-Property)
            b.Property(c => c.OrderIndex).HasDefaultValue(0);

            // Index auf UserId + OrderIndex fuer schnelle Sortierung
            b.HasIndex(c => new { c.UserId, c.OrderIndex });

            b.HasIndex(c => c.Name).IsUnique(false);
            b.HasIndex(c => new { c.UserId, c.Name });
        });

        modelBuilder.Entity<Recipe>(b =>
        {
            b.HasKey(r => r.Id);
            b.Property(r => r.UserId).IsRequired().HasMaxLength(64);
            b.Property(r => r.Title).IsRequired().HasMaxLength(200);
            b.Property(r => r.Description).HasMaxLength(4000);
            b.Property(r => r.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.HasIndex(r => new { r.UserId, r.Title });
        });

        modelBuilder.Entity<RecipeCookbook>(buildAction =>
        {
            buildAction.HasKey(rc => rc.Id);
            buildAction.HasOne(rc => rc.Cookbook)
                .WithMany(c => c.RecipeCookbooks)
                .HasForeignKey(rc => rc.CookbookId)
                .OnDelete(DeleteBehavior.Cascade);
            buildAction.HasOne(rc => rc.Recipe)
                .WithMany(r => r.RecipeCookbooks)
                .HasForeignKey(rc => rc.RecipeId)
                .OnDelete(DeleteBehavior.Cascade);
            buildAction.HasIndex(rc => new { rc.CookbookId, rc.RecipeId }).IsUnique();
        });

        modelBuilder.Entity<RecipeSideDish>(b =>
        {
            b.HasKey(sd => sd.Id);
            b.Property(sd => sd.RecipeId).IsRequired().HasMaxLength(64);
            b.Property(sd => sd.SideDishRecipeId).IsRequired().HasMaxLength(64);
            b.Property(sd => sd.OrderIndex).HasDefaultValue(0);

            b.HasOne(sd => sd.Recipe)
                .WithMany(r => r.SideDishes)
                .HasForeignKey(sd => sd.RecipeId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(sd => sd.SideDishRecipe)
                .WithMany(r => r.UsedAsSideDishFor)
                .HasForeignKey(sd => sd.SideDishRecipeId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(sd => new { sd.RecipeId, sd.SideDishRecipeId }).IsUnique();
            b.HasIndex(sd => sd.SideDishRecipeId);
        });

        modelBuilder.Entity<RecipeStep>(b =>
        {
            b.HasKey(s => s.Id);
            b.Property(s => s.Title).HasMaxLength(200);
            b.Property(s => s.Description).IsRequired().HasMaxLength(4000);
            b.Property(s => s.DurationMinutes).HasDefaultValue(0);
            b.HasOne<Recipe>()
                .WithMany(r => r.Steps)
                .HasForeignKey(s => s.RecipeId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasIndex(s => new { s.RecipeId, s.StepIndex }).IsUnique();
        });

        modelBuilder.Entity<RecipeIngredient>(b =>
        {
            b.HasKey(i => i.Id);
            b.Property(i => i.Unit).HasMaxLength(64);
            b.Property(i => i.Name).IsRequired().HasMaxLength(200);
            b.HasOne<RecipeStep>()
                .WithMany(s => s.Ingredients)
                .HasForeignKey(i => i.StepId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Recipe>()
            .HasMany(r => r.Images)
            .WithOne(i => i.Recipe)
            .HasForeignKey(i => i.RecipeId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AiRequestLog>(b => 
        {
            b.HasKey(a => a.Id);
            b.Property(a => a.UserId).IsRequired().HasMaxLength(64);
            b.Property(a => a.Service).IsRequired().HasMaxLength(100);
            b.Property(a => a.Timestamp).HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(a => a.Type).IsRequired();
            b.HasIndex(a => new { a.Type, a.Timestamp });
            b.HasIndex(a => new { a.UserId, a.Type, a.Timestamp });
        });

        // CalendarEvent configuration
        modelBuilder.Entity<CalendarEvent>(b =>
        {
            b.HasKey(e => e.Id);
            b.Property(e => e.UserId).IsRequired().HasMaxLength(64);
            b.Property(e => e.RecipeId).HasMaxLength(64).IsRequired(false);
            b.Property(e => e.StartDate).IsRequired();
            b.Property(e => e.TimeOfDay).IsRequired();
            b.Property(e => e.Portions).HasDefaultValue(1);
            b.Property(e => e.Recurrence).HasDefaultValue(RecurrenceType.None);
            b.Property(e => e.RecurrenceDays).HasDefaultValue(WeekDays.None);
            b.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(e => e.ModifiedAt).IsRequired(false);

            // optional relationship to Recipe (if recipe removed, keep event but null RecipeId)
            b.HasOne(e => e.Recipe)
                .WithMany()
                .HasForeignKey(e => e.RecipeId)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasIndex(e => new { e.UserId, e.StartDate });
            b.HasIndex(e => e.RecipeId);
        });

        modelBuilder.Entity<ShoppingListGroup>(b =>
        {
            b.HasKey(g => g.Id);
            b.Property(g => g.UserId).IsRequired().HasMaxLength(64);
            b.Property(g => g.Name).IsRequired().HasMaxLength(128);
            b.Property(g => g.RecipeId).HasMaxLength(64).IsRequired(false);
            b.Property(g => g.OrderIndex).HasDefaultValue(0);
            b.Property(g => g.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(g => g.ModifiedAt).IsRequired(false);

            b.HasOne(g => g.Recipe)
                .WithMany()
                .HasForeignKey(g => g.RecipeId)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasIndex(g => new { g.UserId, g.OrderIndex });
            b.HasIndex(g => g.RecipeId);
        });

        modelBuilder.Entity<ShoppingListItem>(b =>
        {
            b.HasKey(i => i.Id);
            b.Property(i => i.GroupId).IsRequired().HasMaxLength(64);
            b.Property(i => i.Amount).HasColumnType("TEXT");
            b.Property(i => i.Unit).HasMaxLength(64);
            b.Property(i => i.Name).IsRequired().HasMaxLength(200);
            b.Property(i => i.IsChecked).HasDefaultValue(false);
            b.Property(i => i.OrderIndex).HasDefaultValue(0);
            b.Property(i => i.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(i => i.ModifiedAt).IsRequired(false);

            b.HasOne(i => i.Group)
                .WithMany(g => g.Items)
                .HasForeignKey(i => i.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(i => new { i.GroupId, i.OrderIndex });
        });

        modelBuilder.Entity<BackgroundJob>(b =>
        {
            b.HasKey(j => j.Id);
            b.Property(j => j.JobType).IsRequired().HasMaxLength(100);
            b.Property(j => j.InitiatorUserId).HasMaxLength(64);
            b.Property(j => j.CreatedAt).IsRequired();
            b.Property(j => j.Status).IsRequired();
            b.Property(j => j.Progress).HasDefaultValue(0);
            b.Property(j => j.PayloadJson).HasColumnType("TEXT");
            b.Property(j => j.ResultMessage).HasColumnType("TEXT");
            b.Property(j => j.Error).HasColumnType("TEXT");
            b.HasIndex(j => new { j.Status, j.CreatedAt });
            b.HasIndex(j => j.InitiatorUserId);
        });

        // Konfiguration fuer UserSetting
        modelBuilder.Entity<UserSetting>(b =>
        {
            b.HasKey(u => u.UserId);
            b.Property(u => u.UserId).IsRequired().HasMaxLength(64);
            b.Property(u => u.AiEnabled).IsRequired().HasDefaultValue(true);
            b.HasIndex(u => u.UserId).IsUnique();
        });

        // Konfiguration fuer AppSetting
        modelBuilder.Entity<AppSetting>(b =>
        {
            b.HasKey(a => a.Key);
            b.Property(a => a.Key).IsRequired().HasMaxLength(128);
            b.Property(a => a.Value).IsRequired();
        });
    }
}

