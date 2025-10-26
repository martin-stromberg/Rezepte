using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Rezepte.Web.Data;
using Rezepte.Web.Models;
using Rezepte.Web.Entities;

namespace Rezepte.Web.Services
{
    public sealed class ShoppingListService : IShoppingListService
    {
        private readonly RezepteDbContext _db;
        private readonly ILogger<ShoppingListService> _logger;

        public ShoppingListService(RezepteDbContext db, ILogger<ShoppingListService> logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<ShoppingList>> GetAllAsync(string userId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(userId)) return Array.Empty<ShoppingList>();
            var ents = await _db.Set<ShoppingListEntity>()
                .AsNoTracking()
                .Where(e => e.UserId == userId)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync(ct);
            return ents
                .Select(e => JsonSerializer.Deserialize<ShoppingList>(e.Data))
                .Where(s => s != null)!;
        }

        public async Task<ShoppingList?> GetAsync(string userId, string listId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(listId)) return null;
            var ent = await _db.Set<ShoppingListEntity>()
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.UserId == userId && e.Id == listId, ct);
            if (ent == null) return null;
            return JsonSerializer.Deserialize<ShoppingList>(ent.Data);
        }

        public async Task<ShoppingList> CreateAsync(string userId, ShoppingList list, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentNullException(nameof(userId));
            if (list == null) throw new ArgumentNullException(nameof(list));

            if (string.IsNullOrWhiteSpace(list.Id)) list.Id = Guid.NewGuid().ToString();
            var ent = new ShoppingListEntity
            {
                Id = list.Id,
                UserId = userId,
                Data = JsonSerializer.Serialize(list),
                CreatedAt = DateTime.UtcNow
            };
            _db.Add(ent);
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Created shopping list {ListId} for user {UserId}", ent.Id, userId);
            return list;
        }

        public async Task<bool> UpdateAsync(string userId, ShoppingList list, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(userId) || list == null) return false;
            var ent = await _db.Set<ShoppingListEntity>()
                .FirstOrDefaultAsync(e => e.UserId == userId && e.Id == list.Id, ct);
            if (ent == null) return false;
            ent.Data = JsonSerializer.Serialize(list);
            ent.ModifiedAt = DateTime.UtcNow;
            _db.Update(ent);
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Updated shopping list {ListId} for user {UserId}", ent.Id, userId);
            return true;
        }

        public async Task<bool> DeleteAsync(string userId, string listId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(listId)) return false;
            var ent = await _db.Set<ShoppingListEntity>()
                .FirstOrDefaultAsync(e => e.UserId == userId && e.Id == listId, ct);
            if (ent == null) return false;
            _db.Remove(ent);
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Deleted shopping list {ListId} for user {UserId}", listId, userId);
            return true;
        }
    }
}