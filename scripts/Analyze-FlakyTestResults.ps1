#Requires -Version 7.0
<#
.SYNOPSIS
    Evaluates TRX files for flake events and reports quarantine candidates.

.DESCRIPTION
    A "run" is grouped by the file name convention: <name>.trx is the primary run,
    <name>-flaky-retry.trx in the same directory is its retry (produced by
    scripts/Retry-FlakyTests.ps1 or the CI step "Retry flaky tests once").

    A flake event is:
      - a failure in the primary TRX + green in the retry TRX of the same run, or
      - a failure in the primary TRX of run N followed by green in the primary TRX
        of the next run containing the test (sorted by the run timestamp from the
        TRX <Times start> attribute, LastWriteTimeUtc as fallback).

    Primary outcomes outside Passed/Failed (e.g. NotExecuted through CI skip flags
    or discovery failures) count as a missing observation: they raise neither the
    execution counter nor the failure rate and do not interrupt the run sequence
    (no phantom "green in the follow-up run" after a skip, no lost fail->pass
    across a skip).

    Classification per test name (window: last -MaxRuns primary runs):
      - quarantine candidate: >= QuarantineMinFlakeEvents flake events OR
        failure rate >= QuarantineMinFailureRate at >= QuarantineMinExecutions runs.
      - watch: >= 1 flake event OR failure rate >= WatchMinFailureRate.
      - stable: otherwise.

    Report only — the exit code is always 0. For quarantine candidates a
    recommendation is printed (open a quarantine issue + document the failure
    pattern in Docs/help/os-interface-tests.md); in GitHub Actions additionally as
    a ::warning:: annotation.

.PARAMETER Path
    One or more directories scanned recursively for TRX files — e.g. local runs
    (Rezepte.Tests/TestResults, Rezepte.Tests.Browser/TestResults) or CI artifacts
    fetched via "gh run download <run-id> -n test-results-browser-<pr|staging>-aN".

.PARAMETER MaxRuns
    Evaluation window: only the last N primary runs (by the run timestamp inside
    the TRX, LastWriteTimeUtc as fallback) count. Default: 20.

.PARAMETER QuarantineMinFlakeEvents
    A test with this many flake events in the window is a quarantine candidate.
    Default: 2 (starting value, calibratable).

.PARAMETER QuarantineMinFailureRate
    Failure rate threshold for quarantine (together with QuarantineMinExecutions).
    Default: 0.2 (starting value, calibratable).

.PARAMETER QuarantineMinExecutions
    Minimum number of primary executions before the failure-rate quarantine applies.
    Default: 5 (starting value, calibratable).

.PARAMETER WatchMinFailureRate
    Failure rate threshold for the watch level. Default: 0.1 (starting value,
    calibratable).

.EXAMPLE
    ./scripts/Analyze-FlakyTestResults.ps1 -Path Rezepte.Tests/TestResults, Rezepte.Tests.Browser/TestResults

.EXAMPLE
    gh run download 123456789 -n test-results-browser-pr-a1 -D artifacts/browser-pr
    ./scripts/Analyze-FlakyTestResults.ps1 -Path artifacts/browser-pr
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string[]]$Path,
    [int]$MaxRuns = 20,
    [int]$QuarantineMinFlakeEvents = 2,
    [double]$QuarantineMinFailureRate = 0.2,
    [int]$QuarantineMinExecutions = 5,
    [double]$WatchMinFailureRate = 0.1
)

$ErrorActionPreference = "Stop"
$inGitHubActions = $env:GITHUB_ACTIONS -eq "true"

. (Join-Path $PSScriptRoot "lib/Trx.ps1")

# 1. Collect TRX files and split them into primary/retry
$trxFiles = @()
foreach ($dir in $Path) {
    if (Test-Path $dir) {
        $trxFiles += @(Get-ChildItem -Path $dir -Filter "*.trx" -Recurse -File)
    }
}

$primaries = [System.Collections.Generic.List[object]]::new()
$retries = [System.Collections.Generic.List[object]]::new()
foreach ($file in $trxFiles) {
    if ($file.BaseName -like "*-flaky-retry") {
        $retries.Add($file)
    }
    else {
        $primaries.Add($file)
    }
}
# Run order primarily via the run timestamp inside the TRX (<Times start="...">):
# LastWriteTimeUtc only reflects the download time of artifacts fetched via
# "gh run download" and is therefore not a reliable ordering criterion.
$primaries = @($primaries | Sort-Object {
    $runStart = Get-TrxRunStartTime -TrxPath $_.FullName
    if ($null -ne $runStart) { $runStart } else { $_.LastWriteTimeUtc }
} | Select-Object -Last $MaxRuns)

Write-Host "==> $($primaries.Count) primary run(s) and $($retries.Count) retry TRX in the window (MaxRuns=$MaxRuns)."

if ($primaries.Count -eq 0) {
    Write-Host "==> No TRX files found — empty report."
    exit 0
}

