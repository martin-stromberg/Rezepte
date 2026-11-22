using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Rezepte.Tests.TestHelpers;
using Rezepte.Web;

namespace Rezepte.Tests.TestSupport;

/// <summary>
/// A <see cref="WebApplicationFactory{TEntryPoint}"/> that runs the application in the
/// Production environment against a temporary SQLite database file. The database file is
/// deleted when the factory is disposed.
/// </summary>
public class SqliteFileWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databasePath;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteFileWebApplicationFactory"/> class.
    /// </summary>
    /// <param name="databasePath">The path of the temporary SQLite database file.</param>
    public SqliteFileWebApplicationFactory(string databasePath)
    {
        _databasePath = databasePath;
    }

    /// <summary>
    /// Creates a client that does not follow redirects.
    /// </summary>
    /// <returns>The anonymous client.</returns>
    public HttpClient CreateAnonymousClient()
        => CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting(WebHostDefaults.EnvironmentKey, "Production");
        builder.UseSetting("Jwt:Key", "integration-test-signing-key-0123456789");
        builder.UseSetting("ConnectionStrings:Default", $"Data Source={_databasePath}");
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
        {
            return;
        }

        SqliteConnection.ClearAllPools();
        TempDirectoryCleanup.DeleteFileBestEffort(_databasePath);
    }
}
