param(
    [string]$OutputDirectory = "artifacts/contract-export",
    [string]$ContractVersion,
    [string]$SourceCommit,
    [string]$ApiCompatBaselineDirectory = "contract-baselines/import-contract",
    [string]$ApiCompatBaselineVersion,
    [string]$ApiCompatToolPath
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$outputRoot = if ([System.IO.Path]::IsPathRooted($OutputDirectory)) { $OutputDirectory } else { Join-Path $repoRoot $OutputDirectory }
$stagingRoot = Join-Path $outputRoot "_staging"
$exportRoot = Join-Path $stagingRoot "export"
$metadataPath = Join-Path $outputRoot "contract-export.metadata.json"
$contractProjectRoots = @("Rezepte.Import.Abstractions", "Rezepte.Import.PluginSdk")
$allowedContractFiles = @(
    "Directory.Build.props",
    "Rezepte.Import.Abstractions/ICollectionImportHandler.cs",
    "Rezepte.Import.Abstractions/IImportHandler.cs",
    "Rezepte.Import.Abstractions/IImportInteraction.cs",
    "Rezepte.Import.Abstractions/IImportPlugin.cs",
    "Rezepte.Import.Abstractions/IInteractiveImportHandler.cs",
    "Rezepte.Import.Abstractions/ImportCollectionModels.cs",
    "Rezepte.Import.Abstractions/ImportedImage.cs",
    "Rezepte.Import.Abstractions/ImportedIngredient.cs",
    "Rezepte.Import.Abstractions/ImportedRecipe.cs",
    "Rezepte.Import.Abstractions/ImportedRecipeStep.cs",
    "Rezepte.Import.Abstractions/ImportResult.cs",
    "Rezepte.Import.Abstractions/PluginUsabilityIssue.cs",
    "Rezepte.Import.Abstractions/PluginUsabilityResult.cs",
    "Rezepte.Import.Abstractions/Rezepte.Import.Abstractions.csproj",
    "Rezepte.Import.PluginSdk/ImportParserBase.cs",
    "Rezepte.Import.PluginSdk/ParsedIngredient.cs",
    "Rezepte.Import.PluginSdk/Rezepte.Import.PluginSdk.csproj",
    "Rezepte.Import.PluginSdk/UrlHelpers.cs"
)
$sensitiveFileExtensions = @(".cer", ".crt", ".der", ".env", ".key", ".p12", ".pem", ".pfx", ".snk", ".user")
$sensitiveFileNames = @(
    ".env",
    "appsettings.development.json",
    "appsettings.production.json",
    "appsettings.local.json",
    "secrets.json"
)

function Fail([string]$Message) {
    throw "Contract export failed: $Message"
}

function Get-RelativePath([string]$BasePath, [string]$FullPath) {
    [System.IO.Path]::GetRelativePath($BasePath, $FullPath).Replace('\', '/')
}

function Assert-SafeRelativePath([string]$Path) {
    if ([string]::IsNullOrWhiteSpace($Path)) {
        Fail "empty path is not allowed"
    }

    if ($Path -match '^[A-Za-z]:') {
        Fail "absolute Windows path is not allowed: $Path"
    }

    if ($Path.StartsWith("/", [System.StringComparison]::Ordinal) -or $Path.StartsWith("\", [System.StringComparison]::Ordinal)) {
        Fail "absolute path is not allowed: $Path"
    }

    if ($Path.Contains("\")) {
        Fail "backslashes are not allowed in ZIP paths: $Path"
    }

    $segments = $Path.Split('/')
    foreach ($segment in $segments) {
        if ($segment -eq "." -or $segment -eq "..") {
            Fail "unsafe path segment is not allowed: $Path"
        }

        if ($segment.Equals("bin", [System.StringComparison]::OrdinalIgnoreCase) -or $segment.Equals("obj", [System.StringComparison]::OrdinalIgnoreCase)) {
            Fail "build artifact path is not allowed: $Path"
        }
    }
}

function Get-Sha256([string]$Path) {
    $stream = [System.IO.File]::OpenRead($Path)
    try {
        $sha = [System.Security.Cryptography.SHA256]::Create()
        try {
            ($sha.ComputeHash($stream) | ForEach-Object { $_.ToString("x2") }) -join ""
        }
        finally {
            $sha.Dispose()
        }
    }
    finally {
        $stream.Dispose()
    }
}

function Copy-ContractFile([string]$RelativePath) {
    Assert-SafeRelativePath $RelativePath
    $source = Join-Path $repoRoot $RelativePath
    $target = Join-Path $exportRoot $RelativePath
    $targetDirectory = Split-Path -Parent $target
    if (-not [System.IO.Directory]::Exists($targetDirectory)) {
        [System.IO.Directory]::CreateDirectory($targetDirectory) | Out-Null
    }

    [System.IO.File]::Copy($source, $target, $true)
}

function Test-BuildArtifactRelativePath([string]$Path) {
    $segments = $Path.Split('/')
    foreach ($segment in $segments) {
        if ($segment.Equals("bin", [System.StringComparison]::OrdinalIgnoreCase) -or $segment.Equals("obj", [System.StringComparison]::OrdinalIgnoreCase)) {
            return $true
        }
    }

    return $false
}

function Test-SensitiveContractFile([string]$Path) {
    $fileName = [System.IO.Path]::GetFileName($Path)
    foreach ($sensitiveName in $sensitiveFileNames) {
        if ($fileName.Equals($sensitiveName, [System.StringComparison]::OrdinalIgnoreCase)) {
            return $true
        }
    }

    $extension = [System.IO.Path]::GetExtension($Path)
    foreach ($sensitiveExtension in $sensitiveFileExtensions) {
        if ($extension.Equals($sensitiveExtension, [System.StringComparison]::OrdinalIgnoreCase)) {
            return $true
        }
    }

    return $false
}

function Assert-AllowedContractFiles([string[]]$AllowedFiles) {
    $allowed = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
    foreach ($file in $AllowedFiles) {
        Assert-SafeRelativePath $file
        [void]$allowed.Add($file)
    }

    foreach ($root in $contractProjectRoots) {
        $rootPath = Join-Path $repoRoot $root
        $files = Get-ChildItem -LiteralPath $rootPath -Recurse -File -Force |
            ForEach-Object { Get-RelativePath $repoRoot $_.FullName } |
            Sort-Object

        foreach ($file in $files) {
            if (Test-BuildArtifactRelativePath $file) {
                continue
            }

            Assert-SafeRelativePath $file
            if (Test-SensitiveContractFile $file) {
                Fail "sensitive contract file is not allowed: $file"
            }

            if (-not $allowed.Contains($file)) {
                Fail "unexpected contract file is not allowed: $file"
            }
        }
    }

    foreach ($file in $AllowedFiles) {
        if (-not (Test-Path -LiteralPath (Join-Path $repoRoot $file) -PathType Leaf)) {
            Fail "allowed contract file is missing: $file"
        }
    }
}

function Invoke-ContractBuild([string]$ProjectRelativePath) {
    $projectPath = Join-Path $exportRoot $ProjectRelativePath
    $nugetConfigPath = Join-Path $stagingRoot "NuGet.Config"
    @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
"@ | Set-Content -LiteralPath $nugetConfigPath -Encoding utf8NoBOM

    $dotnetHome = Join-Path $stagingRoot "dotnet-home"
    $dotnetAppData = Join-Path $dotnetHome "AppData"
    $dotnetPackages = Join-Path $stagingRoot "packages"
    [System.IO.Directory]::CreateDirectory($dotnetHome) | Out-Null
    [System.IO.Directory]::CreateDirectory($dotnetAppData) | Out-Null
    [System.IO.Directory]::CreateDirectory($dotnetPackages) | Out-Null

    $oldHome = $env:HOME
    $oldAppData = $env:APPDATA
    $oldUserProfile = $env:USERPROFILE
    $oldNuGetPackages = $env:NUGET_PACKAGES
    try {
        $env:HOME = $dotnetHome
        $env:APPDATA = $dotnetAppData
        $env:USERPROFILE = $dotnetHome
        $env:NUGET_PACKAGES = $dotnetPackages

        $restoreArguments = @(
            "restore",
            $projectPath,
            "--configfile",
            $nugetConfigPath
        )

        Push-Location $exportRoot
        try {
            & dotnet @restoreArguments
            if ($LASTEXITCODE -ne 0) {
                Fail "dotnet restore failed for $ProjectRelativePath with exit code $LASTEXITCODE"
            }
        }
        finally {
            Pop-Location
        }

        $arguments = @(
            "build",
            $projectPath,
            "--configuration",
            "Release",
            "--no-restore",
            "-p:ContinuousIntegrationBuild=true",
            "-p:PathMap=$exportRoot=/_/",
            "-p:ImportContractVersion=$ContractVersion"
        )

        Push-Location $exportRoot
        try {
            & dotnet @arguments
            if ($LASTEXITCODE -ne 0) {
                Fail "dotnet build failed for $ProjectRelativePath with exit code $LASTEXITCODE"
            }
        }
        finally {
            Pop-Location
        }
    }
    finally {
        $env:HOME = $oldHome
        $env:APPDATA = $oldAppData
        $env:USERPROFILE = $oldUserProfile
        $env:NUGET_PACKAGES = $oldNuGetPackages
    }
}

function Remove-BuildArtifacts([string]$Directory) {
    Get-ChildItem -LiteralPath $Directory -Recurse -Directory -Force |
        Where-Object { $_.Name -in @("bin", "obj") } |
        Sort-Object { $_.FullName.Length } -Descending |
        ForEach-Object { Remove-Item -LiteralPath $_.FullName -Recurse -Force }
}

function New-DeterministicZip([string]$SourceDirectory, [string]$ZipPath, [string[]]$RelativePaths) {
    if ([System.IO.File]::Exists($ZipPath)) {
        [System.IO.File]::Delete($ZipPath)
    }

    Add-Type -AssemblyName System.IO.Compression
    $fixedTimestamp = [System.DateTimeOffset]::new(2024, 1, 1, 0, 0, 0, [System.TimeSpan]::Zero)
    $fileStream = [System.IO.File]::Open($ZipPath, [System.IO.FileMode]::CreateNew, [System.IO.FileAccess]::ReadWrite, [System.IO.FileShare]::None)
    try {
        $archive = [System.IO.Compression.ZipArchive]::new($fileStream, [System.IO.Compression.ZipArchiveMode]::Create, $false)
        try {
            foreach ($relativePath in ($RelativePaths | Sort-Object)) {
                Assert-SafeRelativePath $relativePath
                $entry = $archive.CreateEntry($relativePath, [System.IO.Compression.CompressionLevel]::Optimal)
                $entry.LastWriteTime = $fixedTimestamp
                $entry.ExternalAttributes = (420 -shl 16)

                $sourcePath = Join-Path $SourceDirectory $relativePath
                $entryStream = $entry.Open()
                try {
                    $sourceStream = [System.IO.File]::OpenRead($sourcePath)
                    try {
                        $sourceStream.CopyTo($entryStream)
                    }
                    finally {
                        $sourceStream.Dispose()
                    }
                }
                finally {
                    $entryStream.Dispose()
                }
            }
        }
        finally {
            $archive.Dispose()
        }
    }
    finally {
        $fileStream.Dispose()
    }
}

function Read-ContractVersionFromProps {
    $propsPath = Join-Path $repoRoot "Directory.Build.props"
    if (-not [System.IO.File]::Exists($propsPath)) {
        Fail "required path is missing: Directory.Build.props"
    }

    [xml]$props = Get-Content -Raw -LiteralPath $propsPath
    $value = $props.Project.PropertyGroup.ImportContractVersion | Select-Object -First 1
    if ([string]::IsNullOrWhiteSpace($value)) {
        Fail "Directory.Build.props does not define ImportContractVersion"
    }

    $value.Trim()
}

function Resolve-OptionalPath([string]$Path) {
    if ([string]::IsNullOrWhiteSpace($Path)) {
        return $null
    }

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return $Path
    }

    Join-Path $repoRoot $Path
}

function Get-ApiCompatExecutable {
    if (-not [string]::IsNullOrWhiteSpace($ApiCompatToolPath)) {
        $candidate = Resolve-OptionalPath $ApiCompatToolPath
        if ([System.IO.File]::Exists($candidate)) {
            return $candidate
        }

        Fail "ApiCompat tool was not found: $ApiCompatToolPath"
    }

    $command = Get-Command "apicompat" -ErrorAction SilentlyContinue
    if ($command) {
        return $command.Source
    }

    return $null
}

function Test-SemVer([string]$Version) {
    $Version -match '^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-[0-9A-Za-z.-]+)?(?:\+[0-9A-Za-z.-]+)?$'
}

function Parse-SemVer([string]$Version) {
    if ($Version -notmatch '^(?<major>0|[1-9]\d*)\.(?<minor>0|[1-9]\d*)\.(?<patch>0|[1-9]\d*)(?:-(?<prerelease>[0-9A-Za-z.-]+))?(?:\+[0-9A-Za-z.-]+)?$') {
        Fail "value must be SemVer: $Version"
    }

    $prerelease = if ($Matches.ContainsKey("prerelease")) { [string]$Matches["prerelease"] } else { "" }
    [pscustomobject]@{
        Major = [int]$Matches["major"]
        Minor = [int]$Matches["minor"]
        Patch = [int]$Matches["patch"]
        HasPrerelease = -not [string]::IsNullOrWhiteSpace($prerelease)
        Prerelease = if ([string]::IsNullOrWhiteSpace($prerelease)) { @() } else { @($prerelease.Split('.')) }
    }
}

function Compare-SemVer([string]$Left, [string]$Right) {
    $leftVersion = Parse-SemVer $Left
    $rightVersion = Parse-SemVer $Right

    foreach ($part in @("Major", "Minor", "Patch")) {
        if ($leftVersion.$part -lt $rightVersion.$part) {
            return -1
        }

        if ($leftVersion.$part -gt $rightVersion.$part) {
            return 1
        }
    }

    if (-not $leftVersion.HasPrerelease -and -not $rightVersion.HasPrerelease) {
        return 0
    }

    if (-not $leftVersion.HasPrerelease) {
        return 1
    }

    if (-not $rightVersion.HasPrerelease) {
        return -1
    }

    $maxPrereleaseParts = [System.Math]::Max($leftVersion.Prerelease.Count, $rightVersion.Prerelease.Count)
    for ($index = 0; $index -lt $maxPrereleaseParts; $index++) {
        if ($index -ge $leftVersion.Prerelease.Count) {
            return -1
        }

        if ($index -ge $rightVersion.Prerelease.Count) {
            return 1
        }

        $leftIdentifier = [string]$leftVersion.Prerelease[$index]
        $rightIdentifier = [string]$rightVersion.Prerelease[$index]
        $leftIsNumeric = $leftIdentifier -match '^\d+$'
        $rightIsNumeric = $rightIdentifier -match '^\d+$'

        if ($leftIsNumeric -and $rightIsNumeric) {
            $leftNumber = [int]$leftIdentifier
            $rightNumber = [int]$rightIdentifier
            if ($leftNumber -lt $rightNumber) {
                return -1
            }

            if ($leftNumber -gt $rightNumber) {
                return 1
            }

            continue
        }

        if ($leftIsNumeric -and -not $rightIsNumeric) {
            return -1
        }

        if (-not $leftIsNumeric -and $rightIsNumeric) {
            return 1
        }

        $textComparison = [System.StringComparer]::Ordinal.Compare($leftIdentifier, $rightIdentifier)
        if ($textComparison -lt 0) {
            return -1
        }

        if ($textComparison -gt 0) {
            return 1
        }
    }

    return 0
}

function Resolve-ApiCompatBaseline {
    $baselineDirectory = Resolve-OptionalPath $ApiCompatBaselineDirectory
    if (-not $baselineDirectory -or -not [System.IO.Directory]::Exists($baselineDirectory)) {
        Write-Host "ApiCompat baseline directory not found; skipping historical API comparison: $ApiCompatBaselineDirectory"
        return $null
    }

    if (-not [string]::IsNullOrWhiteSpace($ApiCompatBaselineVersion)) {
        if (-not (Test-SemVer $ApiCompatBaselineVersion)) {
            Fail "-ApiCompatBaselineVersion must be SemVer: $ApiCompatBaselineVersion"
        }

        $explicitBaselineRoot = Join-Path $baselineDirectory $ApiCompatBaselineVersion
        if (-not [System.IO.Directory]::Exists($explicitBaselineRoot)) {
            Fail "explicit ApiCompat baseline version was not found: $ApiCompatBaselineVersion"
        }

        Write-Host "Using explicit ApiCompat baseline version $ApiCompatBaselineVersion."
        return [pscustomobject]@{
            Version = $ApiCompatBaselineVersion
            Path = $explicitBaselineRoot
        }
    }

    $selectedVersion = $null
    $selectedPath = $null
    foreach ($candidate in Get-ChildItem -LiteralPath $baselineDirectory -Directory -Force) {
        if (-not (Test-SemVer $candidate.Name)) {
            continue
        }

        if ((Compare-SemVer $candidate.Name $ContractVersion) -gt 0) {
            continue
        }

        if ($null -eq $selectedVersion -or (Compare-SemVer $candidate.Name $selectedVersion) -gt 0) {
            $selectedVersion = $candidate.Name
            $selectedPath = $candidate.FullName
        }
    }

    if ($null -eq $selectedVersion) {
        Write-Host "No ApiCompat SemVer baseline at or below contract version $ContractVersion found; skipping historical API comparison."
        return $null
    }

    Write-Host "Using ApiCompat baseline version $selectedVersion for contract version $ContractVersion."
    [pscustomobject]@{
        Version = $selectedVersion
        Path = $selectedPath
    }
}

function Invoke-ApiCompatValidation {
    $baselineSelection = Resolve-ApiCompatBaseline
    if (-not $baselineSelection) {
        return
    }

    $apiCompat = Get-ApiCompatExecutable
    if (-not $apiCompat) {
        Fail "ApiCompat baseline exists, but apicompat is not installed or passed with -ApiCompatToolPath"
    }

    foreach ($projectName in $contractProjectRoots) {
        $baselineAssembly = Join-Path $baselineSelection.Path "$projectName.dll"
        $currentAssembly = Join-Path $exportRoot "baselines/$ContractVersion/$projectName.dll"

        if (-not [System.IO.File]::Exists($baselineAssembly)) {
            Fail "ApiCompat baseline assembly is missing: $baselineAssembly"
        }

        if (-not [System.IO.File]::Exists($currentAssembly)) {
            Fail "ApiCompat current assembly is missing: $currentAssembly"
        }

        & $apiCompat -l $baselineAssembly -r $currentAssembly --strict-mode --enable-rule-cannot-change-parameter-name
        if ($LASTEXITCODE -ne 0) {
            Fail "ApiCompat validation failed for $projectName with exit code $LASTEXITCODE"
        }
    }
}

try {

$propsContractVersion = Read-ContractVersionFromProps
if (-not $ContractVersion) {
    $ContractVersion = $propsContractVersion
}
elseif ($ContractVersion -ne $propsContractVersion) {
    Fail "-ContractVersion must match Directory.Build.props ImportContractVersion ($propsContractVersion): $ContractVersion"
}

if (-not (Test-SemVer $ContractVersion)) {
    Fail "contractVersion must be SemVer: $ContractVersion"
}

if (-not $SourceCommit) {
    $SourceCommit = (& git -C $repoRoot rev-parse HEAD).Trim()
}

if ($SourceCommit -notmatch '^[0-9a-fA-F]{7,40}$') {
    Fail "sourceCommit must be a Git-like hex identifier: $SourceCommit"
}

$requiredPaths = @(
    "Directory.Build.props",
    "Rezepte.Import.Abstractions",
    "Rezepte.Import.Abstractions/Rezepte.Import.Abstractions.csproj",
    "Rezepte.Import.PluginSdk",
    "Rezepte.Import.PluginSdk/Rezepte.Import.PluginSdk.csproj"
)

foreach ($requiredPath in $requiredPaths) {
    if (-not (Test-Path -LiteralPath (Join-Path $repoRoot $requiredPath))) {
        Fail "required path is missing: $requiredPath"
    }
}

Assert-AllowedContractFiles $allowedContractFiles
$sourceFiles = $allowedContractFiles | Sort-Object -Unique

if ([System.IO.Directory]::Exists($stagingRoot)) {
    Remove-Item -LiteralPath $stagingRoot -Recurse -Force
}

[System.IO.Directory]::CreateDirectory($exportRoot) | Out-Null
[System.IO.Directory]::CreateDirectory($outputRoot) | Out-Null

foreach ($relativePath in ($sourceFiles | Sort-Object -Unique)) {
    Copy-ContractFile $relativePath
}

Invoke-ContractBuild "Rezepte.Import.Abstractions/Rezepte.Import.Abstractions.csproj"
Invoke-ContractBuild "Rezepte.Import.PluginSdk/Rezepte.Import.PluginSdk.csproj"

$baselineRoot = Join-Path $exportRoot "baselines/$ContractVersion"
[System.IO.Directory]::CreateDirectory($baselineRoot) | Out-Null
foreach ($projectName in @("Rezepte.Import.Abstractions", "Rezepte.Import.PluginSdk")) {
    $dllPath = Join-Path $exportRoot "$projectName/bin/Release/net10.0/$projectName.dll"
    if (-not [System.IO.File]::Exists($dllPath)) {
        Fail "baseline assembly was not built: $projectName.dll"
    }

    [System.IO.File]::Copy($dllPath, (Join-Path $baselineRoot "$projectName.dll"), $true)
}

Invoke-ApiCompatValidation

Remove-BuildArtifacts $exportRoot

$manifestPath = Join-Path $exportRoot "contract-export.json"
$exportedFiles = Get-ChildItem -LiteralPath $exportRoot -Recurse -File -Force |
    ForEach-Object { Get-RelativePath $exportRoot $_.FullName } |
    Where-Object { $_ -ne "contract-export.json" } |
    Sort-Object

$manifestFiles = foreach ($relativePath in $exportedFiles) {
    Assert-SafeRelativePath $relativePath
    [ordered]@{
        path = $relativePath
        sha256 = Get-Sha256 (Join-Path $exportRoot $relativePath)
    }
}

$manifest = [ordered]@{
    exportFormat = "rezepte-import-contract-v1"
    contractVersion = $ContractVersion
    sourceCommit = $SourceCommit.ToLowerInvariant()
    baselines = [ordered]@{
        path = "baselines/$ContractVersion"
        assemblies = @(
            "baselines/$ContractVersion/Rezepte.Import.Abstractions.dll",
            "baselines/$ContractVersion/Rezepte.Import.PluginSdk.dll"
        )
    }
    files = @($manifestFiles)
}

$manifest | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $manifestPath -Encoding utf8NoBOM

$zipPath = Join-Path $outputRoot "rezepte-import-contract-$ContractVersion.zip"
$zipEntries = @("contract-export.json") + @($exportedFiles)
New-DeterministicZip $exportRoot $zipPath $zipEntries
$zipSha256 = Get-Sha256 $zipPath

$metadata = [ordered]@{
    artifact = (Split-Path -Leaf $zipPath)
    artifactSha256 = $zipSha256
    exportFormat = "rezepte-import-contract-v1"
    contractVersion = $ContractVersion
    sourceCommit = $SourceCommit.ToLowerInvariant()
    files = @($manifestFiles)
}

$metadata | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $metadataPath -Encoding utf8NoBOM
$metadata | ConvertTo-Json -Depth 6

}
catch {
    [Console]::Error.WriteLine($_.Exception.Message)
    exit 1
}
