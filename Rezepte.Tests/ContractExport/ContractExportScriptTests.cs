using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Rezepte.Tests.ContractExport;

public sealed class ContractExportScriptTests
{
    private const string ContractVersion = "0.3.0";
    private const string SourceCommit = "0123456789abcdef0123456789abcdef01234567";

    [Fact]
    public void ExportCreatesManifestWithSafeRelativePathsAndHashes()
    {
        using var temp = new TempDirectory();
        var result = RunExport(temp.CreateSubdirectory("export"));

        result.ExitCode.Should().Be(0, result.Error);
        var metadata = ReadJson(result.OutputDirectory, "contract-export.metadata.json");
        var zipPath = result.OutputDirectory.File(metadata.RootElement.GetProperty("artifact").GetString()!);

        File.Exists(zipPath).Should().BeTrue();
        metadata.RootElement.TryGetProperty("artifactPath", out _).Should().BeFalse();
        metadata.RootElement.GetProperty("artifactSha256").GetString().Should().Be(Sha256(zipPath));

        using var archive = ZipFile.OpenRead(zipPath);
        var entries = archive.Entries.Select(entry => entry.FullName).Order(StringComparer.Ordinal).ToArray();
        entries.Should().Contain("contract-export.json");
        entries.Should().Contain("Directory.Build.props");
        entries.Should().Contain("Rezepte.Import.Abstractions/Rezepte.Import.Abstractions.csproj");
        entries.Should().Contain("Rezepte.Import.PluginSdk/Rezepte.Import.PluginSdk.csproj");
        entries.Should().Contain("baselines/0.3.0/Rezepte.Import.Abstractions.dll");
        entries.Should().Contain("baselines/0.3.0/Rezepte.Import.PluginSdk.dll");
        entries.Should().NotContain(path => path.Contains("/bin/", StringComparison.OrdinalIgnoreCase) || path.Contains("/obj/", StringComparison.OrdinalIgnoreCase));
        entries.All(IsSafeRelativePath).Should().BeTrue();

        using var manifestStream = archive.GetEntry("contract-export.json")!.Open();
        using var manifest = JsonDocument.Parse(manifestStream);
        manifest.RootElement.GetProperty("exportFormat").GetString().Should().Be("rezepte-import-contract-v1");
        manifest.RootElement.GetProperty("contractVersion").GetString().Should().Be(ContractVersion);
        manifest.RootElement.GetProperty("sourceCommit").GetString().Should().Be(SourceCommit);

        var files = manifest.RootElement.GetProperty("files").EnumerateArray().ToArray();
        files.Select(file => file.GetProperty("path").GetString()).Should().NotContain("contract-export.json");
        files.Select(file => file.GetProperty("path").GetString()).Should().BeEquivalentTo(entries.Where(path => path != "contract-export.json"));

        var extractionDirectory = temp.CreateSubdirectory("inspect");
        ZipFile.ExtractToDirectory(zipPath, extractionDirectory);
        File.ReadAllText(extractionDirectory.File("Directory.Build.props"))
            .Should().Contain($"<ImportContractVersion>{ContractVersion}</ImportContractVersion>");
        foreach (var file in files)
        {
            var relativePath = file.GetProperty("path").GetString()!;
            file.GetProperty("sha256").GetString().Should().Be(Sha256(extractionDirectory.File(relativePath)));
        }
    }

    [Fact]
    public void ExportIsReproducibleForSameSourcesAndCommit()
    {
        using var temp = new TempDirectory();
        var first = RunExport(temp.CreateSubdirectory("first"));
        var second = RunExport(temp.CreateSubdirectory("second"));

        first.ExitCode.Should().Be(0, first.Error);
        second.ExitCode.Should().Be(0, second.Error);

        var firstMetadata = ReadJson(first.OutputDirectory, "contract-export.metadata.json");
        var secondMetadata = ReadJson(second.OutputDirectory, "contract-export.metadata.json");
        firstMetadata.RootElement.GetProperty("artifactSha256").GetString()
            .Should().Be(secondMetadata.RootElement.GetProperty("artifactSha256").GetString());

        File.ReadAllBytes(first.OutputDirectory.File("rezepte-import-contract-0.3.0.zip"))
            .Should().Equal(File.ReadAllBytes(second.OutputDirectory.File("rezepte-import-contract-0.3.0.zip")));
    }

