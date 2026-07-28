using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using FluentAssertions;
using Rezepte.Tests.TestHelpers;
using Xunit;

namespace Rezepte.Tests.Deployment;

public class DeploymentDocumentationTests
{
    [Fact]
    public void InstallDocumentation_ShouldDescribeRuntimeRequirementsAndCorrectEntrypoint()
    {
        var install = RepositoryPaths.ReadRepositoryFile("Docs", "install.md");

        install.Should().NotContain("Rezepte.dll");
        install.Should().Contain("Rezepte.Web.dll");
        install.Should().Contain("Rezepte.Web");
        install.Should().Contain("dotnet --info");
        install.Should().Contain("Microsoft.NETCore.App");
        install.Should().Contain("Microsoft.AspNetCore.App");
        install.Should().Contain("System.Runtime.Serialization.Primitives.dll");
        install.Should().Contain("--self-contained false");
        install.Should().Contain("--self-contained true");
        install.Should().Contain("chmod +x /var/www/rezepte/Rezepte.Web");
    }

    [Fact]
    public void ReadmeDeploymentSection_ShouldPointToRuntimeCheckedDeploymentOptions()
    {
        var readme = RepositoryPaths.ReadRepositoryFile("README.md");

        readme.Should().Contain("Docs/install.md");
        readme.Should().Contain(".NET-10-Shared-Frameworks");
        readme.Should().Contain("Microsoft.NETCore.App");
        readme.Should().Contain("Microsoft.AspNetCore.App");
        readme.Should().Contain("self-contained");
    }

    [Fact]
    public void FrameworkDependentLinuxPublish_ShouldProduceDocumentedEntrypointAndRuntimeFrameworks()
    {
        var repositoryRoot = RepositoryPaths.FindRepositoryRoot();
        var publishDirectory = Path.Combine(
            Path.GetTempPath(),
            "rezepte-publish-contract",
            Guid.NewGuid().ToString("N"));

        try
        {
            var result = RunDotnet(
                "publish Rezepte.Web -c Release -f net10.0 -r linux-x64 --self-contained false",
                repositoryRoot,
                publishDirectory);

            result.ExitCode.Should().Be(
                0,
                "dotnet publish should keep the documented framework-dependent linux-x64 contract.{0}stdout:{0}{1}{0}stderr:{0}{2}",
                Environment.NewLine,
                result.StandardOutput,
                result.StandardError);

            File.Exists(Path.Combine(publishDirectory, "Rezepte.Web.dll")).Should().BeTrue();

            var runtimeConfigPath = Path.Combine(publishDirectory, "Rezepte.Web.runtimeconfig.json");
            File.Exists(runtimeConfigPath).Should().BeTrue();

            using var runtimeConfig = JsonDocument.Parse(File.ReadAllText(runtimeConfigPath));
            var frameworks = runtimeConfig.RootElement
                .GetProperty("runtimeOptions")
                .GetProperty("frameworks")
                .EnumerateArray()
                .Select(framework => new RuntimeFramework(
                    framework.GetProperty("name").GetString() ?? string.Empty,
                    framework.GetProperty("version").GetString() ?? string.Empty))
                .ToArray();

            frameworks.Should().Contain(framework =>
                framework.Name == "Microsoft.NETCore.App" &&
                framework.Version.StartsWith("10.", StringComparison.Ordinal));
            frameworks.Should().Contain(framework =>
                framework.Name == "Microsoft.AspNetCore.App" &&
                framework.Version.StartsWith("10.", StringComparison.Ordinal));
        }
        finally
        {
            if (Directory.Exists(publishDirectory))
            {
                Directory.Delete(publishDirectory, recursive: true);
            }
        }
    }

    private static DotnetResult RunDotnet(string arguments, DirectoryInfo workingDirectory, string publishDirectory)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"{arguments} -o \"{publishDirectory}\"",
            WorkingDirectory = workingDirectory.FullName,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Could not start dotnet publish.");

        var standardOutput = process.StandardOutput.ReadToEnd();
        var standardError = process.StandardError.ReadToEnd();

        process.WaitForExit();

        return new DotnetResult(process.ExitCode, standardOutput, standardError);
    }

    private sealed record DotnetResult(int ExitCode, string StandardOutput, string StandardError);

    private sealed record RuntimeFramework(string Name, string Version);
}
