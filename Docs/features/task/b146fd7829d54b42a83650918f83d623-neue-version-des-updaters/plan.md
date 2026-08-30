# Umsetzungsplan

## Ziel

Die drei direkten Referenzen auf `msTools.Updater` werden von `0.7.0-rc.11` auf `0.10.0` aktualisiert. Das bereitgestellte Paket `lib/nuget/msTools.Updater.0.10.0.nupkg` wird verbindlich in die Versionsverwaltung aufgenommen, damit ein Restore aus einem frischen Checkout nicht von einem bereits gefuellten globalen NuGet-Cache abhaengt. Die bestehende Paketquelle, Laufzeitlogik, Konfiguration und UI bleiben unveraendert, sofern die neue Paket-API keine minimale Kompatibilitaetsanpassung erzwingt.

## Umsetzungsschritte

1. **Lokales NuGet-Paket in den Repository-Bestand aufnehmen**
   - `lib/nuget/msTools.Updater.0.10.0.nupkg` als neues, versioniertes Repository-Artefakt hinzufuegen.
   - Die bereitgestellte Paketdatei weder ersetzen noch inhaltlich veraendern.
   - Vor dem Abschluss mit `git ls-files --error-unmatch lib/nuget/msTools.Updater.0.10.0.nupkg` nachweisen, dass Git die Datei erfasst; die Datei muss Bestandteil des Feature-Commits sein.
   - Die vorhandenen Pakete `0.7.0-rc.9`, `0.7.0-rc.10` und `0.7.0-rc.11` nicht im Rahmen dieser Anforderung entfernen.

2. **Direkte Paketreferenzen aktualisieren**
   - In `Rezepte.Web/Rezepte.Web.csproj` die `PackageReference` auf `msTools.Updater` auf Version `0.10.0` setzen.
   - In `Rezepte.Tests/Rezepte.Tests.csproj` dieselbe Versionsanpassung durchfuehren.
   - In `Rezepte.Updater.TestHost/Rezepte.Updater.TestHost.csproj` dieselbe Versionsanpassung durchfuehren.
   - Keine Anwendungsklassen, Schnittstellen, Konfigurationen oder UI-Dateien vorsorglich aendern.

3. **Cache-unabhaengige Paketaufloesung nachweisen**
   - Fuer den Nachweis ein neues, anfangs nicht vorhandenes Verzeichnis unter `artifacts/` als isolierten NuGet-Paketcache verwenden; keinen vorhandenen globalen Paketcache wiederverwenden.
   - `dotnet restore Rezepte.sln --configfile NuGet.config --packages <isolierter-cache> --no-http-cache --force --verbosity diagnostic` ausfuehren.
   - In der Restore-Ausgabe nachweisen, dass `msTools.Updater` `0.10.0` aus der lokalen Quelle `lib/nuget` installiert wurde.
   - Zusaetzlich die SHA-256-Pruefsumme von `lib/nuget/msTools.Updater.0.10.0.nupkg` mit der im isolierten Paketcache abgelegten Datei `mstools.updater/0.10.0/mstools.updater.0.10.0.nupkg` vergleichen. Beide Pruefsummen muessen identisch sein.
   - Verifizieren, dass alle drei Projekte in ihren erzeugten Restore-Artefakten `msTools.Updater/0.10.0` referenzieren. Der Nachweis gilt nur, wenn Restore, Herkunftspruefung und Hashvergleich erfolgreich sind.

4. **Kompilierung und API-Kompatibilitaet pruefen**
   - `Rezepte.sln` nach dem isolierten Restore ohne erneuten Restore bauen; die Solution enthaelt `Rezepte.Web`, `Rezepte.Tests` und `Rezepte.Updater.TestHost`.
   - Bei Compilerfehlern zuerst feststellen, ob sie durch API-Aenderungen von `msTools.Updater` `0.10.0` verursacht werden.
   - Nur erforderliche, minimale API-Anpassungen an den in `inventory/runtime-integration.md` und `inventory/tests-and-testhost.md` genannten Updater-Integrationen vornehmen. Fachlogik, Konfiguration und UI duerfen ohne nachgewiesene Paketinkompatibilitaet nicht geaendert werden.
   - Nach jeder notwendigen API-Anpassung Restore-Nachweis, Build und betroffene Tests erneut ausfuehren.

5. **Updater-Regressionstests ausfuehren**
   - In `Rezepte.Tests` mindestens die folgenden Testklassen gegen den isoliert wiederhergestellten Stand ausfuehren:
     - `ApplicationUpdateSettingsServiceTests`
     - `ApplicationUpdatePreInstallHandlerTests`
     - `UpdateBackupServiceTests`
     - `PluginUpdateServiceTests`
   - Die Tests ohne erneuten Restore ausfuehren, damit sie denselben nachgewiesenen Paketbestand verwenden.
   - Fehler nach Paketinkompatibilitaet, bestehendem Umgebungsproblem oder unabhaengiger Regression unterscheiden; nur paketbedingte Fehler im Umfang dieser Anforderung beheben.

