namespace Rezepte.Tests.TestHelpers;

/// <summary>
/// Central names for the xUnit trait categories used to split the CI test lanes.
/// </summary>
public static class TestCategories
{
    /// <summary>
    /// The trait name under which all lane categories are stored (<c>Category</c>),
    /// matching what <c>dotnet test --filter "Category=..."</c> evaluates.
    /// </summary>
    public const string Category = "Category";

    /// <summary>
    /// Tests that touch real operating-system interfaces (child processes, sockets,
    /// browsers, external toolchains) and are therefore environment-sensitive. They
    /// run in the best-effort lane.
    /// </summary>
    public const string OsInterface = "OsInterface";

    /// <summary>
    /// Tests with a documented environment-caused flake event. Only valid in
    /// combination with <see cref="OsInterface"/> and entitles the test to a single
    /// automated retry in the best-effort lane.
    /// </summary>
    public const string Flaky = "Flaky";
}
