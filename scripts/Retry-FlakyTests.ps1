#Requires -Version 7.0
<#
.SYNOPSIS
    Evaluates the primary TRX of a test run and repeats failed tests marked as
    "Flaky" exactly once with a name filter.

.DESCRIPTION
    Flow:
      1. Parse the primary TRX: collect the names of all "Failed" results (theory
         data rows are normalized to the method FQN).
      2. Determine the flaky set of the project via "dotnet test --no-build
         --list-tests --filter Category=Flaky".
      3. Empty intersection -> "no flaky failures", exit 0 (no retry).
         Otherwise exactly one re-run: --filter "(Category=Flaky)&(FullyQualifiedName~...)".
      4. Evaluate the retry TRX: recovered (failed -> passed) vs. persistent.
         Persistent failures -> exit 1 (an honest signal; in CI the job stays
         non-blocking through job-level continue-on-error).

    A missing or unreadable primary TRX counts as "no flaky failures" (exit 0) so a
    step running under if: failure() never masks a build failure or a missing TRX.

.PARAMETER TestProject
    Path to the test project (.csproj) whose primary run is evaluated.

.PARAMETER PrimaryTrx
    Path to the TRX file of the primary run.

.PARAMETER Configuration
    Build configuration (default: Release).

.PARAMETER RetryTrxFileName
    File name of the retry TRX in the same TestResults directory.
    Default: <primaryname>-flaky-retry.trx

.EXAMPLE
    ./scripts/Retry-FlakyTests.ps1 -TestProject Rezepte.Tests.Browser/Rezepte.Tests.Browser.csproj -PrimaryTrx Rezepte.Tests.Browser/TestResults/test-results-browser-a1.trx
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$TestProject,
    [Parameter(Mandatory = $true)]
    [string]$PrimaryTrx,
    [string]$Configuration = "Release",
    [string]$RetryTrxFileName = ""
)

$ErrorActionPreference = "Stop"
$inGitHubActions = $env:GITHUB_ACTIONS -eq "true"

. (Join-Path $PSScriptRoot "lib/Trx.ps1")
. (Join-Path $PSScriptRoot "lib/TestDiscovery.ps1")

function Write-Annotation {
    param([string]$Kind, [string]$Message)
    if ($inGitHubActions) { Write-Host "::$Kind`::$Message" }
    else { Write-Host "[$Kind] $Message" }
}

function ConvertTo-MethodName {
    # Normalizes theory data rows ("Ns.Class.Method(arg: 1)") to the method FQN.
    param([string]$TestName)
    if ($TestName -match '^([A-Za-z0-9_]+(\.[A-Za-z0-9_]+)+)\(.*\)$') { return $matches[1] }
    return $TestName
}

# 1. Evaluate the primary TRX
$primaryResults = Get-TrxTestResults -TrxPath $PrimaryTrx
if ($null -eq $primaryResults) {
    Write-Host "No flaky failures: primary TRX '$PrimaryTrx' is missing or unreadable."
    exit 0
}

$failedMethods = @($primaryResults.GetEnumerator() |
    Where-Object { $_.Value -eq "Failed" } |
    ForEach-Object { ConvertTo-MethodName $_.Key } |
    Sort-Object -Unique)
if ($failedMethods.Count -eq 0) {
    Write-Host "No flaky failures: the primary run has no failed tests."
    exit 0
}

# 2. Determine the flaky set of the project (helper from lib/TestDiscovery.ps1)
$flakyDiscovery = Get-FlakyTestNames -ProjectPath $TestProject -Configuration $Configuration
$flakyMethods = $flakyDiscovery.Names
if (-not $flakyDiscovery.DiscoverySucceeded) {
    # A failed --list-tests (e.g. a stale build) produces an empty or partial flaky
    # set that is indistinguishable from "no flaky tests" — a needed retry would be
    # skipped silently. The exit-0 path stays (a discovery outage must not mask the
    # primary run's result), but the cause is surfaced as a warning.
    Write-Annotation -Kind "warning" -Message (
        "Flaky test discovery failed: 'dotnet test --list-tests' exited with code " +
        "$($flakyDiscovery.DiscoveryExitCode) for '$TestProject' — the flaky set may " +
        "be incomplete and a needed retry may be skipped.")
}

