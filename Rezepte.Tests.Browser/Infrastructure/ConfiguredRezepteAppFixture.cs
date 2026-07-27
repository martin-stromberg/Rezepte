namespace Rezepte.Tests.Browser.Infrastructure;

/// <summary>
/// Starts the Rezepte.Web application with a fixed set of environment variable overrides,
/// so a derived, parameterless fixture class can apply a non-default <c>LoadingBar</c> configuration
/// in a single line instead of repeating the <see cref="RezepteAppFixture.GetEnvironmentOverrides"/> override.
/// </summary>
public abstract class ConfiguredRezepteAppFixture(IReadOnlyDictionary<string, string?> environmentOverrides) : RezepteAppFixture
{
    protected override IReadOnlyDictionary<string, string?> GetEnvironmentOverrides() => environmentOverrides;
}
