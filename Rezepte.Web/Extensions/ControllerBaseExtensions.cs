using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;

namespace Rezepte.Web.Extensions;

/// <summary>
/// Represents the controller base extensions class.
/// </summary>
public static class ControllerBaseExtensions
{
    /// <summary>
    /// Returns the file content with a SHA256-based ETag, honoring If-None-Match and setting cache headers.
    /// </summary>
    /// <param name="controller">The controller parameter.</param>
    /// <param name="data">The data parameter.</param>
    /// <param name="contentType">The content type parameter.</param>
    /// <param name="fileName">The file name parameter.</param>
    /// <param name="cacheMaxAgeSeconds">The cache max age seconds parameter.</param>
    /// <returns>The result.</returns>
    public static IActionResult FileWithETag(this ControllerBase controller, byte[] data, string contentType, string? fileName, int cacheMaxAgeSeconds)
    {
        var cacheControl = $"private, max-age={cacheMaxAgeSeconds}";

        // ETag via SHA256 over the file data
        string etag;
        using (var sha = SHA256.Create())
        {
            var hash = sha.ComputeHash(data);
            etag = "\"" + Convert.ToBase64String(hash) + "\""; // quoted ETag
        }

        // Check If-None-Match
        if (controller.Request.Headers.TryGetValue("If-None-Match", out var ifNoneMatch) && ifNoneMatch.Any(h => h == etag))
        {
            controller.Response.Headers["ETag"] = etag;
            controller.Response.Headers["Cache-Control"] = cacheControl;
            return controller.StatusCode(StatusCodes.Status304NotModified);
        }

        // Set cache headers
        controller.Response.Headers["Cache-Control"] = cacheControl;
        controller.Response.Headers["ETag"] = etag;

        return controller.File(data, contentType, fileName);
    }
}
