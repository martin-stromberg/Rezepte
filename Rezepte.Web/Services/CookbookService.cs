using Microsoft.EntityFrameworkCore;
using Rezepte.Web.Data;
using Rezepte.Web.Entities;

namespace Rezepte.Web.Services;

public interface ICookbookService
{
    Task<List<Cookbook>> GetAllAsync(string userId, CancellationToken ct);
    Task<Cookbook?> GetByIdAsync(string userId, string id, CancellationToken ct);
    Task<(bool ok, string? error, Cookbook? cookbook)> CreateAsync(string userId, string name, string? description, CancellationToken ct);
    Task<(bool ok, string? error)> UpdateAsync(string userId, string id, string name, string? description, CancellationToken ct);
    Task<(bool ok, string? error)> DeleteAsync(string userId, string id, CancellationToken ct);
}

public class CookbookService(RezepteDbContext db) : ICookbookService
{
    private readonly RezepteDbContext _db = db;

    public async Task<List<Cookbook>> GetAllAsync(string userId, CancellationToken ct)
    {
        return await _db.Cookbooks.AsNoTracking().Where(c => c.UserId == userId).OrderBy(c => c.Name).ToListAsync(ct);
    }

    public async Task<Cookbook?> GetByIdAsync(string userId, string id, CancellationToken ct)
    {
        return await _db.Cookbooks.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId, ct);
    }

    public async Task<(bool ok, string? error, Cookbook? cookbook)> CreateAsync(string userId, string name, string? description, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length < 3)
        {
            return (false, "Der Name muss mindestens 3 Zeichen haben.", null);
        }
        var entity = new Cookbook
        {
            UserId = userId,
            Name = name.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim()
        };
        _db.Cookbooks.Add(entity);
        await _db.SaveChangesAsync(ct);
        return (true, null, entity);
    }

    public async Task<(bool ok, string? error)> UpdateAsync(string userId, string id, string name, string? description, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length < 3)
        {
            return (false, "Der Name muss mindestens 3 Zeichen haben.");
        }
        var entity = await _db.Cookbooks.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId, ct);
        if (entity is null)
        {
            return (false, "Kochbuch nicht gefunden.");
        }
        entity.Name = name.Trim();
        entity.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        await _db.SaveChangesAsync(ct);
        return (true, null);
    }

    public async Task<(bool ok, string? error)> DeleteAsync(string userId, string id, CancellationToken ct)
    {
        var entity = await _db.Cookbooks.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId, ct);
        if (entity is null)
        {
            return (false, "Kochbuch nicht gefunden.");
        }
        _db.Cookbooks.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return (true, null);
    }
}
