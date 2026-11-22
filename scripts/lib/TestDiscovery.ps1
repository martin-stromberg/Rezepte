#Requires -Version 7.0
<#
.SYNOPSIS
    Shared test discovery helpers for the test scripts (imported via dot-sourcing).

.DESCRIPTION
    Encapsulates the evaluation of "dotnet test --list-tests" output so the call and
    the line parsing are not maintained in every script. Usage in the calling script:

        . (Join-Path $PSScriptRoot "lib/TestDiscovery.ps1")
#>

function Get-FlakyTestNames {
    # Returns a result object:
    #   Names              — HashSet[string] of the method FQNs of all tests with
    #                        Category=Flaky of the project,
    #   DiscoverySucceeded — the --list-tests call exited with code 0,
    #   DiscoveryExitCode  — raw $LASTEXITCODE of the --list-tests call.
    # The call uses --no-build and therefore assumes a previous successful build. A
    # failed discovery (e.g. a stale build) yields an empty or partial set that must
    # not look like "no flaky tests" — callers check DiscoverySucceeded and surface
    # a warning.
    # xUnit theory data rows ("Ns.Class.Method(arg: 1)") are automatically reduced to
    # the method FQN by the prefix pattern because the parenthesized arguments are
    # not part of the match.
    param([string]$ProjectPath, [string]$Configuration = "Release")
    $listOutput = & dotnet test $ProjectPath -c $Configuration --no-build --list-tests --filter "Category=Flaky" 2>&1
    $listExitCode = $LASTEXITCODE
    $names = [System.Collections.Generic.HashSet[string]]::new()
    foreach ($line in $listOutput) {
        $trimmed = "$line".Trim()
        if ($trimmed -match '^([A-Za-z0-9_]+(\.[A-Za-z0-9_]+)+)') {
            [void]$names.Add($matches[1])
        }
    }
    return [PSCustomObject]@{
        Names              = $names
        DiscoverySucceeded = ($listExitCode -eq 0)
        DiscoveryExitCode  = $listExitCode
    }
}