# 2. Parse the runs (primary + paired retry in the same directory)
$runs = [System.Collections.Generic.List[object]]::new()
foreach ($primary in $primaries) {
    $primaryResults = Get-TrxTestResults -TrxPath $primary.FullName
    if ($null -eq $primaryResults) { continue }
    $retryPath = Join-Path $primary.DirectoryName "$($primary.BaseName)-flaky-retry.trx"
    $retryResults = $null
    if (Test-Path $retryPath) {
        $retryResults = Get-TrxTestResults -TrxPath $retryPath
    }
    $runs.Add([PSCustomObject]@{
        Name    = $primary.BaseName
        Primary = $primaryResults
        Retry   = $retryResults
    })
}

# 3. Per test name: executions, failures, flake events
$stats = @{}
foreach ($run in $runs) {
    foreach ($entry in $run.Primary.GetEnumerator()) {
        $name = $entry.Key
        # Non-Passed/Failed primary outcomes (e.g. NotExecuted through skip flags or
        # discovery failures) count as a missing observation: no execution, no
        # sequence transition, no LastOutcome update. Otherwise fail->skip would
        # produce a phantom "green in the follow-up run", dilute Executions/failure
        # rate, and erase a recorded LastOutcome=Failed (a real fail->skip->pass
        # sequence would be lost).
        if ($entry.Value -ne "Passed" -and $entry.Value -ne "Failed") { continue }
        $failed = $entry.Value -eq "Failed"
        if (-not $stats.ContainsKey($name)) {
            $stats[$name] = [PSCustomObject]@{
                Executions   = 0
                Failures     = 0
                FlakeEvents  = 0
                LastOutcome  = $null
            }
        }
        $stat = $stats[$name]
        $stat.Executions++
        if ($failed) { $stat.Failures++ }
        $retried = $null -ne $run.Retry -and $run.Retry.Contains($name)
        $retryOutcome = if ($retried) { $run.Retry[$name] } else { $null }
        # Only "Passed" counts as green — other retry outcomes (e.g. NotExecuted
        # through a skip flag or a discovery failure in the filtered re-run) are no
        # proof of success and must not produce a flake event (same rule as
        # Retry-FlakyTests.ps1).
        $recovered = $failed -and $retried -and $retryOutcome -eq "Passed"
        if ($recovered) {
            # Failure in the primary + green in the retry of the same run.
            $stat.FlakeEvents++
        }
        # For the primary-to-primary sequence the effective run outcome counts: a
        # run rescued by the retry counts as green so a flake event is not counted
        # twice (retry-green + follow-up-run-green). Only Passed/Failed retry
        # outcomes are adopted — a re-run that did not execute does not change the
        # primary outcome.
        $effectiveOutcome = if ($retryOutcome -eq "Passed" -or $retryOutcome -eq "Failed") { $retryOutcome } else { $entry.Value }
        if ($effectiveOutcome -ne "Failed" -and $stat.LastOutcome -eq "Failed") {
            # Failure in the primary of run N, green in the primary of the next run.
            $stat.FlakeEvents++
        }
        $stat.LastOutcome = $effectiveOutcome
    }
}

# 4. Classification and report
$report = [System.Collections.Generic.List[object]]::new()
foreach ($entry in $stats.GetEnumerator()) {
    $stat = $entry.Value
    $failureRate = if ($stat.Executions -gt 0) { $stat.Failures / $stat.Executions } else { 0 }
    $classification = "stable"
    if ($stat.FlakeEvents -ge $QuarantineMinFlakeEvents -or
        ($stat.Executions -ge $QuarantineMinExecutions -and $failureRate -ge $QuarantineMinFailureRate)) {
        $classification = "quarantine candidate"
    }
    elseif ($stat.FlakeEvents -ge 1 -or $failureRate -ge $WatchMinFailureRate) {
        $classification = "watch"
    }
    $report.Add([PSCustomObject]@{
        Test           = $entry.Key
        Executions     = $stat.Executions
        Failures       = $stat.Failures
        FlakeEvents    = $stat.FlakeEvents
        FailureRate    = "{0:P0}" -f $failureRate
        Classification = $classification
    })
}

$severity = @{ "quarantine candidate" = 0; "watch" = 1; "stable" = 2 }
$sorted = @($report | Sort-Object { $severity[$_.Classification] }, Test)

Write-Host ""
Write-Host "| Test | Executions | Failures | Flake events | Failure rate | Classification |"
Write-Host "|---|---|---|---|---|---|"
foreach ($row in $sorted) {
    Write-Host "| $($row.Test) | $($row.Executions) | $($row.Failures) | $($row.FlakeEvents) | $($row.FailureRate) | $($row.Classification) |"
}
Write-Host ""

$candidates = @($report | Where-Object { $_.Classification -eq "quarantine candidate" })
$watched = @($report | Where-Object { $_.Classification -eq "watch" })
Write-Host "==> $($report.Count) tests evaluated: $($candidates.Count) quarantine candidate(s), $($watched.Count) watch, $($report.Count - $candidates.Count - $watched.Count) stable."

foreach ($candidate in $candidates) {
    $message = "Quarantine candidate: $($candidate.Test) ($($candidate.FlakeEvents) flake events, failure rate $($candidate.FailureRate)) — recommendation: open a quarantine issue + document the failure pattern in Docs/help/os-interface-tests.md."
    if ($inGitHubActions) { Write-Host "::warning::$message" }
    else { Write-Host "[warning] $message" -ForegroundColor Yellow }
}

exit 0
