using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Rezepte.Updater.TestHost;
using msTools.Updater;
using System.Linq;

var builder = Host.CreateApplicationBuilder(args);

builder.UseAutoUpdate(autoUpdate =>
{
    autoUpdate.BindConfiguration("ApplicationUpdates");

    var section = builder.Configuration.GetSection("ApplicationUpdates");

    var appPoolName = section["AppPoolName"];
    var siteName = section["SiteName"];
    var repositoryOwner = section["RepositoryOwner"];
    var repositoryName = section["RepositoryName"];
    var manifestAssetName = section["ManifestAssetName"];
    var localSourceDirectory = section["LocalSourceDirectory"];
    var updateUnitName = section["UpdateUnitName"];
    var allowPrerelease = bool.TryParse(section["AllowPrereleaseUpdates"], out var parsed) && parsed;

    if (allowPrerelease)
    {
        autoUpdate.EnablePrereleaseUpdates();
    }

    if (!string.IsNullOrWhiteSpace(updateUnitName))
    {
        autoUpdate.WithUpdateUnitName(updateUnitName);
    }

    if (!string.IsNullOrWhiteSpace(repositoryOwner) && !string.IsNullOrWhiteSpace(repositoryName))
    {
        autoUpdate.UseGithubSource(repositoryOwner, repositoryName, manifestAssetName);
    }
    else if (!string.IsNullOrWhiteSpace(localSourceDirectory))
    {
        autoUpdate.UseLocalFolderSource(localSourceDirectory);
    }

    if (!string.IsNullOrWhiteSpace(appPoolName))
    {
        autoUpdate.WithIisApplicationPool(appPoolName, siteName ?? string.Empty);
    }
});

var processRunnerDescriptors = builder.Services
    .Where(d => d.ServiceType == typeof(IAutoUpdateProcessRunner))
    .ToList();

foreach (var descriptor in processRunnerDescriptors)
{
    builder.Services.Remove(descriptor);
}

builder.Services.AddSingleton<IAutoUpdateProcessRunner, LoggingAutoUpdateProcessRunner>();

using var host = builder.Build();

var command = args.FirstOrDefault()?.ToLowerInvariant() ?? "preflight";

switch (command)
{
    case "preflight":
        await RunPreflightAsync(host);
        break;
    case "check":
        await RunCheckAsync(host);
        break;
    case "download":
        await RunDownloadAsync(host);
        break;
    case "install":
        await RunInstallAsync(host);
        break;
    case "run":
        await RunUpdateAsync(host);
        break;
    default:
        Console.WriteLine("Unknown command.");
        Console.WriteLine("Usage: Rezepte.Updater.TestHost [preflight|check|download|install|run]");
        break;
}

static async Task RunPreflightAsync(IHost host)
{
    var resolver = host.Services.GetRequiredService<IAutoUpdateServiceResolver>();
    var options = host.Services.GetRequiredService<AutoUpdateOptions>();

    Console.WriteLine("Configuration:");
    Console.WriteLine($"  AppPoolName:    {options.AppPoolName}");
    Console.WriteLine($"  SiteName:       {options.SiteName}");
    Console.WriteLine($"  ServiceName:    {options.ServiceName}");
    Console.WriteLine($"  ExecutablePath: {options.ExecutablePath}");
    Console.WriteLine($"  UpdateUnitName: {options.UpdateUnitName}");
    Console.WriteLine($"  DownloadPath:   {options.DownloadPath}");
    Console.WriteLine($"  Source:         {options.Source}");
    Console.WriteLine();

    try
    {
        var target = resolver.Resolve();
        Console.WriteLine("Resolved installation target:");
        Console.WriteLine($"  Platform:       {target.Platform}");
        Console.WriteLine($"  AppPoolName:    {target.AppPoolName}");
        Console.WriteLine($"  SiteName:       {target.SiteName}");
        Console.WriteLine($"  ServiceName:    {target.ServiceName}");
        Console.WriteLine($"  ExecutablePath: {target.ExecutablePath}");

        if (string.IsNullOrWhiteSpace(target.AppPoolName)
            && string.IsNullOrWhiteSpace(target.ServiceName)
            && string.IsNullOrWhiteSpace(target.ExecutablePath)
            && string.IsNullOrWhiteSpace(options.UpdateUnitName))
        {
            Console.WriteLine();
            Console.WriteLine("WARNING: No installation target was resolved. Check AppPoolName, ServiceName, ExecutablePath or UpdateUnitName.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"ERROR: Could not resolve installation target: {ex.Message}");
    }
}

static async Task RunCheckAsync(IHost host)
{
    var orchestrator = host.Services.GetRequiredService<IAutoUpdateOrchestrator>();
    var result = await orchestrator.CheckForUpdateAsync(CancellationToken.None);
    PrintResult(result);
}

static async Task RunDownloadAsync(IHost host)
{
    var orchestrator = host.Services.GetRequiredService<IAutoUpdateOrchestrator>();
    var result = await orchestrator.DownloadAsync(CancellationToken.None);
    PrintResult(result);
}

static async Task RunInstallAsync(IHost host)
{
    var orchestrator = host.Services.GetRequiredService<IAutoUpdateOrchestrator>();
    var result = await orchestrator.InstallAsync(confirmDowntime: true, CancellationToken.None);
    PrintResult(result);
}

static async Task RunUpdateAsync(IHost host)
{
    var orchestrator = host.Services.GetRequiredService<IAutoUpdateOrchestrator>();
    var result = await orchestrator.RunUpdateAsync(CancellationToken.None);
    PrintResult(result);
}

static void PrintResult(AutoUpdateResult result)
{
    Console.WriteLine($"Outcome:    {result.Outcome}");
    Console.WriteLine($"State:      {result.State}");
    Console.WriteLine($"Code:       {result.Code}");
    if (!string.IsNullOrWhiteSpace(result.Message))
    {
        Console.WriteLine($"Message:    {result.Message}");
    }
    if (result.Error is not null)
    {
        Console.WriteLine($"Error Code: {result.Error.Code}");
        Console.WriteLine($"Message:    {result.Error.Message}");
        if (result.Error.Exception is not null)
        {
            Console.WriteLine($"Exception:  {result.Error.Exception}");
        }
    }
}
