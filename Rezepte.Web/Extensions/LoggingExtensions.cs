using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace Rezepte.Web.Extensions;

/// <summary>
/// Represents the logging extensions class.
/// </summary>
public static class LoggingExtensions
{
    /// <summary>
    /// Configures Serilog for console (systemd) and rolling file logging.
    /// - Console sink logs Errors (suitable for Linux service journalctl)
    /// - File sink logs Information+ to ./logs/app-.log (daily rolling), retained for 7 days
    /// </summary>
    /// <param name="builder">The builder parameter.</param>
    /// <param>...</param>
    /// <param>...</param>
    /// <param>...</param>
    public static void ConfigureSerilog(this WebApplicationBuilder builder)
    {
        var env = builder.Environment;
        var logDir = Path.Combine(AppContext.BaseDirectory, "logs");
        Directory.CreateDirectory(logDir);

        // Set minimum levels
        var levelSwitch = new Serilog.Core.LoggingLevelSwitch(env.IsDevelopment() ? LogEventLevel.Information : LogEventLevel.Information);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.ControlledBy(levelSwitch)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "Rezepte")
            // Console: only errors for systemd/journalctl
            .WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Error)
            // File: daily rolling, keep 7 days by time, non-shared and non-buffered to avoid exceptions
            .WriteTo.File(
                path: Path.Combine(logDir, "app-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: null,
                retainedFileTimeLimit: TimeSpan.FromDays(7),
                restrictedToMinimumLevel: LogEventLevel.Information,
                shared: false,
                buffered: false)
            .CreateLogger();

        builder.Host.UseSerilog(Log.Logger, dispose: true);
    }
}
