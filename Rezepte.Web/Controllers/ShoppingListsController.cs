using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Rezepte.Web.Models;
using Rezepte.Web.Services;
using System.Globalization;
using static Rezepte.Web.Controllers.RecipesController;

namespace Rezepte.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public sealed class ShoppingListsController : ControllerBase
    {
        private readonly IShoppingListService _shoppingListService;
        private readonly IRecipeService _recipeService;
        private readonly ILogger<ShoppingListsController> _logger;

        public ShoppingListsController(IShoppingListService shoppingListService, IRecipeService recipeService, ILogger<ShoppingListsController> logger)
        {
            _shoppingListService = shoppingListService ?? throw new ArgumentNullException(nameof(shoppingListService));
            _recipeService = recipeService ?? throw new ArgumentNullException(nameof(recipeService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // Helper to resolve a user id from claims. Adjust to your auth scheme.
        private string GetUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.Identity?.Name
                ?? "anonymous";
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ShoppingList>>> GetAll(CancellationToken ct)
        {
            var userId = GetUserId();
            var lists = await _shoppingListService.GetAllAsync(userId, ct);
            return Ok(lists);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ShoppingList>> Get(string id, CancellationToken ct)
        {
            var userId = GetUserId();
            var list = await _shoppingListService.GetAsync(userId, id, ct);
            if (list == null) return NotFound();
            return Ok(list);
        }

        [HttpPost]
        public async Task<ActionResult<ShoppingList>> Create([FromBody] ShoppingList list, CancellationToken ct)
        {
            var userId = GetUserId();
            if (list == null) return BadRequest();
            var created = await _shoppingListService.CreateAsync(userId, list, ct);
            return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] ShoppingList list, CancellationToken ct)
        {
            var userId = GetUserId();
            if (list == null || id != list.Id) return BadRequest();
            var ok = await _shoppingListService.UpdateAsync(userId, list, ct);
            return ok ? NoContent() : NotFound();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id, CancellationToken ct)
        {
            var userId = GetUserId();
            var ok = await _shoppingListService.DeleteAsync(userId, id, ct);
            return ok ? NoContent() : NotFound();
        }

        public sealed record AddRecipeGroupRequest(string RecipeId);

        [HttpPost("{listId}/groups/from-recipe")]
        public async Task<IActionResult> AddGroupFromRecipeAsync(string listId, [FromBody] AddRecipeGroupRequest req, CancellationToken ct)
        {
            var userId = GetUserId();
            var list = await _shoppingListService.GetAsync(userId, listId, ct);
            if (list == null)
            {
                return NotFound();
            }
            var recipe = await _recipeService.GetByIdAsync(userId, req.RecipeId!, ct);
            if (recipe == null)
            {
                return NotFound();
            }

            var items = recipe.Steps
                .SelectMany(s => s.Ingredients != null
                    ? s.Ingredients.Select(i => new ShoppingItem
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = i.Name ?? string.Empty,
                        Quantity = i.Amount != 0 ? i.Amount.ToString(CultureInfo.InvariantCulture) : string.Empty,
                        Unit = i.Unit ?? string.Empty
                    })
                    : Enumerable.Empty<ShoppingItem>())
                .ToList();

            list.Groups ??= new List<ShoppingGroup>();

            if (list.Groups.Count == 1 && (list.Groups[0].Items == null || list.Groups[0].Items.Count == 0))
            {
                list.Groups[0].Name = recipe.Title ?? "Einkauf";
                list.Groups[0].Items = items;
            }
            else
            {
                list.Groups.Add(new ShoppingGroup { Id = Guid.NewGuid().ToString(), Name = recipe.Title ?? "Einkauf", Items = items });
            }

            await _shoppingListService.UpdateAsync(userId, list, ct);
            return Ok(list);
        }
    }
}