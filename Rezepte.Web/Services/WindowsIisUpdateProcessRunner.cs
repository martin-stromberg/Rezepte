using System.Diagnostics;
using System.Runtime.InteropServices;
using msTools.Updater;

namespace Rezepte.Web.Services;

/// <summary>
/// Windows process runner for msTools.Updater that wraps the generated PowerShell script
/// with a workaround for IIS application pools. The in-box IISAdministration module (1.1.0.0)
/// does not expose Stop-IISApplicationPool / Start-IISApplicationPool, but WebAdministration
/// provides the equivalent Stop-WebAppPool / Start-WebAppPool cmdlets. This runner defines
/// the missing functions before invoking the generated script and logs the script output.
/// </summary>
public sealed class WindowsIisUpdateProcessRunner : IAutoUpdateProcessRunner
{
    private readonly ILogger<WindowsIisUpdateProcessRunner> _logger;

    public WindowsIisUpdateProcessRunner(ILogger<WindowsIisUpdateProcessRunner> logger)
    {
        _logger = logger;
    }

    public void EnsureUpdateUnitAvailable(string scriptPath)
    {
        // No systemd unit on Windows; the orchestrator already manages the update lock.
    }

    public void StartScript(string scriptPath)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            throw new PlatformNotSupportedException($"{nameof(WindowsIisUpdateProcessRunner)} can only be used on Windows.");
        }

        var wrapperPath = Path.Combine(Path.GetTempPath(), $"RezepteUpdaterWrapper-{Guid.NewGuid():N}.ps1");

        var wrapperContent = string.Format(
            "Import-Module WebAdministration{0}" +
            "function Stop-IISApplicationPool {{{0}" +
            "    param([string]$Name){0}" +
            "    Stop-WebAppPool -Name $Name{0}" +
            "}}{0}" +
            "function Start-IISApplicationPool {{{0}" +
            "    param([string]$Name){0}" +
            "    Start-WebAppPool -Name $Name{0}" +
            "}}{0}" +
            "& \"{1}\"",
            Environment.NewLine,
            scriptPath);

        File.WriteAllText(wrapperPath, wrapperContent);

        _logger.LogInformation(
            "Starting installation script with WebAdministration workaround: {WrapperPath} -> {ScriptPath}",
            wrapperPath,
            scriptPath);

        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{wrapperPath}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        var process = new Process { StartInfo = startInfo };

        process.Start();

        _ = Task.Run(() =>
        {
            try
            {
                var outputTask = Task.Run(() => process.StandardOutput.ReadToEnd());
                var errorTask = Task.Run(() => process.StandardError.ReadToEnd());

                process.WaitForExit();

                var output = outputTask.GetAwaiter().GetResult();
                var error = errorTask.GetAwaiter().GetResult();

                if (!string.IsNullOrWhiteSpace(output))
                {
                    _logger.LogInformation("Installation script output: {Output}", output);
                }

                if (!string.IsNullOrWhiteSpace(error))
                {
                    _logger.LogError("Installation script error: {Error}", error);
                }

                _logger.LogInformation(
                    "Installation script {ScriptPath} exited with code {ExitCode}",
                    scriptPath,
                    process.ExitCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read installation script output for {ScriptPath}", scriptPath);
            }
            finally
            {
                process.Dispose();

                try
                {
                    if (File.Exists(wrapperPath))
                    {
                        File.Delete(wrapperPath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete wrapper script {WrapperPath}", wrapperPath);
                }
            }
        });
    }
}
