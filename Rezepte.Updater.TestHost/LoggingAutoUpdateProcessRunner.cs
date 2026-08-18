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
        var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        var fileName = isWindows ? "powershell" : "pwsh";

        string actualScriptPath = scriptPath;
        string? wrapperPath = null;

        if (isWindows)
        {
            // Workaround: msTools.Updater generates scripts that use the IISAdministration
            // cmdlets Stop-IISApplicationPool / Start-IISApplicationPool. The in-box
            // IISAdministration module (1.1.0.0) does not export these cmdlets, but the
            // WebAdministration module does (Stop-WebAppPool / Start-WebAppPool). We
            // wrap the original script with a small helper script that defines the
            // missing functions before invoking the generated one.
            wrapperPath = Path.Combine(Path.GetTempPath(), $"RezepteUpdaterWrapper-{Guid.NewGuid():N}.ps1");

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
            actualScriptPath = wrapperPath;
        }

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{actualScriptPath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };

            Console.WriteLine($"[install] Running {fileName} {startInfo.Arguments}");
            Console.WriteLine("[install] --- script content ---");
            try
            {
                Console.WriteLine(File.ReadAllText(scriptPath));
            }
            catch (IOException ex)
            {
                Console.WriteLine($"[install] Could not read script content: {ex.Message}");
            }

            Console.WriteLine("[install] --- end of script content ---");

            process.Start();

            var outputTask = Task.Run(() => process.StandardOutput.ReadToEnd());
            var errorTask = Task.Run(() => process.StandardError.ReadToEnd());

            process.WaitForExit();

            var output = outputTask.GetAwaiter().GetResult();
            var error = errorTask.GetAwaiter().GetResult();

            if (!string.IsNullOrWhiteSpace(output))
            {
                Console.WriteLine(output);
            }

            if (!string.IsNullOrWhiteSpace(error))
            {
                Console.Error.WriteLine(error);
            }

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"Installation script exited with code {process.ExitCode}: {scriptPath}\n\n{error}");
            }
        }
        finally
        {
            if (!string.IsNullOrEmpty(wrapperPath) && File.Exists(wrapperPath))
            {
                File.Delete(wrapperPath);
            }
        }
    }
}
