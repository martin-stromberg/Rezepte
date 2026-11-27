using System.Diagnostics;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text;
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
    private const int StartupTimeoutSeconds = 120;
    private const int ReadinessPollIntervalMilliseconds = 250;
    private const int ShutdownGraceMilliseconds = 5000;
    private const int MaxStartAttempts = 3;
    private const int OutputTailCapacity = 100;

    private readonly object _outputLock = new();
    private readonly Queue<string> _stdoutTail = new();
    private readonly Queue<string> _stderrTail = new();

    private Process? _process;
    private string? _tempDirectory;
    private string? _applicationDllPath;
    private string? _databasePath;
    private string? _processLogFilePath;
    private StreamWriter? _processLogWriter;

    /// <summary>
    /// Gets the base URL of the started application.
    /// </summary>
    public string BaseAddress { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the path of the SQLite database file the currently running application process
    /// uses, or <see langword="null"/> when no database exists at the moment.
    /// </summary>
    public string? DatabasePath => _databasePath;

    /// <summary>
    /// Gets a value indicating whether the application process was started successfully.
    /// </summary>
    public bool ApplicationAvailable { get; private set; }

    /// <summary>
    /// Gets the reason given to xUnit when <see cref="ApplicationAvailable"/> is <c>false</c>.
    /// </summary>
    public string ApplicationUnavailableSkipReason { get; private set; } = "Rezepte.Web is not published.";

    /// <summary>
    /// Gets the directory the application process is started from — the publish output that
    /// serves as its content root — or <see langword="null"/> before initialization.
    /// </summary>
    protected string? ApplicationDirectory =>
        _applicationDllPath is null ? null : Path.GetDirectoryName(_applicationDllPath);

    /// <summary>
    /// Starts the published web application, waits until it is reachable, and registers the test user.
    /// </summary>
    /// <returns>A task that represents the asynchronous initialization operation.</returns>
    public async ValueTask InitializeAsync()
    {
        var applicationDllPath = ResolveApplicationDllPath();
        if (applicationDllPath is null)
        {
            ApplicationAvailable = false;
            return;
        }

        _applicationDllPath = applicationDllPath;
        var databasePath = CreateTemporaryDatabase();
        try
        {
            await StartApplicationProcessWithRetryAsync(applicationDllPath, databasePath, "startup");
            if (SeedSharedTestUser)
            {
                await RegisterTestUserAsync();
            }

            ApplicationAvailable = true;
        }
        catch
        {
            await DisposeAsync();
            throw;
        }
    }

    /// <summary>
    /// Gets a value indicating whether the shared test user (<see cref="TestUsername"/>) is
    /// registered during <see cref="InitializeAsync"/>. Fixtures that need to exercise the
    /// registration flow itself override this with <c>false</c>, because the application only
    /// allows registration while no user exists.
    /// </summary>
    protected virtual bool SeedSharedTestUser => true;

    /// <summary>
    /// Stops the application process and restarts it against a newly created, empty database.
    /// After the restart no user exists, so the registration page is reachable again.
    /// </summary>
    /// <returns>A task that represents the asynchronous restart operation.</returns>
    public async Task RestartWithEmptyDatabaseAsync()
    {
        var applicationDllPath = _applicationDllPath
            ?? throw new InvalidOperationException("The application fixture has not been initialized.");

        StopApplicationProcess();
        DeleteTemporaryDatabase();

        var databasePath = CreateTemporaryDatabase();
        await StartApplicationProcessWithRetryAsync(applicationDllPath, databasePath, "restart");
    }

    /// <summary>
    /// Stops the application process and deletes the temporary database.
    /// </summary>
    /// <returns>A completed task.</returns>
    public virtual ValueTask DisposeAsync()
    {
        StopApplicationProcess();
        DeleteTemporaryDatabase();
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Returns the environment variables that should be added to the application process.
    /// </summary>
    /// <returns>A read-only dictionary of environment variable names and values.</returns>
    protected virtual IReadOnlyDictionary<string, string?> GetEnvironmentOverrides()
    {
        return new Dictionary<string, string?>();
    }

    private void StopApplicationProcess()
    {
        try
        {
            if (_process is not null && !_process.HasExited)
            {
                // Best effort like TempDirectoryCleanup: the child may exit between the
                // HasExited check and Kill (InvalidOperationException / Win32Exception).
                // A cleanup failure propagating through DisposeAsync would mask the
                // real test result — the exact problem the never-throws contract avoids.
                _process.Kill(entireProcessTree: true);
                if (!_process.WaitForExit(ShutdownGraceMilliseconds))
                {
                    Console.Error.WriteLine(
                        $"RezepteAppFixture: the application process was still running "
                        + $"{ShutdownGraceMilliseconds}ms after Kill — proceeding anyway.");
                }
            }
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(
                $"RezepteAppFixture: could not stop the application process: {error.Message}");
        }
        finally
        {
            _process?.Dispose();
            lock (_outputLock)
            {
                // _process is nulled under the lock so late async output callbacks of
                // the killed process fail the ReferenceEquals guard in CaptureOutputLine
                // and cannot write old-attempt lines into the next attempt's log file.
                _process = null;
                _processLogWriter?.Dispose();
                _processLogWriter = null;
            }
        }
    }

    private void DeleteTemporaryDatabase()
    {
        if (_tempDirectory is not null)
        {
            // No kill safety net is passed here on purpose: both callers run
            // StopApplicationProcess() first, so _process is always null at this point.
            TempDirectoryCleanup.DeleteBestEffort(_tempDirectory);
        }

        _tempDirectory = null;
        _databasePath = null;
    }

    private string CreateTemporaryDatabase()
    {
        _tempDirectory = Directory.CreateTempSubdirectory("rezepte-browser-tests-").FullName;
        _databasePath = Path.Combine(_tempDirectory, "rezepte-browser-test.db");
        return _databasePath;
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

        var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Could not start the Rezepte.Web process for the browser tests.");

        // _process is registered under _outputLock before the output drain starts, so the
        // ReferenceEquals guard in CaptureOutputLine sees the same process the callbacks of
        // this attempt report as their sender.
        lock (_outputLock)
        {
            _process = process;
        }

        // The redirected output/error pipes must be drained continuously; otherwise the OS pipe buffer
        // fills up once the application logs enough (e.g. one line per incoming request) and the child
        // process blocks on its next Console.Write call, hanging the whole application. Drained lines
        // are kept in a bounded tail (and a full log file) so a startup failure is diagnosable.
        process.OutputDataReceived += (sender, args) => CaptureOutputLine(sender as Process, _stdoutTail, args.Data);
        process.ErrorDataReceived += (sender, args) => CaptureOutputLine(sender as Process, _stderrTail, args.Data);
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
    }

    private async Task StartApplicationProcessWithRetryAsync(string applicationDllPath, string databasePath, string phase)
    {
        for (var attempt = 1; attempt <= MaxStartAttempts; attempt++)
        {
            StartProcessLogFile(phase, attempt);
            StartApplicationProcess(applicationDllPath, databasePath);

            try
            {
                await WaitUntilReadyAsync(phase);
                return;
            }
            catch (Exception error) when (error is TimeoutException or InvalidOperationException)
            {
                // A lost port TOCTOU race surfaces as an early child exit; retrying on a
                // freshly picked port heals it without any product-code change.
                StopApplicationProcess();
                if (attempt == MaxStartAttempts)
                {
                    throw new InvalidOperationException(
                        $"Rezepte.Web did not start successfully after {MaxStartAttempts} attempts during {phase}.",
                        error);
                }

                Console.Error.WriteLine(
                    $"Rezepte.Web start attempt {attempt}/{MaxStartAttempts} failed during {phase} "
                    + $"— retrying on a fresh port. {error.Message}");
            }
        }
    }

    private async Task WaitUntilReadyAsync(string phase)
    {
        using var httpClient = new HttpClient();
        var started = DateTime.UtcNow;
        var deadline = started.AddSeconds(StartupTimeoutSeconds);
        Exception? lastError = null;

        while (DateTime.UtcNow < deadline)
        {
            if (_process is not null && _process.HasExited)
            {
                throw new InvalidOperationException(
                    $"Rezepte.Web exited early with code {_process.ExitCode} during {phase} "
                    + $"({DescribeElapsed(started)} elapsed).{DescribeOutputTail()}");
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
            $"Rezepte.Web did not become ready at '{BaseAddress}' within {StartupTimeoutSeconds}s "
            + $"during {phase} ({DescribeElapsed(started)} elapsed).{DescribeOutputTail()}",
            lastError);
    }

    private void CaptureOutputLine(Process? source, Queue<string> tail, string? line)
    {
        if (line is null)
        {
            return;
        }

        lock (_outputLock)
        {
            // The async pipe drain of a killed process can outlive WaitForExit; a late
            // callback of a previous attempt must not write old-attempt lines into the
            // new attempt's tail or log file.
            if (!ReferenceEquals(source, _process))
            {
                return;
            }

            while (tail.Count >= OutputTailCapacity)
            {
                tail.Dequeue();
            }

            tail.Enqueue(line);
            _processLogWriter?.WriteLine(line);
        }
    }

    private void StartProcessLogFile(string phase, int attempt)
    {
        lock (_outputLock)
        {
            // Every attempt gets its own diagnostics: leftover tail lines of the previous
            // attempt's process would be mislabeled as the "last output" of this attempt.
            _stdoutTail.Clear();
            _stderrTail.Clear();
            _processLogWriter?.Dispose();
            _processLogWriter = null;
            try
            {
                var logDirectory = Path.Combine(
                    RepositoryPaths.FindRepositoryRoot().FullName, "browser-app-logs");
                Directory.CreateDirectory(logDirectory);
                _processLogFilePath = Path.Combine(
                    logDirectory,
                    $"rezepte-app-{phase}-attempt{attempt}-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.log");
                _processLogWriter = new StreamWriter(_processLogFilePath) { AutoFlush = true };
            }
            catch (Exception)
            {
                // The log file is a diagnostic bonus; a failure to create it must not stop the tests.
                _processLogWriter = null;
                _processLogFilePath = null;
            }
        }
    }

    private string DescribeOutputTail()
    {
        string[] stdoutLines;
        string[] stderrLines;
        string? processLogFilePath;
        lock (_outputLock)
        {
            stdoutLines = _stdoutTail.ToArray();
            stderrLines = _stderrTail.ToArray();
            processLogFilePath = _processLogFilePath;
        }

        var builder = new StringBuilder(" Last child process output:");
        if (stdoutLines.Length == 0 && stderrLines.Length == 0)
        {
            builder.Append(" <none>");
        }

        if (stdoutLines.Length > 0)
        {
            builder.Append(" stdout: ").Append(string.Join(" | ", stdoutLines));
        }

        if (stderrLines.Length > 0)
        {
            builder.Append(" stderr: ").Append(string.Join(" | ", stderrLines));
        }

        if (processLogFilePath is not null)
        {
            builder.Append($" (full output: {processLogFilePath})");
        }

        return builder.ToString();
    }

    private static string DescribeElapsed(DateTime started)
        => $"{(DateTime.UtcNow - started).TotalSeconds:F1}s";

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
