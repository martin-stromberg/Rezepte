using System;

namespace Rezepte.Web.Entities
{
    public sealed class ShoppingListEntity
    {
        public string Id { get; set; } = default!;
        public string UserId { get; set; } = default!;
        // JSON-Serialisiertes Modell (Rezepte.Web.Models.ShoppingList)
        public string Data { get; set; } = default!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ModifiedAt { get; set; }
    }
}