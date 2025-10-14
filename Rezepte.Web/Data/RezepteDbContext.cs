using Microsoft.EntityFrameworkCore;
using Rezepte.Web.Entities;

namespace Rezepte.Web.Data;

public class RezepteDbContext(DbContextOptions<RezepteDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Cookbook> Cookbooks => Set<Cookbook>();
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<RecipeStep> RecipeSteps => Set<RecipeStep>();
    public DbSet<RecipeCookbook> RecipeCookbooks => Set<RecipeCookbook>();
    public DbSet<RecipeIngredient> RecipeIngredients => Set<RecipeIngredient>();
    public DbSet<RecipeImage> RecipeImages { get; set; } = null!;

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
    }
}