$retryMethods = @($failedMethods | Where-Object { $flakyMethods.Contains($_) })
if ($retryMethods.Count -eq 0) {
    Write-Host "No flaky failures: $($failedMethods.Count) failure(s) in the primary run, none of them Flaky-marked."
    exit 0
}

Write-Host "==> $($retryMethods.Count) flaky failure(s) will be retried exactly once:" -ForegroundColor Cyan
$retryMethods | ForEach-Object { Write-Host "    $_" }

# 3. Exactly one filtered re-run
$resultsDirectory = Split-Path -Parent $PrimaryTrx
if ([string]::IsNullOrEmpty($resultsDirectory)) {
    # Bare file name without a directory part (local call from the TRX directory):
    # "" would produce an unusable --results-directory "" and a broken Join-Path.
    $resultsDirectory = "."
}
if ([string]::IsNullOrWhiteSpace($RetryTrxFileName)) {
    $RetryTrxFileName = [IO.Path]::GetFileNameWithoutExtension($PrimaryTrx) + "-flaky-retry.trx"
}
$nameFilter = ($retryMethods | ForEach-Object { "FullyQualifiedName~$_" }) -join "|"
$retryOutput = & dotnet test $TestProject -c $Configuration --no-build `
    --filter "(Category=Flaky)&($nameFilter)" `
    --logger "trx;LogFileName=$RetryTrxFileName" `
    --results-directory $resultsDirectory `
    --logger "console;verbosity=normal" 2>&1
$retryOutput | ForEach-Object { Write-Host $_ }

# 4. Evaluate the retry TRX: only "Passed" counts as recovered. Other recorded
# outcomes (e.g. NotExecuted through a skip flag) are no proof of success, and a
# test that did not run at all in the filtered re-run (e.g. a discovery failure)
# is neither a pass nor a fail — both land in the indeterminate category.
# Reporting "failed in the retry" for a test that never ran would be wrong.
$retryResults = Get-TrxTestResults -TrxPath (Join-Path $resultsDirectory $RetryTrxFileName)
$persistent = [System.Collections.Generic.List[string]]::new()
$indeterminate = [System.Collections.Generic.List[string]]::new()
foreach ($method in $retryMethods) {
    $retryOutcomes = @()
    if ($null -ne $retryResults) {
        $retryOutcomes = @($retryResults.GetEnumerator() |
            Where-Object { (ConvertTo-MethodName $_.Key) -eq $method } |
            ForEach-Object { $_.Value })
    }
    if ($retryOutcomes.Count -gt 0 -and @($retryOutcomes | Where-Object { $_ -ne "Passed" }).Count -eq 0) {
        Write-Annotation -Kind "notice" -Message "Flaky retry recovered: $method (failed in the primary run, passed in the retry)"
    }
    elseif ($retryOutcomes.Count -eq 0) {
        $indeterminate.Add("$method (not executed in the retry)")
    }
    elseif (@($retryOutcomes | Where-Object { $_ -eq "Failed" }).Count -gt 0) {
        $persistent.Add($method)
    }
    else {
        $indeterminate.Add("$method (retry outcome: $(($retryOutcomes | Sort-Object -Unique) -join ', '))")
    }
}

foreach ($method in $persistent) {
    Write-Annotation -Kind "warning" -Message "Flaky retry persistent: $method (failed in the primary run AND in the retry)"
}
foreach ($method in $indeterminate) {
    Write-Annotation -Kind "warning" -Message "Flaky retry not recovered: $method (no Passed result in the retry)"
}

if ($persistent.Count -gt 0 -or $indeterminate.Count -gt 0) {
    Write-Host "==> $($persistent.Count) flaky test(s) also failed in the retry (persistent), $($indeterminate.Count) without a Passed result (not recovered)." -ForegroundColor Red
    exit 1
}
Write-Host "==> All flaky failures passed in the retry (recovered)." -ForegroundColor Green
exit 0
