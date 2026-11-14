# Bestandsaufnahme: Versionsverzeichnisse für Plugin-Installationen (Issue #179, Schritt 1)

Analysiert wurde der Installations- und Erkennungspfad quelleninstallierter Import-Plugins (`PluginUpdateService` → `PluginPackageInstaller` → `PluginManager`) bezogen auf die Anforderung, neue Pluginversionen in eigenen Versionsverzeichnissen unter `plugins/` abzulegen, Altverzeichnisse beim koordinierten Austausch zu entfernen bzw. persistent als zu löschen zu markieren und markierte Verzeichnisse aus der Erkennung auszuschließen.

## Zusammenfassung

- **Installation erfolgt aktuell „in place":** `PluginPackageInstaller.InstallWithReloadTrackingAsync` sichert je Quellverzeichnis das bestehende Ziel `plugins/{Verzeichnisname}` nach `%TEMP%/rezepte-plugin-backup/{guid}`, löscht es und kopiert die neue Version an dieselbe Stelle (`PluginPackageInstaller.cs:45-69`). Es existiert **kein** aus dem Release-Tag abgeleitetes Versionsverzeichnis.
- **Der Release-Tag reicht nicht bis zum Installer:** `IPluginPackageInstaller.InstallAsync`/`InstallWithReloadTrackingAsync` nehmen nur `IReadOnlyList<string> pluginDirectories` entgegen (`IPluginPackageInstaller.cs:13,20`). `PluginUpdateService` besitzt den Tag als `record.ReleaseTag` (`PluginUpdateService.cs:150`), übergibt ihn aber nicht.
- **Kein Mechanismus für Löschmarkierungen:** Scheitert `Directory.Delete` auf ein Altverzeichnis, wird die Ausnahme nicht als „zu löschen markieren" behandelt — im Austauschpfad bricht sie den Vorgang mit `InstallFailed`/`ReloadFailed` ab, in `TryDeleteDirectory` (Rollback/Temp-Bereinigung) wird sie nur geloggt. Es existiert keine persistente Markierung, kein Ausschluss markierter Verzeichnisse aus der Erkennung und keine Nachbereinigung beim Start oder Austausch.
- **Erkennung betrachtet genau eine Unterverzeichnisebene:** `PluginManager.DiscoverExternalPlugins` sammelt `*.dll` direkt unter den Plugin-Roots (`{ContentRoot}/plugins`, `{BaseDirectory}/plugins` via `GetPluginRoots`) plus DLL-Kandidaten aus direkten Unterverzeichnissen (`GetPluginAssemblyCandidates`, `PluginManager.cs:244-283`). Tiefere Verschachtelung wird nicht durchsucht; bei mehreren Verzeichnissen mit derselben Plugin-`Id` gewinnt `SelectPreferredDescriptor` (`PluginManager.cs:449-455`) — es gibt aber keine versionsbewusste Auswahl.
- **Publish-Plugins sind nicht von quelleninstallierten Verzeichnissen unterscheidbar:** Die csproj-Targets `CopyImportPluginsToOutput`/`CopyImportPluginsToPublish` legen mitgelieferte Plugins als `plugins/<Projektname>/` ab (`Rezepte.Web.csproj:69-91`); externe Plugin-Artefakte landen ebenfalls direkt unter `plugins/`. Es existiert kein Namensschema oder Verwaltungsverzeichnis, das quelleninstallierte von Publish-Plugin-Verzeichnissen trennt.
- **`Missing`-Status und Rollback-Grundgerüst vorhanden:** `SynchronizeSettingsAsync` markiert nicht mehr entdeckte Plugins als `Missing` (`PluginManager.cs:439-444`); `PluginPackageInstallException` trägt bereits `InstallFailed`/`ReloadFailed` und `PluginUpdateService` persistiert `ReloadStatus`/`ReloadError` getrennt. `PluginStatus`/`PluginSourceReleaseStatus` decken die geforderten Zustände ab (voraussichtlich unverändert).
- **Kein manueller Update-Trigger und keine UI-Änderung nötig** — Auslöser bleibt `PluginUpdateHostedService` beim Start (Scope-Ausschluss); `PluginStartupService` delegiert `StartAsync` an `IPluginManager.InitializeAsync` und ist der Pfad, über den eine Startbereinigung wirksam würde.

**Test-Ausgangszustand:** Projektweiter Baseline-Lauf vom selben Codestand (HEAD `ee49bad`, Code identisch zu `3e0d864`): `Rezepte.Tests` 609/609 bestanden, `Rezepte.Tests.Browser` 19 bestanden + 1 preexisting Fehlschlag (`RegisterTests.Register_WithDemoData_CreatesDemoData`, Demo-Daten-Seed-Timeout, außerhalb Plugin-Bereich). Eigener Plugin-gefilterter Lauf auf diesem Branch: `dotnet test Rezepte.Tests --filter "FullyQualifiedName~Plugin"` → **49 erfolgreich, 0 fehlgeschlagen, 0 übersprungen, Exit-Code 0**. Details und Nachweise: [inventory/tests.md](inventory/tests.md) sowie projektweite Baseline unter `Docs/projects/task/issue-179-783283f76acc4581b81051a26394df02-aktualisierung-der-plugins/inventory/tests.md`.

## Details

- [Datenmodell](inventory/models.md)
- [Logik](inventory/logic.md)
- [Status-Konstanten („Enums")](inventory/enums.md)
- [Interfaces](inventory/interfaces.md)
- [Tests](inventory/tests.md)
