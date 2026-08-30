# Plan-Gegenprüfung

## Ergebnis

**Status:** Plan vollständig

## Abgleich Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| Die drei direkten `PackageReference`-Einträge verwenden `msTools.Updater` `0.10.0`. | Umsetzungsschritt 2 nennt `Rezepte.Web`, `Rezepte.Tests` und `Rezepte.Updater.TestHost` mit der konkreten Zielversion. | Umsetzungsschritt 6 fordert die repository-weite Suche nach verbliebenen Referenzen auf `0.7.0-rc.11` und genau drei Referenzen auf `0.10.0`; zusätzlich werden die Restore-Artefakte geprüft. | Abgedeckt |
| Das bereitgestellte Paket `lib/nuget/msTools.Updater.0.10.0.nupkg` steht nach einem frischen Checkout über die lokale Paketquelle zur Verfügung. | Umsetzungsschritt 1 nimmt die bislang unversionierte Paketdatei verbindlich und unverändert in den Repository-Bestand auf. | Git-Erfassung und Aufnahme in den Feature-Commit werden ausdrücklich nachgewiesen. | Abgedeckt |
| Restore mit der bestehenden `NuGet.config` löst `msTools.Updater` `0.10.0` aus `lib/nuget` auf. | Umsetzungsschritt 3 verwendet die bestehende Konfiguration und einen neuen isolierten Paketcache. | Diagnostisches Restore-Protokoll, Kontrolle der Restore-Artefakte und SHA-256-Vergleich zwischen Quellpaket und Cache-Datei belegen Version und Herkunft unabhängig vom globalen Cache. | Abgedeckt |
| Die bestehende Updater-Integration ist mit `0.10.0` kompilierbar; notwendige API-Anpassungen bleiben minimal und auf die Integration begrenzt. | Umsetzungsschritt 4 begrenzt Änderungen auf nachgewiesene API-Inkompatibilitäten an den im Inventar genannten Integrationspunkten. | Der vollständige Solution-Build ohne erneuten Restore prüft Web-Anwendung, Tests und Testhost gegen denselben isoliert wiederhergestellten Paketbestand. | Abgedeckt |
| Die betroffenen bestehenden Updater-Tests bleiben erfolgreich. | Umsetzungsschritt 5 nennt `ApplicationUpdateSettingsServiceTests`, `ApplicationUpdatePreInstallHandlerTests`, `UpdateBackupServiceTests` und `PluginUpdateServiceTests`. | Alle vier im Inventar ausgewiesenen Regressionstestklassen werden ohne erneuten Restore ausgeführt; Fehler werden nach Ursache eingeordnet. | Abgedeckt |
| Der eigenständige Updater-Testhost bleibt build- und grundsätzlich startfähig. | Umsetzungsschritte 4 und 6 berücksichtigen den Testhost und seine Modi `preflight`, `check`, `download`, `install` und `run`. | Solution-Build und `preflight`-Smoke-Test sind verbindlich; potenziell eingreifende Modi werden nur mit geeigneter isolierter Testkonfiguration ausgeführt und andernfalls nachvollziehbar dokumentiert. | Abgedeckt |
| Fachlogik, Konfiguration und UI bleiben ohne nachgewiesene Paketinkompatibilität unverändert. | Ziel, Umsetzungsschritte 2 und 4 sowie der definierte Dateiumfang schließen vorsorgliche Folgeänderungen aus. | Der finale Git-Diff wird gegen den geplanten Dateiumfang geprüft; unbegründete Laufzeit-, Konfigurations- und UI-Änderungen sind ein negatives Abnahmekriterium. | Abgedeckt |

## Fehlende oder unvollständige Testanforderungen

Keine.

## E2E-Abdeckung

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| Reiner Wechsel der Paketversion ohne Änderung an UI, Navigation, Sichtbarkeit, Berechtigungen oder Benutzerinteraktion | Kein UI-E2E-Test erforderlich. Der isolierte Restore, der vollständige Build, die vier gezielten Regressionstestklassen und der Testhost-`preflight`-Smoke-Test decken den technischen Änderungsumfang ab. | Nicht erforderlich mit Begründung |

## Fehlende oder unvollständige Planbestandteile

Keine.

## Hinweise

Die im vorherigen Plan-Check beanstandeten Punkte sind vollständig behoben: Die Paketdatei wird verbindlich versioniert, und der Restore-Nachweis ist durch einen neuen isolierten Paketcache, deaktivierten HTTP-Cache, diagnostische Ausgabe, Restore-Artefakte und einen Hashvergleich vom globalen NuGet-Cache unabhängig.
