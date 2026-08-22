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

    // Neue Methode: Reihenfolge persistieren (Liste von Cookbook-Ids in gewünschter Reihenfolge)
    Task<(bool ok, string? error)> ReorderAsync(string userId, List<string> orderedIds, CancellationToken ct);
}

public class CookbookService(RezepteDbContext db) : ICookbookService
{
    private readonly RezepteDbContext _db = db;

    public async Task<List<Cookbook>> GetAllAsync(string userId, CancellationToken ct)
    {
        // nach OrderIndex und dann Name liefern
        return await _db.Cookbooks.AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderBy(c => EF.Property<int>(c, "OrderIndex"))
            .ThenBy(c => c.Name)
            .ToListAsync(ct);
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

        // bestimme maximalen OrderIndex des Benutzers und hänge neu an
        var maxIndex = await _db.Cookbooks
            .Where(c => c.UserId == userId)
            .Select(c => (int?)c.OrderIndex)
            .MaxAsync(ct) ?? -1;

        var entity = new Cookbook
        {
            UserId = userId,
            Name = name.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            OrderIndex = maxIndex + 1
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

    // --- Neue Implementierung: Reihenfolge persistieren ---
    public async Task<(bool ok, string? error)> ReorderAsync(string userId, List<string> orderedIds, CancellationToken ct)
    {
        if (orderedIds == null || orderedIds.Count == 0)
            return (false, "Keine Reihenfolge übergeben.");

        // Lade alle Kochbücher des Benutzers
        var userCookbooks = await _db.Cookbooks.Where(c => c.UserId == userId).ToListAsync(ct);

        // Prüfe, ob alle angegebenen IDs zum Benutzer gehören
        var unknown = orderedIds.Except(userCookbooks.Select(c => c.Id)).ToList();
        if (unknown.Count > 0)
            return (false, "Ungültige Kochbuch-Ids in der Reihenfolge.");

        // Setze OrderIndex entsprechend der übergebenen Reihenfolge
        for (int i = 0; i < orderedIds.Count; i++)
        {
            var id = orderedIds[i];
            var cb = userCookbooks.FirstOrDefault(c => c.Id == id);
            if (cb is not null)
            {
                cb.OrderIndex = i;
            }
        }

        // Für Kochbücher des Users, die nicht in orderedIds sind, setze fortlaufende Indizes danach
        var missing = userCookbooks.Where(c => !orderedIds.Contains(c.Id)).OrderBy(c => c.Name).ToList();
        var start = orderedIds.Count;
        foreach (var cb in missing)
        {
            cb.OrderIndex = start++;
        }

        await _db.SaveChangesAsync(ct);
        return (true, null);
    }
}