    [Fact]
    public void ExportedWorkspaceBuildsOutsideRepository()
    {
        using var temp = new TempDirectory();
        var result = RunExport(temp.CreateSubdirectory("export"));
        result.ExitCode.Should().Be(0, result.Error);

        var workspace = temp.CreateSubdirectory("workspace");
        ZipFile.ExtractToDirectory(result.OutputDirectory.File("rezepte-import-contract-0.3.0.zip"), workspace);

        foreach (var project in new[]
        {
            "Rezepte.Import.Abstractions/Rezepte.Import.Abstractions.csproj",
            "Rezepte.Import.PluginSdk/Rezepte.Import.PluginSdk.csproj"
        })
        {
            RunProcess("dotnet", "build", workspace.File(project), "--configuration", "Release")
                .ExitCode.Should().Be(0);
        }
    }

    [Fact]
    public void ExportedBaselineAssembliesUseContractVersion()
    {
        using var temp = new TempDirectory();
        var result = RunExport(temp.CreateSubdirectory("export"));
        result.ExitCode.Should().Be(0, result.Error);

        var workspace = temp.CreateSubdirectory("workspace");
        ZipFile.ExtractToDirectory(result.OutputDirectory.File("rezepte-import-contract-0.3.0.zip"), workspace);

        foreach (var assemblyPath in ContractAssemblies.Select(assembly => workspace.File($"baselines/0.3.0/{assembly}.dll")))
        {
            AssemblyName.GetAssemblyName(assemblyPath).Version.Should().Be(new Version(0, 3, 0, 0));
            FileVersionInfo.GetVersionInfo(assemblyPath).FileVersion.Should().Be(ContractVersion);
        }
    }

    [Fact]
    public void ExportUsesLatestStoredApiCompatBaselineBelowCurrentVersion()
    {
        using var temp = new TempDirectory();
        var fakeRepo = CreateFakeRepoWithContractSources(temp);
        File.WriteAllText(
            fakeRepo.File("Directory.Build.props"),
            File.ReadAllText(fakeRepo.File("Directory.Build.props")).Replace(
                "<ImportContractVersion>0.2.0</ImportContractVersion>",
                "<ImportContractVersion>0.3.0</ImportContractVersion>",
                StringComparison.Ordinal));

        var baselineRoot = temp.CreateSubdirectory("stored-baselines");
        foreach (var baselineVersion in new[] { "0.1.0", "0.2.0" })
        {
            Directory.CreateDirectory(baselineRoot.File(baselineVersion));
            foreach (var assembly in ContractAssemblies)
                File.WriteAllText(baselineRoot.File($"{baselineVersion}/{assembly}.dll"), baselineVersion);
        }

        var apiCompatLog = temp.File("apicompat.log");
        var result = RunExportFromScript(
            fakeRepo.File("scripts/Export-ImportContract.ps1"),
            temp.CreateSubdirectory("out"),
            contractVersion: "0.3.0",
            apiCompatBaselineDirectory: baselineRoot,
            apiCompatToolPath: CreateFakeApiCompatTool(temp),
            environment: new Dictionary<string, string> { ["REZEPTE_APICOMPAT_LOG"] = apiCompatLog });

        result.ExitCode.Should().Be(0, result.Error);
        result.Output.Should().Contain("Using ApiCompat baseline version 0.2.0 for contract version 0.3.0.");
        File.ReadAllLines(apiCompatLog).Should().BeEquivalentTo(ContractAssemblies.Select(assembly =>
            $"{baselineRoot.File($"0.2.0/{assembly}.dll")}|{result.OutputDirectory.File($"_staging/export/baselines/0.3.0/{assembly}.dll")}"));
    }

