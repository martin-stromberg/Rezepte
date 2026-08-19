using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rezepte.Web.Dto;
using Rezepte.Web.Services;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class UserStatsController : ControllerBase
{
    private readonly IRecipeService _recipes;
    private readonly ICookbookService _cookbooks;
    private readonly IAiUsageService _aiUsage;
    private readonly IUserService _users;

    public UserStatsController(IRecipeService recipes, ICookbookService cookbooks, IAiUsageService aiUsage, IUserService users)
    {
        _recipes = recipes;
        _cookbooks = cookbooks;
        _aiUsage = aiUsage;
        _users = users;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMyStats(CancellationToken ct)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return Unauthorized();

        var user = await _users.GetByIdAsync(userId, ct);
        var reg = user?.RegistrationTime;
        var since = reg.HasValue ? (DateTime.UtcNow - reg.Value) : TimeSpan.Zero;
        var cookbooks = await _cookbooks.GetAllAsync(userId, ct);
        var cookbookCount = cookbooks?.Count ?? 0;
        var recipes = await _recipes.GetByCookbookAsync(userId, "", ct); // ct hinzugefügt
        var ownRecipeCount = recipes?.Count ?? 0;
        var aiCount = await _aiUsage.GetCountAsync(userId, ct);

        var dto = new UserStatsDto(
            TimeSinceRegistration: FormatTimeSpan(since),
            CookbookCount: cookbookCount,
            OwnRecipeCount: ownRecipeCount,
            AiRequestCount: aiCount
        );
        return Ok(dto);
    }

    private static string FormatTimeSpan(TimeSpan ts)
    {
        if (ts.TotalDays < 1) return $"{(int)ts.TotalHours}h";
        if (ts.TotalDays < 30) return $"{(int)ts.TotalDays}d";
        if (ts.TotalDays < 365) return $"{(int)(ts.TotalDays / 30)}mo";
        return $"{(int)(ts.TotalDays / 365)}y";
    }
}