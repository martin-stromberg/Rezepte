using FluentAssertions;
using Rezepte.Tests.TestHelpers;
using Xunit;

namespace Rezepte.Tests.Deployment;

public class CsprojCredentialCopyTests
{
    [Fact]
    public void Csproj_DoesNotCopyCredentialFiles_ToOutput()
    {
        var csproj = RepositoryPaths.ReadRepositoryFile("Rezepte.Web", "Rezepte.Web.csproj");

        csproj.Should().NotContain("google.application-credentials.json");
        csproj.Should().NotContain("google.gemini.api-key.json");
    }
}