    [Fact]
    public void ExportFailsFastWhenRequiredPluginSdkPathIsMissing()
    {
        using var temp = new TempDirectory();
        var fakeRepo = temp.CreateSubdirectory("fake-repo");
        Directory.CreateDirectory(fakeRepo.File("scripts"));
        Directory.CreateDirectory(fakeRepo.File("Rezepte.Import.Abstractions"));
        File.Copy(RepositoryRoot.File("scripts/Export-ImportContract.ps1"), fakeRepo.File("scripts/Export-ImportContract.ps1"));
        File.Copy(RepositoryRoot.File("Directory.Build.props"), fakeRepo.File("Directory.Build.props"));
        File.WriteAllText(fakeRepo.File("Rezepte.Import.Abstractions/Rezepte.Import.Abstractions.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\" />");

        var result = RunProcess(
            PowerShellPath,
            "-NoProfile",
            "-ExecutionPolicy",
            "Bypass",
            "-File",
            fakeRepo.File("scripts/Export-ImportContract.ps1"),
            "-OutputDirectory",
            temp.CreateSubdirectory("out"),
            "-ContractVersion",
            ContractVersion,
            "-SourceCommit",
            SourceCommit);

        result.ExitCode.Should().NotBe(0);
        result.Error.Should().Contain("required path is missing: Rezepte.Import.PluginSdk");
    }

    [Fact]
    public void ExportFailsFastWhenDirectoryBuildPropsIsMissing()
    {
        using var temp = new TempDirectory();
        var fakeRepo = temp.CreateSubdirectory("fake-repo");
        Directory.CreateDirectory(fakeRepo.File("scripts"));
        File.Copy(RepositoryRoot.File("scripts/Export-ImportContract.ps1"), fakeRepo.File("scripts/Export-ImportContract.ps1"));

        var result = RunExportFromScript(
            fakeRepo.File("scripts/Export-ImportContract.ps1"),
            temp.CreateSubdirectory("out"));

        result.ExitCode.Should().NotBe(0);
        result.Error.Should().Contain("required path is missing: Directory.Build.props");
    }

    [Fact]
    public void ExportFailsFastWhenUnexpectedContractFileExists()
    {
        using var temp = new TempDirectory();
        var fakeRepo = CreateFakeRepoWithContractSources(temp);
        File.WriteAllText(fakeRepo.File("Rezepte.Import.PluginSdk/notes.txt"), "scratch");

        var result = RunExportFromScript(
            fakeRepo.File("scripts/Export-ImportContract.ps1"),
            temp.CreateSubdirectory("out"));

        result.ExitCode.Should().NotBe(0);
        result.Error.Should().Contain("unexpected contract file is not allowed: Rezepte.Import.PluginSdk/notes.txt");
    }

    [Fact]
    public void ExportFailsFastWhenSensitiveContractFileExists()
    {
        using var temp = new TempDirectory();
        var fakeRepo = CreateFakeRepoWithContractSources(temp);
        File.WriteAllText(fakeRepo.File("Rezepte.Import.Abstractions/signing-key.pfx"), "secret");

        var result = RunExportFromScript(
            fakeRepo.File("scripts/Export-ImportContract.ps1"),
            temp.CreateSubdirectory("out"));

        result.ExitCode.Should().NotBe(0);
        result.Error.Should().Contain("sensitive contract file is not allowed: Rezepte.Import.Abstractions/signing-key.pfx");
    }

    [Fact]
    public void ExportIgnoresBuildArtifactsInContractDirectories()
    {
        using var temp = new TempDirectory();
        var fakeRepo = CreateFakeRepoWithContractSources(temp);
        Directory.CreateDirectory(fakeRepo.File("Rezepte.Import.PluginSdk/bin/Release/net10.0"));
        Directory.CreateDirectory(fakeRepo.File("Rezepte.Import.PluginSdk/obj"));
        File.WriteAllText(fakeRepo.File("Rezepte.Import.PluginSdk/bin/Release/net10.0/local.dll"), "binary");
        File.WriteAllText(fakeRepo.File("Rezepte.Import.PluginSdk/obj/project.assets.json"), "{}");

        var result = RunExportFromScript(
            fakeRepo.File("scripts/Export-ImportContract.ps1"),
            temp.CreateSubdirectory("out"));

        result.ExitCode.Should().Be(0, result.Error);

        using var archive = ZipFile.OpenRead(result.OutputDirectory.File("rezepte-import-contract-0.3.0.zip"));
        archive.Entries.Select(entry => entry.FullName)
            .Should().NotContain(path => path.Contains("/bin/", StringComparison.OrdinalIgnoreCase) || path.Contains("/obj/", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ExportFailsFastWhenParameterVersionDiffersFromDirectoryBuildProps()
    {
        using var temp = new TempDirectory();
        var result = RunExport(temp.CreateSubdirectory("out"), contractVersion: "9.9.9");

        result.ExitCode.Should().NotBe(0);
        result.Error.Should().Contain("-ContractVersion must match Directory.Build.props ImportContractVersion (0.3.0): 9.9.9");
    }

    private static readonly string[] ContractAssemblies =
    [
        "Rezepte.Import.Abstractions",
        "Rezepte.Import.PluginSdk"
    ];

    private static ExportResult RunExport(
        string outputDirectory,
        string? contractVersion = ContractVersion,
        string? apiCompatBaselineDirectory = null,
        string? apiCompatBaselineVersion = null,
        string? apiCompatToolPath = null,
        IReadOnlyDictionary<string, string>? environment = null)
    {
        return RunExportFromScript(
            RepositoryRoot.File("scripts/Export-ImportContract.ps1"),
            outputDirectory,
            contractVersion,
            apiCompatBaselineDirectory,
            apiCompatBaselineVersion,
            apiCompatToolPath,
            environment);
    }

    private static ExportResult RunExportFromScript(
        string scriptPath,
        string outputDirectory,
        string? contractVersion = ContractVersion,
        string? apiCompatBaselineDirectory = null,
        string? apiCompatBaselineVersion = null,
        string? apiCompatToolPath = null,
        IReadOnlyDictionary<string, string>? environment = null)
    {
        var arguments = new List<string>
        {
            "-NoProfile",
            "-ExecutionPolicy",
            "Bypass",
            "-File",
            scriptPath,
            "-OutputDirectory",
            outputDirectory,
            "-SourceCommit",
            SourceCommit
        };

        if (contractVersion is not null)
        {
            arguments.Add("-ContractVersion");
            arguments.Add(contractVersion);
        }

        arguments.Add("-ApiCompatBaselineDirectory");
        arguments.Add(apiCompatBaselineDirectory ?? outputDirectory.File("__missing-api-compat-baselines"));

        if (apiCompatBaselineVersion is not null)
        {
            arguments.Add("-ApiCompatBaselineVersion");
            arguments.Add(apiCompatBaselineVersion);
        }

        if (apiCompatToolPath is not null)
        {
            arguments.Add("-ApiCompatToolPath");
            arguments.Add(apiCompatToolPath);
        }

        var result = RunProcess(
            PowerShellPath,
            environment,
            arguments.ToArray());

        return new ExportResult(result.ExitCode, outputDirectory, result.Output, result.Error);
    }

    private static string CreateFakeApiCompatTool(TempDirectory temp)
    {
        var toolPath = temp.File("fake-apicompat.ps1");
        File.WriteAllText(
            toolPath,
            """
            param(
                [Alias('l')]
                [string]$LeftAssembly,

                [Alias('r')]
                [string]$RightAssembly
            )

            Add-Content -LiteralPath $env:REZEPTE_APICOMPAT_LOG -Value "$LeftAssembly|$RightAssembly"
            exit 0
            """);

        return toolPath;
    }

    private static string CreateFakeRepoWithContractSources(TempDirectory temp)
    {
        var fakeRepo = temp.CreateSubdirectory("fake-repo");
        Directory.CreateDirectory(fakeRepo.File("scripts"));
        File.Copy(RepositoryRoot.File("scripts/Export-ImportContract.ps1"), fakeRepo.File("scripts/Export-ImportContract.ps1"));
        CopyRepositoryFile("Directory.Build.props", fakeRepo);
        CopyRepositoryContractDirectory("Rezepte.Import.Abstractions", fakeRepo);
        CopyRepositoryContractDirectory("Rezepte.Import.PluginSdk", fakeRepo);

        return fakeRepo;
    }

    private static void CopyRepositoryContractDirectory(string relativeDirectory, string fakeRepo)
    {
        foreach (var sourceFile in Directory.EnumerateFiles(RepositoryRoot.File(relativeDirectory), "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(RepositoryRoot, sourceFile).Replace(Path.DirectorySeparatorChar, '/');
            if (relativePath.Split('/').Any(segment => segment.Equals("bin", StringComparison.OrdinalIgnoreCase) || segment.Equals("obj", StringComparison.OrdinalIgnoreCase)))
                continue;

            CopyRepositoryFile(relativePath, fakeRepo);
        }
    }

    private static void CopyRepositoryFile(string relativePath, string fakeRepo)
    {
        var target = fakeRepo.File(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        File.Copy(RepositoryRoot.File(relativePath), target);
    }

    private static ProcessResult RunProcess(string fileName, params string[] arguments) =>
        RunProcess(fileName, null, arguments);

    private static ProcessResult RunProcess(string fileName, IReadOnlyDictionary<string, string>? environment, params string[] arguments)
    {
        var profileRoot = Path.Combine(Path.GetTempPath(), "rezepte-contract-export-process", Guid.NewGuid().ToString("N"));
        var appData = Path.Combine(profileRoot, "AppData");
        var packages = Path.Combine(profileRoot, "packages");
        Directory.CreateDirectory(appData);
        Directory.CreateDirectory(packages);

        var startInfo = new ProcessStartInfo(fileName)
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };
        startInfo.Environment["HOME"] = profileRoot;
        startInfo.Environment["APPDATA"] = appData;
        startInfo.Environment["USERPROFILE"] = profileRoot;
        startInfo.Environment["NUGET_PACKAGES"] = packages;
        if (environment is not null)
        {
            foreach (var (key, value) in environment)
                startInfo.Environment[key] = value;
        }

        foreach (var argument in arguments)
            startInfo.ArgumentList.Add(argument);

        try
        {
            using var process = Process.Start(startInfo)!;
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            return new ProcessResult(process.ExitCode, output, error);
        }
        finally
        {
            if (Directory.Exists(profileRoot))
                Directory.Delete(profileRoot, true);
        }
    }

    private static JsonDocument ReadJson(string directory, string fileName) =>
        JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, fileName)));

    private static string Sha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private static bool IsSafeRelativePath(string path)
    {
        if (Path.IsPathRooted(path) || path.Contains('\\') || path.Contains(':'))
            return false;

        return path.Split('/').All(segment => segment is not "" and not "." and not "..");
    }

    private static string PowerShellPath => FindOnPath("pwsh") ?? FindOnPath("powershell") ?? "pwsh";

    private static string? FindOnPath(string executableName)
    {
        var path = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(path))
            return null;

        var candidates = new[] { string.Empty }
            .Concat(Environment.GetEnvironmentVariable("PATHEXT")?.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries) ?? [])
            .Distinct(StringComparer.OrdinalIgnoreCase);
        foreach (var directory in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            foreach (var extension in candidates)
            {
                var candidate = Path.Combine(directory, executableName + extension);
                if (File.Exists(candidate))
                    return candidate;
            }
        }

        return null;
    }

    private static string RepositoryRoot
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Rezepte.sln")))
                directory = directory.Parent;

            return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root could not be located.");
        }
    }

    private sealed record ExportResult(int ExitCode, string OutputDirectory, string Output, string Error);

    private sealed record ProcessResult(int ExitCode, string Output, string Error);

    private sealed class TempDirectory : IDisposable
    {
        private readonly string _path = Path.Combine(Path.GetTempPath(), "rezepte-contract-export-tests", Guid.NewGuid().ToString("N"));

        public TempDirectory()
        {
            Directory.CreateDirectory(_path);
        }

        public string CreateSubdirectory(string name)
        {
            var path = File(name);
            Directory.CreateDirectory(path);
            return path;
        }

        public string File(string relativePath) => Path.Combine(_path, relativePath.Replace('/', Path.DirectorySeparatorChar));

        public void Dispose()
        {
            if (Directory.Exists(_path))
                Directory.Delete(_path, true);
        }
    }
}

internal static class ContractExportPathExtensions
{
    public static string File(this string directory, string relativePath) =>
        Path.Combine(directory, relativePath.Replace('/', Path.DirectorySeparatorChar));
}
