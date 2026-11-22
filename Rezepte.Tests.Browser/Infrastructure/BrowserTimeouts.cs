namespace Rezepte.Tests.Browser.Infrastructure;

/// <summary>
/// Central timeout constants shared by the browser test page objects.
/// </summary>
internal static class BrowserTimeouts
{
    /// <summary>
    /// The default navigation timeout applied to every browser context via
    /// <c>context.SetDefaultNavigationTimeout</c>: generous enough for a loaded CI runner
    /// (up to three application child processes plus Chromium can run in parallel), while
    /// staying well below the host-side startup deadline of <see cref="RezepteAppFixture"/>.
    /// Explicit per-call timeout options keep precedence over this default.
    /// </summary>
    internal const int NavigationTimeoutMilliseconds = 60000;
}
