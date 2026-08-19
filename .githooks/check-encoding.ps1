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
$mojibake = @()
$mojibakePattern = "`u{00C3}(?:`u{00A4}|`u{00B6}|`u{00BC}|`u{201E}|`u{2013}|`u{0153}|`u{0178})"
$transliterationFiles = @()
$transliterations = @(
    'Abhaengigkeit',
    'Abhaengigkeiten',
    'Abhaengigkeits',
    'Ausgewaehlt',
    'Benutzermenue',
    'Benutzerzugehoerigkeit',
    'Bestaetigung',
    'Bestaetigungsdialog',
    'Dafuer',
    'Eindeutigkeitspruefung',
    'Fruehere',
    'Fuer',
    'Geprueft',
    'Gueltigkeit',
    'Hauptmenuepunkt',
    'Hinzufuegen',
    'Importablaeufe',
    'Kochbuecher',
    'Kochbuechern',
    'Kompressionsverhaeltnis',
    'Laeufe',
    'Laeufen',
    'Loesche',
    'Loeschen',
    'Loeschungen',
    'Menue',
    'Menueleiste',
    'Menuepunkt',
    'Pruefe',
    'Pruefen',
    'Pruefung',
    'Pruefungen',
    'Quellenpruefung',
    'Rezeptuebernahme',
    'Ruecksync',
    'Schluessel',
    'Signaturschluessel',
    'Startpruefung',
    'Ueberpruefen',
    'Ungueltige',
    'Ungueltiges',
    'Unzulaessige',
    'Vertrauensbestaetigung',
    'Waehlen',
    'Waehrend',
    'Zusaetzlich',
    'abgewaehlt',
    'abhaengig',
    'abhaengigem',
    'abhaengigen',
    'abhaengiger',
    'abhaengiges',
    'abzuwaehlen',
    'aeltere',
    'aendern',
    'aendert',
    'angehaengt',
    'angewaehlt',
    'ausdrueckliche',
    'ausgewaehlt',
    'ausgewaehlte',
    'ausgewaehltem',
    'ausgewaehlten',
    'auslaeuft',
    'auswaehlen',
    'benoetigen',
    'benoetigt',
    'beruecksichtigt',
    'bestaetigt',
    'bestaetigte',
    'dafuer',
    'duerfen',
    'enthaelt',
    'erhaelt',
    'essloeffel',
    'fruehere',
    'fuer',
    'geaendert',
    'geaenderte',
    'geloescht',
    'gemaess',
    'geoeffnet',
    'geprueft',
    'gewaehlt',
    'gewaehlte',
    'gewaehlten',
    'gewaehrleisten',
    'gueltige',
    'gueltigen',
    'gueltiger',
    'haelt',
    'hinzufuegen',
    'hinzugefuegt',
    'hinzugefuegte',
    'hinzuzufuegen',
    'koennen',
    'laeuft',
    'loeschen',
    'muessen',
    'noetig',
    'oeffentlich',
    'oeffentliche',
    'oeffentlichen',
    'oeffnen',
    'oeffnet',
    'pruefen',
    'prueft',
    'stueck',
    'stuecke',
    'teeloeffel',
    'ueber',
    'uebergeben',
    'uebergeordnete',
    'uebergibt',
    'ueberhaupt',
    'uebernehmen',
    'uebernommen',
    'ueberprueft',
    'ueberschreiben',
    'ueberschreibt',
    'ueberschrieben',
    'ueberspringen',
    'uebersprungen',
    'uebertragen',
    'unabhaengig',
    'ungueltige',
    'ungueltigen',
    'ungueltiges',
    'unterdrueckt',
    'unveraenderlich',
    'unveraendert',
    'veraendert',
    'verfuegbar',
    'verfuegbare',
    'verfuegbaren',
    'verknuepft',
    'veroeffentlicht',
    'veroeffentlichte',
    'veroeffentlichten',
    'vertrauenswuerdig',
    'verzoegert',
    'verzoegerte',
    'vollstaendig',
    'vollstaendigen',
    'vorausgewaehlt',
    'waehlt',
    'waehrend',
    'waehrenddessen',
    'zufaellig',
    'zufaellige',
    'zufaelliger',
    'zulaessig',
    'zulaessige',
    'zurueck',
    'zurueckliegt',
    'zusaetzlich',
    'zusaetzliche'
)
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
        if ($text -match $mojibakePattern) {
            $mojibake += $relative
        }
                foreach ($t in $transliterations) {
            if ($text.Contains($t)) {
                $transliterationFiles += $relative
                break
            }
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

if ($mojibake.Count -gt 0) {
    Write-Host "The following files contain double-encoded umlauts (Mojibake like 'AbhÃ¤ngigkeit'):" -ForegroundColor Red
    $mojibake | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
    $hasErrors = $true
}

if ($transliterationFiles.Count -gt 0) {
    Write-Host "The following files contain ASCII-transliterated German umlauts (ue/ae/oe):" -ForegroundColor Red
    $transliterationFiles | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
    $hasErrors = $true
}

if ($hasErrors) {
    exit 1
}

Write-Host "Encoding check passed."
