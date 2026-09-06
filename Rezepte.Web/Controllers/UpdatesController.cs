using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using msTools.Updater;

namespace Rezepte.Web.Controllers;

/// <summary>
/// Represents the updates controller class.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
public class UpdatesController : ControllerBase
{
    private readonly IAutoUpdateServiceResolver _serviceResolver;
    private readonly IOptions<AutoUpdateOptions> _updateOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdatesController"/> class.
    /// </summary>
    /// <param name="serviceResolver">The service resolver parameter.</param>
    /// <param name="updateOptions">The update options parameter.</param>
    public UpdatesController(IAutoUpdateServiceResolver serviceResolver, IOptions<AutoUpdateOptions> updateOptions)
    {
        _serviceResolver = serviceResolver;
        _updateOptions = updateOptions;
    }

    /// <summary>
    /// preflights the value.
    /// </summary>
    /// <returns>The result.</returns>
    [HttpGet("preflight")]
    public IActionResult Preflight()
    {
        var options = _updateOptions.Value;

        AutoUpdateInstallationTarget target;
        try
        {
            target = _serviceResolver.Resolve();
        }
        catch (Exception ex)
        {
            return Ok(new
            {
                Resolved = false,
                Error = ex.Message,
                Options = new
                {
                    options.AppPoolName,
                    options.SiteName,
                    options.ServiceName,
                    options.ExecutablePath,
                    options.UpdateUnitName,
                    options.StopHostAfterScriptStart,
                    options.DownloadPath
                },
                Hints = new[]
                {
                    "The service resolver rejected the current configuration.",
                    "Ensure one of AppPoolName, ServiceName or ExecutablePath is set on Windows, or UpdateUnitName on Linux."
                }
            });
        }

        var hints = new List<string>();

        if (string.IsNullOrWhiteSpace(target.AppPoolName)
            && string.IsNullOrWhiteSpace(target.ServiceName)
            && string.IsNullOrWhiteSpace(target.ExecutablePath))
        {
            hints.Add("No Windows installation target (AppPoolName, ServiceName, ExecutablePath) was resolved.");
        }

        if (target.Platform == "linux" && string.IsNullOrWhiteSpace(options.UpdateUnitName))
        {
            hints.Add("Linux platform detected but UpdateUnitName is not configured.");
        }

        if (target.Platform == "windows" && !string.IsNullOrWhiteSpace(target.AppPoolName))
        {
            hints.Add($"App pool '{target.AppPoolName}' was resolved. Verify the application account has permission to stop/start it.");
        }

        return Ok(new
        {
            Resolved = true,
            Target = new
            {
                target.Platform,
                target.AppPoolName,
                target.SiteName,
                target.ServiceName,
                target.ExecutablePath
            },
            Options = new
            {
                options.AppPoolName,
                options.SiteName,
                options.ServiceName,
                options.ExecutablePath,
                options.UpdateUnitName,
                options.StopHostAfterScriptStart,
                options.DownloadPath
            },
            Hints = hints
        });
    }
}
