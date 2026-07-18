using FluentAssertions;
using Rezepte.Web.Services.Import.Plugins;
using Xunit;

namespace Rezepte.Tests.Services.Import;

public sealed class PluginSourceReleaseTests
{
    [Fact]
    public void GitHubRepository_Parse_ShouldCanonicalizeHttpsGitHubRepository()
    {
        var repository = GitHubRepository.Parse("https://github.com/example/my-plugin.git");

        repository.Owner.Should().Be("example");
        repository.Repository.Should().Be("my-plugin");
        repository.CanonicalUrl.Should().Be("https://github.com/example/my-plugin");
    }

    [Fact]
    public void GitHubReleaseInfo_FindZipAsset_ShouldAllowVariableZipAssetNames()
    {
        var release = new GitHubReleaseInfo(1, "v1.2.3",
        [
            new GitHubReleaseAsset(10, "notes.txt", "https://example.invalid/notes"),
            new GitHubReleaseAsset(11, "latest-release.zip", "https://example.invalid/latest-release.zip")
        ]);

        release.FindZipAsset().Should().NotBeNull();
        release.FindZipAsset()!.Name.Should().Be("latest-release.zip");
    }
}
