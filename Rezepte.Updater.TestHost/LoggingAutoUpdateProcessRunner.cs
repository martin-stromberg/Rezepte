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

    public void StartScript(string scriptPath, string? zipPath)
    {
        if (!string.IsNullOrWhiteSpace(zipPath))
        {
            var package = new FileInfo(zipPath);
            if (!package.Exists)
            {
                throw new FileNotFoundException("Update package was not found.", zipPath);
            }

            if (package.Length == 0)
            {
                throw new InvalidOperationException($"Update package is empty: {zipPath}");
            }
        }

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
}
