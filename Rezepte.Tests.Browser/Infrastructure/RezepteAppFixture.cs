using System.Diagnostics;
using System.Net.Http.Json;
using System.Net.Sockets;
using Rezepte.Tests.TestHelpers;
using Xunit;

namespace Rezepte.Tests.Browser.Infrastructure;

/// <summary>
/// Starts the Rezepte.Web application as its own process against a temporary SQLite database,
/// seeds a test user, and tears the process and database down again.
/// </summary>
public class RezepteAppFixture : IAsyncLifetime
{
    /// <summary>
    /// The default username used for the seeded browser test account.
    /// </summary>
    public const string TestUsername = "browsertest";

    /// <summary>
    /// The default password used for the seeded browser test account.
    /// </summary>
    public const string TestPassword = "BrowserTest!123";

    private const string PublishDirectoryEnvironmentVariable = "REZEPTE_PUBLISH_DIR";
    private const string TestJwtSigningKey = "browser-test-signing-key-0123456789";
    private const string RegisterEndpoint = "api/auth/register";
    private const string TestEmail = "browsertest@example.invalid";
    private const int StartupTimeoutSeconds = 60;
    private const int ReadinessPollIntervalMilliseconds = 250;
    private const int ShutdownGraceMilliseconds = 5000;

    private Process? _process;
    private string? _tempDirectory;

