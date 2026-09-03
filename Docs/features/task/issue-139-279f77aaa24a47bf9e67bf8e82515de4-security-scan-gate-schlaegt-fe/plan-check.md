# Plan-Gegenprüfung

## Ergebnis

**Status:** Plan vollständig

## Abgleich Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| `SQLitePCLRaw.lib.e_sqlite3` ist auf eine aktuelle, nicht verwundbare Version aktualisiert. | Die direkte Ursprungsreferenz `Microsoft.EntityFrameworkCore.Sqlite` wird von `10.0.9` auf `10.0.11` aktualisiert; als transitive Zielauflösung ist `SQLitePCLRaw.lib.e_sqlite3` `3.53.3` für Web-, Unit- und Browsertestprojekt festgelegt. Ein kontrollierter Fallback auf die nächste kompatible, nicht verwundbare Version ist für eine abweichende Paketauflösung beschrieben. | Nach frischem Restore prüft `dotnet list Rezepte.sln package --include-transitive` die projektbezogene Versionsmatrix und schließt alte oder verwundbare Parallelversionen aus. Der abschließende Vulnerability-Scan, SQLite-basierte Service-Tests sowie Publish, Start und Playwright-Lauf auf Windows und Linux ergänzen den Nachweis. | Abgedeckt |
| `AngleSharp` ist auf eine aktuelle, nicht verwundbare Version aktualisiert. | `bunit` wird von `1.38.5` auf `1.40.0` aktualisiert, wodurch `AngleSharp` `1.7.3` erwartet wird. Direktes Pinning ist nur als dokumentierter Fallback vorgesehen, wenn die sichere transitive Auflösung anders nicht erreichbar ist. | Der transitive Paketnachweis bestätigt `AngleSharp` `1.7.3` und das Fehlen der verwundbaren Version `1.2.0`; der Vulnerability-Scan und die vollständigen Komponenten-/Render-Tests prüfen Sicherheit und Teststack-Kompatibilität. | Abgedeckt |
| Die Solution baut erfolgreich. | Die Umsetzungsreihenfolge enthält den vollständigen Release-Build mit Warnungen als Fehler sowie die drei CI-nahen Einzelprojekt-Builds. | `dotnet build Rezepte.sln --configuration Release --no-restore -p:TreatWarningsAsErrors=true` und die Release-Builds von Web-, Unit- und Browsertestprojekt müssen erfolgreich sein. | Abgedeckt |
| Die vollständige Testsuite läuft erfolgreich durch. | Publish, Playwright-Installation und der vollständige Solution-Testlauf sind als verbindlicher Umsetzungsschritt festgelegt; die besonders betroffenen Export-, Restore-, Systembackup-, Komponenten-/Render- und Browsertests werden ausdrücklich ausgewertet. | `dotnet test Rezepte.sln --configuration Release --no-build` muss vollständig erfolgreich sein. Der Browserlauf startet dabei die publizierte Anwendung gegen eine temporäre SQLite-Datenbank. | Abgedeckt |
| `dotnet list package --vulnerable --include-transitive` meldet keine verbleibenden Verwundbarkeiten. | Ein Ausgangsscan und ein abschließender Scan nach Restore, Build, Publish und Tests sind für die gesamte Solution eingeplant. | `dotnet list Rezepte.sln package --vulnerable --include-transitive` muss ohne `SQLitePCLRaw.lib.e_sqlite3`, `AngleSharp` oder andere verbleibende Verwundbarkeiten enden. | Abgedeckt |
| Das `static checks`-Gate aus PR #138 wird durch diese Paketaktualisierungen grün. | Der Plan bildet den vollständigen Jobablauf aus Formatprüfung, Vulnerability-Scan und Release-Build mit `TreatWarningsAsErrors=true` ab, lässt Action und Workflows unverändert und verlangt anschließend den realen PR-CI-Nachweis auf `ubuntu-latest`. | Der lokal nachvollzogene `static checks`-Pfad und die erfolgreichen PR-CI-Jobs `static checks` sowie `build & test` weisen Gate, Coverage-Schwelle und Linux-Laufzeitpfad nach. | Abgedeckt |

## Fehlende oder unvollständige Testanforderungen

Keine.

## E2E-Abdeckung

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| Fachliche UI-Flows, Navigation, Sichtbarkeit, Berechtigungen oder Benutzerinteraktionen | Die Anforderung ändert ausschließlich Paketabhängigkeiten und keinen fachlichen Benutzerfluss; deshalb ist kein neuer UI-E2E-Test erforderlich. | Nicht erforderlich mit Begründung |
| SQLite-Laufzeit nach Aktualisierung der nativen Abhängigkeit | Die vorhandene Playwright-Suite wird nach Release-Publish vollständig ausgeführt und muss die Anwendung mit einer temporären SQLite-Datei auf Windows lokal sowie im unveränderten Linux-PR-CI-Pfad erfolgreich starten. | Abgedeckt |

## Fehlende oder unvollständige Planbestandteile

Keine.

## Hinweise

Die Planung schließt die in der vorherigen Gegenprüfung erkannten Lücken: Der transitive Versionsnachweis verwendet `--include-transitive`, der vollständige `static checks`-Pfad enthält den Format-Check, und die native SQLite-Abhängigkeit wird verbindlich auf Windows und Linux validiert. Die vorhandene Playwright-Suite ist als technischer E2E-Regressionsnachweis angemessen; ein neuer fachlicher UI-Test ist mangels geändertem Benutzerfluss nicht erforderlich.
