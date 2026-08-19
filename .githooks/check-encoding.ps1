[CmdletBinding()]
param(
    [switch]$Staged
)

<#
.SYNOPSIS
    Validates that tracked source files are valid UTF-8 and contain no
    U+FFFD replacement characters (indicates encoding corruption).
#>

$extensions = @('.cs', '.razor', '.cshtml', '.html', '.js', '.css', '.json', '.resx', '.xaml', '.xml')
$utf8 = [System.Text.Encoding]::GetEncoding(
    'UTF-8',
    [System.Text.EncoderFallback]::ExceptionFallback,
    [System.Text.DecoderFallback]::ExceptionFallback
)

if ($Staged) {
    $files = git diff --cached --name-only --diff-filter=ACM |
        Where-Object { $extensions -contains [System.IO.Path]::GetExtension($_).ToLowerInvariant() }
}
else {
    $files = git ls-files |
        Where-Object { $extensions -contains [System.IO.Path]::GetExtension($_).ToLowerInvariant() }
}

$invalid = @()
$replacement = @()
$root = git rev-parse --show-toplevel

foreach ($relative in $files) {
    $path = Join-Path $root $relative
    if (-not (Test-Path $path)) { continue }

    $bytes = [System.IO.File]::ReadAllBytes($path)

    try {
        $text = $utf8.GetString($bytes)
        if ($text.Contains("`u{FFFD}")) {
            $replacement += $relative
        }
    }
    catch {
        $invalid += $relative
    }
}

$hasErrors = $false

if ($invalid.Count -gt 0) {
    Write-Host "The following files are not valid UTF-8:" -ForegroundColor Red
    $invalid | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
    $hasErrors = $true
}

if ($replacement.Count -gt 0) {
    Write-Host "The following files contain U+FFFD replacement characters (encoding corruption):" -ForegroundColor Red
    $replacement | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
    $hasErrors = $true
}

if ($hasErrors) {
    exit 1
}

Write-Host "Encoding check passed."