    /// <summary>
    /// Gets the base URL of the started application.
    /// </summary>
    public string BaseAddress { get; private set; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether the application process was started successfully.
    /// </summary>
    public bool ApplicationAvailable { get; private set; }

    /// <summary>
    /// Gets the reason given to xUnit when <see cref="ApplicationAvailable"/> is <c>false</c>.
    /// </summary>
    public string ApplicationUnavailableSkipReason { get; private set; } = "Rezepte.Web is not published.";

    /// <summary>
    /// Starts the published web application, waits until it is reachable, and registers the test user.
    /// </summary>
    /// <returns>A task that represents the asynchronous initialization operation.</returns>
    public async Task InitializeAsync()
    {
        var applicationDllPath = ResolveApplicationDllPath();
        if (applicationDllPath is null)
        {
            ApplicationAvailable = false;
            return;
        }

        var databasePath = CreateTemporaryDatabase();
        try
        {
            StartApplicationProcess(applicationDllPath, databasePath);

            await WaitUntilReadyAsync();
            await RegisterTestUserAsync();
            ApplicationAvailable = true;
        }
        catch
        {
            await DisposeAsync();
            throw;
        }
    }

    /// <summary>
    /// Stops the application process and deletes the temporary database.
    /// </summary>
    /// <returns>A completed task.</returns>
    public Task DisposeAsync()
    {
        try
        {
            if (_process is not null && !_process.HasExited)
            {
                _process.Kill(entireProcessTree: true);
                _process.WaitForExit(ShutdownGraceMilliseconds);
            }
        }
        finally
        {
            _process?.Dispose();
            _process = null;

            if (_tempDirectory is not null && Directory.Exists(_tempDirectory))
            {
                try
                {
                    Directory.Delete(_tempDirectory, recursive: true);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    // Best effort cleanup; a lingering temp file must not fail the test run.
                }
            }

            _tempDirectory = null;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Returns the environment variables that should be added to the application process.
    /// </summary>
    /// <returns>A read-only dictionary of environment variable names and values.</returns>
    protected virtual IReadOnlyDictionary<string, string?> GetEnvironmentOverrides()
    {
        return new Dictionary<string, string?>();
    }

    private string CreateTemporaryDatabase()
    {
        _tempDirectory = Directory.CreateTempSubdirectory("rezepte-browser-tests-").FullName;
        return Path.Combine(_tempDirectory, "rezepte-browser-test.db");
    }

    private void StartApplicationProcess(string applicationDllPath, string databasePath)
    {
        var port = GetFreeTcpPort();
        BaseAddress = $"http://127.0.0.1:{port}";

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = Path.GetDirectoryName(applicationDllPath),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add(applicationDllPath);

        startInfo.EnvironmentVariables["ASPNETCORE_URLS"] = BaseAddress;
        startInfo.EnvironmentVariables["ASPNETCORE_ENVIRONMENT"] = "Production";
        startInfo.EnvironmentVariables["ConnectionStrings__Default"] = $"Data Source={databasePath}";
        startInfo.EnvironmentVariables["Jwt__Key"] = TestJwtSigningKey;
        // The browser suite logs in far more often than a real client, so the authentication
        // rate limit is raised instead of letting tests fail with HTTP 429.
        startInfo.EnvironmentVariables["RateLimiting__Authentication__PermitLimit"] = "1000";

        foreach (var (key, value) in GetEnvironmentOverrides())
        {
            startInfo.EnvironmentVariables[key] = value;
        }

        _process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Could not start the Rezepte.Web process for the browser tests.");

        // The redirected output/error pipes must be drained continuously; otherwise the OS pipe buffer
        // fills up once the application logs enough (e.g. one line per incoming request) and the child
        // process blocks on its next Console.Write call, hanging the whole application.
        _process.OutputDataReceived += (_, _) => { };
        _process.ErrorDataReceived += (_, _) => { };
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();
    }

    private async Task WaitUntilReadyAsync()
    {
        using var httpClient = new HttpClient();
        var deadline = DateTime.UtcNow.AddSeconds(StartupTimeoutSeconds);
        Exception? lastError = null;

        while (DateTime.UtcNow < deadline)
        {
            if (_process is not null && _process.HasExited)
            {
                throw new InvalidOperationException(
                    $"Rezepte.Web exited early with code {_process.ExitCode} while starting up for the browser tests.");
            }

            try
            {
                using var response = await httpClient.GetAsync(BaseAddress);
                return;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                lastError = ex;
                await Task.Delay(ReadinessPollIntervalMilliseconds);
            }
        }

        throw new TimeoutException(
            $"Rezepte.Web did not become ready at '{BaseAddress}' within the startup window.", lastError);
    }

    private async Task RegisterTestUserAsync()
    {
        using var httpClient = new HttpClient { BaseAddress = new Uri(BaseAddress) };
        using var response = await httpClient.PostAsJsonAsync(RegisterEndpoint, new
        {
            Username = TestUsername,
            Password = TestPassword,
            Email = TestEmail
        });
        response.EnsureSuccessStatusCode();
    }

    private static int GetFreeTcpPort()
    {
        var listener = new TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        try
        {
            return ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        }
        finally
        {
            listener.Stop();
        }
    }

    private string? ResolveApplicationDllPath()
    {
        var overrideDirectory = Environment.GetEnvironmentVariable(PublishDirectoryEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(overrideDirectory))
        {
            var overriddenDllPath = Path.Combine(overrideDirectory, "Rezepte.Web.dll");
            if (!File.Exists(overriddenDllPath))
            {
                // The environment variable is a deliberate override, so a missing DLL here is a
                // misconfiguration (typo, stale directory) rather than "not published". Reporting
                // it as a skip would hide the real cause, so this fails loudly instead.
                throw new InvalidOperationException(
                    $"Environment variable '{PublishDirectoryEnvironmentVariable}' is set to '{overrideDirectory}', " +
                    $"but the resolved path '{overriddenDllPath}' does not exist.");
            }

            return overriddenDllPath;
        }

        // A plain `dotnet build` output does not serve static assets correctly through
        // MapStaticAssets() when the application is started outside of `dotnet run`'s
        // development-time asset patching (it returns HTTP 200 with an empty body for
        // files such as js/loadingBar.js). Only a published output serves them correctly,
        // so the browser tests require Rezepte.Web to be published, not just built.
        // The configuration and target framework are derived from this test assembly's own
        // build output directory (bin/<Configuration>/<Tfm>) so the lookup keeps working if
        // the solution ever changes its target framework or is built in Debug.
        var testAssemblyDirectory = new DirectoryInfo(AppContext.BaseDirectory);
        var tfm = testAssemblyDirectory.Name;
        var configuration = testAssemblyDirectory.Parent?.Name ?? "Release";

        var repositoryRoot = RepositoryPaths.FindRepositoryRoot();
        var dllPath = Path.Combine(repositoryRoot.FullName, "Rezepte.Web", "bin", configuration, tfm, "publish", "Rezepte.Web.dll");

        if (File.Exists(dllPath))
        {
            return dllPath;
        }

        ApplicationUnavailableSkipReason =
            $"Rezepte.Web is not published at '{dllPath}'. Run 'dotnet publish Rezepte.Web -c {configuration}' before running the browser tests.";
        return null;
    }
}
