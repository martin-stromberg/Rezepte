# Builds the static GitHub Pages site into an output directory (default artifacts/pages).
# Assembles the maintained sources under site/ with the screenshots from
# Docs/screenshots/ and the application icons from Rezepte.Web/wwwroot/icons/.
# The same script is used for the local preview and by .github/workflows/pages.yml.
param(
    [ValidateNotNullOrEmpty()]
    [string]$OutputDirectory = "artifacts/pages"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function Fail([string]$Message) {
    throw "Site build failed: $Message"
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path

# The build deletes and recreates the output directory. To make a mistyped
# -OutputDirectory harmless, the fully resolved path must be a strict child of
# <repo>/artifacts/ - the repo root, parent directories, site/, Docs/,
# Rezepte.Web/, scripts/ or any other location are rejected before anything
# is removed. Resolving "." / ".." / empty segments happens via GetFullPath,
# so traversal attempts like "artifacts/../site" are rejected as well.
$artifactsRoot = [IO.Path]::GetFullPath((Join-Path $repoRoot "artifacts"))
$rawOutput = if ([IO.Path]::IsPathRooted($OutputDirectory)) { $OutputDirectory } else { Join-Path $repoRoot $OutputDirectory }
$outputRoot = [IO.Path]::GetFullPath($rawOutput)
$artifactsPrefix = $artifactsRoot.TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
if (-not $outputRoot.StartsWith($artifactsPrefix, [StringComparison]::OrdinalIgnoreCase)) {
    Fail "Output directory '$OutputDirectory' resolves to '$outputRoot', which is not a child of the artifact area '$artifactsRoot'. Only directories below <repo>/artifacts/ are allowed, so repo sources can never be deleted."
}

$siteSource = Join-Path $repoRoot "site"
$screenshotsSource = Join-Path $repoRoot "Docs/screenshots"
$iconsSource = Join-Path $repoRoot "Rezepte.Web/wwwroot/icons"
$iconFiles = @("rezepte-180x180.png", "rezepte_64x64.ico")

function Assert-Directory([string]$Path, [string]$Description) {
    if (-not (Test-Path -LiteralPath $Path -PathType Container)) {
        Fail "$Description not found at '$Path'."
    }
}

Assert-Directory $siteSource "Site source directory"
Assert-Directory $screenshotsSource "Screenshots source directory"
Assert-Directory $iconsSource "Icons source directory"

foreach ($iconFile in $iconFiles) {
    $iconPath = Join-Path $iconsSource $iconFile
    if (-not (Test-Path -LiteralPath $iconPath -PathType Leaf)) {
        Fail "Required icon '$iconFile' not found at '$iconPath'."
    }
}

$screenshotFiles = Get-ChildItem -LiteralPath $screenshotsSource -File
if ($screenshotFiles.Count -eq 0) {
    Fail "Screenshots source directory '$screenshotsSource' is empty."
}

if (Test-Path -LiteralPath $outputRoot) {
    Remove-Item -LiteralPath $outputRoot -Recurse -Force
}
New-Item -ItemType Directory -Path $outputRoot -Force | Out-Null

# 1. Maintained page sources (index.html, assets/css, assets/js, screenshots/*.webp)
Copy-Item -Path (Join-Path $siteSource "*") -Destination $outputRoot -Recurse -Force

# 2. Screenshots stay single-source in Docs/screenshots and are copied into the artifact
$screenshotsTarget = Join-Path $outputRoot "screenshots"
New-Item -ItemType Directory -Path $screenshotsTarget -Force | Out-Null
Copy-Item -Path (Join-Path $screenshotsSource "*") -Destination $screenshotsTarget -Force

# 3. Application icons (favicon, apple-touch-icon, header logo)
$iconsTarget = Join-Path $outputRoot "assets/icons"
New-Item -ItemType Directory -Path $iconsTarget -Force | Out-Null
foreach ($iconFile in $iconFiles) {
    Copy-Item -LiteralPath (Join-Path $iconsSource $iconFile) -Destination $iconsTarget -Force
}

$indexPath = Join-Path $outputRoot "index.html"
if (-not (Test-Path -LiteralPath $indexPath -PathType Leaf)) {
    Fail "site/ did not produce an index.html in '$outputRoot'."
}

$fileCount = (Get-ChildItem -LiteralPath $outputRoot -Recurse -File).Count
Write-Host "Site assembled in '$outputRoot' ($fileCount files, $($screenshotFiles.Count) screenshots, $($iconFiles.Count) icons)."
