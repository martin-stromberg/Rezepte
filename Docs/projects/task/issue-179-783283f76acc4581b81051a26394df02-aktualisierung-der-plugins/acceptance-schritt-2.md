# Abnahmeprüfung – Entwicklungsschritt 2

## Ergebnis

**Status:** Anforderung vollständig erfüllt

## Abweichungen

Keine.

## Hinweise

- **Einzelquellen-Prüfung:** `PluginUpdateService.CheckSourceForUpdatesAsync` (`Rezepte.Web/Services/Import/Plugins/PluginUpdateService.cs:60-87`) lädt die Quelle per Id, prüft `Enabled` (Z. 71-74) und `TrustConfirmed` (Z. 76-79) und läuft anschließend durch denselben `ProcessSourceAsync`-Pfad wie die Start-Prüfung — Release abrufen, ZIP-Asset, Download, Paketprüfung, Installation, Release-Historie (Z. 89-167, `DownloadValidateInstallAsync` Z. 169-257). Die Vorbedingungen sind damit per Konstruktion identisch zur Start-Prüfung; die PAT-Prüfung für private Quellen (Z. 99-106) liegt im gemeinsamen Pfad.
- **Skip/Retry:** Bereits installierte Releases werden mit `NoUpdate(alreadyInstalled: true)` übersprungen (Z. 122-129), zuvor fehlgeschlagene (`ValidationFailed`, `DownloadFailed`, `InstallFailed`, `ReloadFailed`, `RateLimited`) auf `Pending` zurückgesetzt und erneut versucht (Z. 145-152).
- **Statusfelder:** `LastCheckedAt` wird zu Prüfbeginn gesetzt und `LastError`/`LastErrorAt` zurückgesetzt (Z. 91-94); bei Erfolg `LastSuccessfulReleaseTag` (Z. 210-212), bei Fehlern `LastError`/`LastErrorAt` über `MarkSourceFailureAsync` (Z. 287-292) — auch im Catch-Pfad mit `CancellationToken.None`, damit der Fehlerstatus zuverlässig persistiert wird.
- **Serialisierung:** Statisches `SemaphoreSlim CheckGate` (Z. 25) wird von `CheckForUpdatesAsync` und `CheckSourceForUpdatesAsync` für die gesamte Prüfdauer gehalten — prozessweit und damit auch über DI-Scopes hinweg wirksam. Eine manuelle Prüfung während eines laufenden Start-Laufs wird eingereiht (kein „besetzt"-Ergebnis), gleichzeitige Installationen sind ausgeschlossen; die Busy-Darstellung ist Thema von Schritt 4.
- **Admin-only und Ergebnis-Rückgabe:** `PluginSettingsService.CheckSourceForUpdatesAsync` (`PluginSettingsService.cs:186-196`) erzwingt `EnsureAdmin()` und delegiert an `IPluginUpdateService`; `PluginUpdateCheckResult` liefert `Outcome` (`NoUpdate`/`Installed`/`Failed`), `ReleaseTag`, `Error` und `AlreadyInstalled` als Rückgabe für die Oberfläche. Die Admin-Prüfung sitzt — konsistent mit der bestehenden Schichtung — am Settings-Service, nicht am Update-Service selbst.
- **Ohne Neustart wirksam:** Die Installation läuft über `InstallWithReloadTrackingAsync` → `pluginManager.CoordinateReloadAsync` (`PluginPackageInstaller.cs:31-93`), also über die in Schritt 1 abgenommene Austauschmechanik; `ReloadStatus`/`ReloadError` werden weiterhin getrennt fortgeschrieben.
- `PluginUpdateCheckOutcome` ist als String-Konstanten-Klasse umgesetzt — konsistent mit der bestehenden `PluginSourceReleaseStatus`-Konvention. `Error` transportiert die technische (englische) Meldung; Schritt 4 sollte das `Outcome` lokalisiert darstellen und `Error` als Detail anzeigen.
- **Tests:** 16 neue Tests — 3 Vorbedingungs-Tests (Quelle unbekannt/deaktiviert/nicht vertrauensbestätigt, jeweils ohne GitHub-Aufruf), 7 Ablauf-/Status-Tests (Install inkl. Statusfelder, Skip bereits installiert inkl. keinem Download, Retry nach Fehlschlag, kein Release, kein ZIP-Asset, fehlendes PAT, `ReloadFailed`-Mapping, unerwartete Ausnahme), 3 Serialisierungs-Tests (manuell wartet auf Start-Lauf, Start-Lauf wartet auf manuell, Gate-Freigabe nach Fehler) sowie 3 Settings-Service-Tests (Nicht-Admin abgelehnt, fehlender HttpContext abgelehnt, Delegation). Fakes sind realitätsnah (In-Memory-DB, blockierender GitHub-Client für die Parallelitätsnachweise).
- **Verifikation:** `dotnet test Rezepte.Tests` — 677/677 bestanden. Browser-Suite: 19/20 im Gesamtlauf; `Register_WithDemoData_CreatesDemoData` schlug einmal mit einem Timeout in der 120-s-Demo-Daten-Warte fehl (Seed-Job `Succeeded`, beobachteter Zähler `calendar=0`), die Einzelwiederholung bestand (2 m 7 s). Der Diff enthält keine UI- oder Register-Änderung — zeitabhängige Unschärfe, keine Regression; das gemeldete Ergebnis 20/20 ist damit plausibel.
- Die Arbeitsdokumente des Schritts unter `Docs/features/task/…schritt-2-einzelquellen-check*/` wurden im Commit entfernt; `Docs/RELEASE_NOTES.md` wurde ergänzt (Einzelquellen-Prüfung bislang nur auf Service-Ebene, Admin-beschränkt, UI folgt).
- Nachfolgeaktion außerhalb dieses Befunds: `steps.md` führt Schritt 2 noch als „In Arbeit" — auf „Fertig" setzen und Schritt-Tracking-Commit durchführen.