6. **Testhost und Referenzbestand verifizieren**
   - Den Build von `Rezepte.Updater.TestHost` innerhalb des Solution-Builds sowie bei Bedarf separat bestaetigen.
   - Den Modus `preflight` als sicheren Start-/DI-Smoke-Test ausfuehren und eine erfolgreiche Host-Erstellung sowie Aufloesung der verwendeten Updater-Dienste nachweisen.
   - Die Modi `check`, `download`, `install` und `run` nur ausfuehren, wenn eine geeignete lokale Testkonfiguration und ein isoliertes Zielverzeichnis vorhanden sind. Andernfalls ihre Kompilierbarkeit und weiterhin gueltige Befehlsverdrahtung dokumentieren; insbesondere `install` und `run` duerfen nicht gegen eine reale Installation ausgefuehrt werden.
   - Repository-weit pruefen, dass keine direkte `PackageReference` auf `msTools.Updater` `0.7.0-rc.11` verbleibt und genau die drei erwarteten Projekte `0.10.0` referenzieren.
   - Den finalen Git-Diff darauf pruefen, dass die neue `.nupkg`-Datei und die drei Projektdateien enthalten sind und keine unbegruendeten Laufzeit-, Konfigurations- oder UI-Aenderungen aufgenommen wurden.

## Betroffene Dateien

Verbindlich hinzuzufuegen:

- `lib/nuget/msTools.Updater.0.10.0.nupkg`

Verbindlich zu aendern:

- `Rezepte.Web/Rezepte.Web.csproj`
- `Rezepte.Tests/Rezepte.Tests.csproj`
- `Rezepte.Updater.TestHost/Rezepte.Updater.TestHost.csproj`

Nur bei nachgewiesener API-Inkompatibilitaet minimal anzupassen:

- die in `inventory/runtime-integration.md` genannten Updater-Integrationen
- die in `inventory/tests-and-testhost.md` genannten Testhost- und Testdateien

Unveraenderte Pruefgegenstaende:

- `NuGet.config`
- vorhandene Pakete unter `lib/nuget/`
- bestehende Anwendungs-, Update- und UI-Konfiguration

## Test- und Nachweismatrix

| Kriterium | Nachweis |
|---|---|
| Paket ist nach einem Checkout als Repository-Artefakt vorhanden | Git erfasst `lib/nuget/msTools.Updater.0.10.0.nupkg`; die Datei ist im finalen Feature-Commit enthalten |
| Restore ist unabhaengig vom globalen Paketcache | Restore in ein neues isoliertes `--packages`-Verzeichnis mit `--no-http-cache` und `--force` |
| Paket stammt aus `lib/nuget` | Diagnostische Restore-Ausgabe nennt die lokale Quelle; SHA-256 von Quellpaket und wiederhergestellter `.nupkg` ist identisch |
| Alle direkten Referenzen verwenden `0.10.0` | Repository-Suche und Kontrolle der drei Projektdateien sowie Restore-Artefakte |
| Laufzeitintegration ist API-kompatibel | Erfolgreicher Build von `Rezepte.sln` ohne erneuten Restore |
| Bestehendes Updater-Verhalten bleibt regressionsfrei | Erfolgreiche Ausfuehrung der vier benannten Testklassen |
| Testhost bleibt grundsaetzlich startfaehig | Erfolgreicher Build und `preflight`-Smoke-Test; weitere Modi nur in isolierter geeigneter Umgebung |
| Keine unbegruendeten Folgeaenderungen | Kontrolle des finalen Git-Diffs gegen den geplanten Dateiumfang |

Ein UI-E2E-Test ist nicht erforderlich, weil weder UI, Navigation, Sichtbarkeit, Berechtigungen noch ein Benutzerfluss geaendert werden. Build, gezielte Regressionstests und der Testhost-Smoke-Test bilden den technischen Aenderungsumfang ab.

## Abnahmekriterien

- `lib/nuget/msTools.Updater.0.10.0.nupkg` ist unveraendert von Git erfasst und Bestandteil des Feature-Commits.
- Alle drei direkten `msTools.Updater`-Referenzen zeigen auf `0.10.0`; keine direkte Referenz auf `0.7.0-rc.11` verbleibt.
- Ein Restore mit leerem isoliertem Paketcache und deaktiviertem HTTP-Cache ist erfolgreich.
- Restore-Protokoll und identische SHA-256-Pruefsummen belegen die Aufloesung des Pakets aus `lib/nuget`.
- `Rezepte.sln` einschliesslich Web-Anwendung, Tests und Testhost baut ohne durch die Versionsumstellung verursachte Fehler.
- Die vier relevanten Updater-Testklassen bestehen.
- Der Testhost besteht den `preflight`-Smoke-Test; weitergehende Modi sind entweder in einer isolierten Testumgebung erfolgreich oder ihre umgebungsbedingte Nichtausfuehrung ist dokumentiert.
- Es sind keine fachlich unbegruendeten Aenderungen an Laufzeitlogik, Konfiguration oder UI enthalten.

## Offene Punkte

Keine.
