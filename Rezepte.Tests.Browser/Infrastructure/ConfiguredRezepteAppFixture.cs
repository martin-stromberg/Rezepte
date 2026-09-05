namespace Rezepte.Tests.Browser.Infrastructure;

/// <summary>
/// Starts the Rezepte.Web application with a fixed set of environment variable overrides,
/// so a derived, parameterless fixture class can apply a non-default <c>LoadingBar</c> configuration
/// in a single line instead of repeating the <see cref="RezepteAppFixture.GetEnvironmentOverrides"/> override.
/// </summary>
public abstract class ConfiguredRezepteAppFixture : RezepteAppFixture
{
    private readonly IReadOnlyDictionary<string, string?> _environmentOverrides;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfiguredRezepteAppFixture"/> class.
    /// </summary>
    /// <param name="environmentOverrides">The environment variables to apply to the application process.</param>
    protected ConfiguredRezepteAppFixture(IReadOnlyDictionary<string, string?> environmentOverrides)
    {
        _environmentOverrides = environmentOverrides;
    }

    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, string?> GetEnvironmentOverrides() => _environmentOverrides;
}
