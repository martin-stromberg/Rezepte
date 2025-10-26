using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Rezepte.Web.Models;

namespace Rezepte.Web.Services
{
    public interface IShoppingListService
    {
        Task<IEnumerable<ShoppingList>> GetAllAsync(string userId, CancellationToken ct = default);
        Task<ShoppingList?> GetAsync(string userId, string listId, CancellationToken ct = default);
        Task<ShoppingList> CreateAsync(string userId, ShoppingList list, CancellationToken ct = default);
        Task<bool> UpdateAsync(string userId, ShoppingList list, CancellationToken ct = default);
        Task<bool> DeleteAsync(string userId, string listId, CancellationToken ct = default);
    }
}