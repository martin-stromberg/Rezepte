using System.Diagnostics;
using System.Runtime.InteropServices;
using msTools.Updater;

namespace Rezepte.Updater.TestHost;

/// <summary>
/// Process runner that captures the installation script output and prints it to the console.
/// Useful for debugging why the generated PowerShell script does not install the application.
/// </summary>
public sealed class LoggingAutoUpdateProcessRunner : IAutoUpdateProcessRunner
{
    public void EnsureUpdateUnitAvailable(string scriptPath)
    {
        // No systemd unit on Windows; the orchestrator already manages the update lock.
    }

    public void StartScript(string scriptPath)
    {
        var fileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "powershell"
            : "pwsh";

        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };

        process.OutputDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                Console.WriteLine($"[install] {e.Data}");
            }
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                Console.WriteLine($"[install-error] {e.Data}");
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Installation script exited with code {process.ExitCode}: {scriptPath}");
        }
    }
}
