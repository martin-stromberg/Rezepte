param(
    [string]$Configuration = "Release",
    [string]$Output = "artifacts/plugins"
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$outputRoot = Join-Path $root $Output
$projects = @(
    "Rezepte.Import.Plugins.Chefkoch",
    "Rezepte.Import.Plugins.SecondSource",
    "Rezepte.Import.Plugins.ThirdSource",
    "Rezepte.Import.Plugins.FourthSource",
    "Rezepte.Import.Plugins.FifthSource",
    "Rezepte.Import.Plugins.SixthSource"
)

if (Test-Path $outputRoot) {
    Remove-Item -LiteralPath $outputRoot -Recurse -Force
}

foreach ($projectName in $projects) {
    $projectPath = Join-Path $root "$projectName/$projectName.csproj"
    $pluginOutput = Join-Path $outputRoot $projectName
    dotnet publish $projectPath -c $Configuration -o $pluginOutput --no-self-contained

    $contractAssembly = Join-Path $pluginOutput "Rezepte.Import.Abstractions.dll"
    if (Test-Path $contractAssembly) {
        Remove-Item -LiteralPath $contractAssembly -Force
    }
}

Write-Host "Plugins published to $outputRoot"
