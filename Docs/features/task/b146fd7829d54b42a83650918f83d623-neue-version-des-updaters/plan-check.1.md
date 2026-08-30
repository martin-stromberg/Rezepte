# Plan-Gegenprüfung

## Ergebnis

**Status:** Plan lückenhaft

## Abgleich Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| Die drei direkten `PackageReference`-Einträge verwenden `msTools.Updater` `0.10.0`. | Umsetzungsschritt 1 nennt alle drei betroffenen Projektdateien und die konkrete Zielversion. | Umsetzungsschritt 5 sieht eine repository-weite Suche nach verbliebenen Referenzen auf `0.7.0-rc.11` und die Kontrolle der drei Zielreferenzen vor. | Abgedeckt |
| Das bereitgestellte Paket `lib/nuget/msTools.Updater.0.10.0.nupkg` steht über die lokalen Repository-Artefakte zur Verfügung. | Umsetzungsschritt 2 behandelt die Paketdatei nur als vorhandenen Prüfgegenstand und macht ihre Aufnahme in die Versionsverwaltung von einer späteren Bedingung abhängig. Laut Bestandsaufnahme ist sie unversioniert, während die bisherigen Pakete derselben Quelle versioniert sind. Ein verbindlicher Schritt zur Aufnahme der neuen Paketdatei fehlt. | Der Restore soll die lokale Quelle prüfen, weist aber nicht nach, dass das Paket auch in einem frischen Checkout ohne bereits gefüllten globalen NuGet-Cache verfügbar ist. | Lücke |
| Restore mit der bestehenden `NuGet.config` löst `msTools.Updater` `0.10.0` aus `lib/nuget` auf. | Umsetzungsschritt 2 beschreibt Restore und Quellenprüfung. | Restore und Verifikation von Version und Quelle sind vorgesehen. | Abgedeckt |
| Die bestehende Updater-Integration ist mit `0.10.0` kompilierbar; notwendige API-Anpassungen bleiben minimal und auf die Integration begrenzt. | Umsetzungsschritt 3 sieht den Solution-Build und gezielte minimale Anpassungen an den in der Bestandsaufnahme genannten Integrationspunkten vor. | Solution-Build einschließlich Web-Anwendung, Tests und Testhost ist vorgesehen. | Abgedeckt |
| Die betroffenen bestehenden Updater-Tests bleiben erfolgreich. | Umsetzungsschritt 4 nennt `ApplicationUpdateSettingsServiceTests`, `ApplicationUpdatePreInstallHandlerTests`, `UpdateBackupServiceTests` und `PluginUpdateServiceTests`. | Alle vier laut Bestandsaufnahme relevanten Testklassen sollen ausgeführt werden. | Abgedeckt |
| Der eigenständige Updater-Testhost bleibt build- und grundsätzlich startfähig. | Umsetzungsschritte 3 und 5 berücksichtigen den Testhost sowie seine Modi `preflight`, `check`, `download`, `install` und `run`. | Build als Smoke-Test und eine umgebungsabhängige Prüfung der Modi sind vorgesehen. | Abgedeckt |
| Fachlogik, Konfiguration und UI bleiben ohne nachgewiesene Paketinkompatibilität unverändert. | Umsetzungsschritte 1 und 3 begrenzen mögliche Folgeänderungen ausdrücklich auf durch `0.10.0` verursachte API-Anpassungen. | Das Abnahmekriterium fordert das Ausbleiben fachlich unbegründeter Änderungen; der geplante Dateiumfang macht dies prüfbar. | Abgedeckt |

## Fehlende oder unvollständige Testanforderungen

- [ ] Einen reproduzierbaren Restore-Nachweis aus einem frischen Checkout oder mit isoliertem/leeren NuGet-Paketcache planen, der belegt, dass das versionierte Paket `msTools.Updater.0.10.0.nupkg` tatsächlich aus `lib/nuget` aufgelöst wird und der Erfolg nicht von einem bereits gefüllten globalen Cache abhängt.

## E2E-Abdeckung

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| Reiner Wechsel der Paketversion ohne neue oder geänderte UI, Navigation, Berechtigung, Sichtbarkeit oder Benutzerinteraktion | Kein UI-E2E-Test vorgesehen; Build, gezielte Regressionstests und Testhost-Smoke-Tests prüfen die von der Abhängigkeitsaktualisierung betroffenen technischen Abläufe. | Nicht erforderlich mit Begründung |

## Fehlende oder unvollständige Planbestandteile

- [ ] `lib/nuget/msTools.Updater.0.10.0.nupkg` verbindlich als hinzuzufügendes Repository-Artefakt und als betroffene Datei aufnehmen. Die im Plan formulierte Bedingung ist bereits erfüllt: Die Bestandsaufnahme weist die Datei als unversioniert aus, und die vorhandenen Versionen derselben lokalen Paketquelle sind versioniert.
- [ ] Die Repository-Prüfung um den konkreten Nachweis ergänzen, dass `msTools.Updater.0.10.0.nupkg` von Git erfasst wird und damit nach einem frischen Checkout für Restore und Build vorhanden ist.

## Hinweise

Die fachlichen Versions-, Build- und Regressionstestanforderungen sind ansonsten vollständig abgedeckt. Nach verbindlicher Aufnahme der Paketdatei und einem cache-unabhängigen Restore-Nachweis kann der Plan erneut geprüft werden.
