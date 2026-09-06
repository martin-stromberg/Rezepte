using FluentAssertions;
using Rezepte.Tests.TestHelpers;
using Xunit;

namespace Rezepte.Tests.Deployment;

/// <summary>
/// Class representing the csproj credential copy tests.
/// </summary>
public class CsprojCredentialCopyTests
{
    /// <summary>
    /// Csproj does not copy credential files to output.
    /// </summary>
    [Fact]
    public void Csproj_DoesNotCopyCredentialFiles_ToOutput()
    {
        var csproj = RepositoryPaths.ReadRepositoryFile("Rezepte.Web", "Rezepte.Web.csproj");

        csproj.Should().NotContain("google.application-credentials.json");
        csproj.Should().NotContain("google.gemini.api-key.json");
    }
}
