using System;
using System.IO;
using FluentAssertions;
using Xunit;

namespace Rezepte.Tests.Deployment;

public class CsprojCredentialCopyTests
{
    [Fact]
    public void Csproj_DoesNotCopyCredentialFiles_ToOutput()
    {
        var csproj = ReadRepositoryFile("Rezepte.Web", "Rezepte.Web.csproj");

        csproj.Should().NotContain("google.application-credentials.json");
        csproj.Should().NotContain("google.gemini.api-key.json");
    }

    private static string ReadRepositoryFile(params string[] relativePathParts)
    {
        var directory = FindRepositoryRoot();

        var candidate = Path.Combine(directory.FullName, Path.Combine(relativePathParts));
        if (File.Exists(candidate))
        {
            return File.ReadAllText(candidate);
        }

        throw new FileNotFoundException(
            $"Could not locate repository file '{Path.Combine(relativePathParts)}' from '{directory.FullName}'.");
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Rezepte.sln")))
            {
                return directory;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException(
            $"Could not locate repository root from '{AppContext.BaseDirectory}'.");
    }
}
