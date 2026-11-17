using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Rezepte.Web.Configuration;
using Rezepte.Web.Extensions;
using Rezepte.Web.Services;

namespace Rezepte.Web.Controllers;

/// <summary>
/// Represents the share controller class.
/// </summary>
/// <param name="shareService">The share service parameter.</param>
/// <param name="imageOptions">The image options parameter.</param>
/// <returns>The result.</returns>
[ApiController]
[Route("api/share")]
public class ShareController(IRecipeShareService shareService, IOptions<ImageOptions> imageOptions) : ControllerBase
{
    private readonly IRecipeShareService _shareService = shareService;
    private readonly ImageOptions _imageOptions = imageOptions.Value;

    /// <summary>
    /// Gets the shared image.
    /// </summary>
    /// <param name="token">The token parameter.</param>
    /// <param name="imageId">The image id parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    [HttpGet("{token}/image/{imageId}")]
    public async Task<IActionResult> GetSharedImage(string token, string imageId, CancellationToken ct)
    {
        var image = await _shareService.GetSharedImageAsync(token, imageId, ct);
        if (image == null) return NotFound();

        var contentType = string.IsNullOrWhiteSpace(image.ContentType) ? "image/jpeg" : image.ContentType;

        return this.FileWithETag(image.Data, contentType, image.FileName, _imageOptions.CacheMaxAgeSeconds);
    }
}
