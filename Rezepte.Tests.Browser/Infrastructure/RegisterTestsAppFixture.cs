namespace Rezepte.Tests.Browser.Infrastructure;

/// <summary>
/// Application fixture for the registration browser tests. The shared test user is not seeded,
/// because the application only allows access to the registration page while no user exists
/// (see <c>RedirectToRegisterMiddleware</c>). Each test additionally restarts the application
/// against an empty database via <see cref="RezepteAppFixture.RestartWithEmptyDatabaseAsync"/>.
/// </summary>
public sealed class RegisterTestsAppFixture : RezepteAppFixture
{
    /// <inheritdoc />
    protected override bool SeedSharedTestUser => false;
}
