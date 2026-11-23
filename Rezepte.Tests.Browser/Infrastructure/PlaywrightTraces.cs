using Rezepte.Tests.TestHelpers;

namespace Rezepte.Tests.Browser.Infrastructure;

/// <summary>
/// Resolves the deterministic Playwright trace directory at the repository root.
/// The test host's working directory is the <c>bin/</c> output directory, so writing
/// traces relative to it hides them from the CI artifact upload, which collects
/// <c>playwright-traces/</c> from the repository root.
/// </summary>
internal static class PlaywrightTraces
{
    /// <summary>
    /// Returns the trace directory <c>&lt;repo root&gt;/playwright-traces</c>, creating
    /// it when necessary.
    /// </summary>
    /// <returns>The absolute trace directory path.</returns>
    internal static string GetTraceDirectory()
    {
        var directory = Path.Combine(
            RepositoryPaths.FindRepositoryRoot().FullName, "playwright-traces");
        Directory.CreateDirectory(directory);
        return directory;
    }

    /// <summary>
    /// Returns a convention-conformant trace file path
    /// (<c>{prefix}-{yyyyMMdd-HHmmss}-{Guid:N}.zip</c>) inside the trace directory.
    /// </summary>
    /// <param name="prefix">The file name prefix identifying the producing page object.</param>
    /// <returns>The absolute trace file path.</returns>
    internal static string GetTraceFilePath(string prefix)
    {
        var fileName = $"{prefix}-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.zip";
        return Path.Combine(GetTraceDirectory(), fileName);
    }
}
