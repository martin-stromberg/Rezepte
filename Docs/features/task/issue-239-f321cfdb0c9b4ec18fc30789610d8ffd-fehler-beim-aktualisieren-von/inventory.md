# Bestandsaufnahme: Fehler beim Aktualisieren von Plugins (Issue #239)

Bestandsaufnahme des Plugin-Update-Pfads (`CheckSourceForUpdatesAsync` bzw. die Startprüfung `CheckForUpdatesAsync`) bezogen auf die Anforderung, dass temporäre Arbeitsverzeichnisse keine vorhandene Verzeichnisstruktur voraussetzen dürfen und Verzeichnisfehler als fachliche, lokalisierte Meldung statt als rohe Exception erscheinen sollen.

## Zusammenfassung

- **Drei Produktiv-Fundstellen** legen Arbeitsverzeichnisse direkt unter `Path.GetTempPath()` an: `PluginUpdateService.DownloadValidateInstallAsync` (`rezepte-plugin-download/<guid>`, Zeile 171/172), `PluginPackageValidator.ValidateAsync` (`rezepte-plugin-package/<guid>`, Zeile 29/30) und `PluginPackageInstaller.BackupDirectory` (`rezepte-plugin-backup/<guid>`, Zeile 245). Es gibt **keine zentrale Abstraktion** für Arbeitsverzeichnisse und keine Beschreibbarkeitsprüfung des Basisverzeichnisses.
- Bei `PluginPackageValidator` steht das `Directory.CreateDirectory` **außerhalb** des `try`-Blocks — ein Verzeichnisfehler propagiert als Exception; bei `PluginUpdateService` und `PluginPackageInstaller` landet die rohe `Exception.Message` in `PluginSource.LastError` und `PluginUpdateCheckResult.Failed`.
- Die UI (`PluginSettings.razor`, `MapCheckError`, Zeilen 389–404) lokalisiert nur die acht bekannten `PluginUpdateCheckErrors`-Konstanten; unbekannte Fehlertexte — wie die Exception-Message „Could not find file '/tmp/rezepte-plugin-download'." — fallen in den Fallback `"{PluginCheckFailed} ({error})"`. Eine Fehlerkonstante für „Arbeitsverzeichnis nicht anlegbar" **existiert nicht**.
- Als garantiert beschreibbares Basisverzeichnis steht `{ContentRootPath}/plugins` bereit: `PluginDirectoryRegistry` (singleton, `PluginRoot`, Zeile 20) und `PluginPackageInstaller` (Zeile 52) legen es per `Directory.CreateDirectory` an und schreiben darin. `PluginUpdateOptions` (Section `PluginUpdates`) hat **keinen** Schlüssel für ein Arbeitsverzeichnis.
- `PluginUpdateService` und `PluginPackageValidator` haben **keine** `IHostEnvironment`-Abhängigkeit; `PluginPackageInstaller` hat sie bereits.
- Gleiches `Path.GetTempPath()`-Muster außerhalb des Scope (ausdrücklich nicht Teil der Korrektur): `ExportService.cs:277`, `AdminExportsController.cs:105`.
- Test-Ausgangszustand: **keine Fehlschläge** — 763/763 (deterministische Lane), 15/15 (OsInterface-Subset), 5/5 (PluginSettings-Browser-Subset); Nachweis in [inventory/tests.md](inventory/tests.md). Testlücke: kein Test simuliert ein fehlendes/nicht beschreibbares Temp-Basisverzeichnis; Rest der Browser-Suite nicht ausgeführt.

## Details

- [Datenmodell](inventory/models.md)
- [Logik](inventory/logic.md)
- [Enums](inventory/enums.md)
- [Interfaces](inventory/interfaces.md)
- [Tests](inventory/tests.md)
