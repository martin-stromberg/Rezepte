# Plan-Gegenprüfung

## Ergebnis

**Status:** Plan lückenhaft

## Abgleich Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| `SQLitePCLRaw.lib.e_sqlite3` ist auf eine aktuelle, nicht verwundbare Version aktualisiert. | Die Aktualisierung von `Microsoft.EntityFrameworkCore.Sqlite` auf `10.0.11` und die erwartete transitive Auflösung von `SQLitePCLRaw.lib.e_sqlite3` auf `3.53.3` sind konkret beschrieben. | Restore und Vulnerability-Scan sind vorgesehen. Der in Schritt 3 genannte Kontrollbefehl `dotnet list Rezepte.sln package` listet ohne `--include-transitive` die zu prüfende transitive SQLitePCLRaw-Version jedoch nicht auf; damit fehlt ein ausführbarer Nachweis der konkret aufgelösten Zielversion. | Lücke |
| `AngleSharp` ist auf eine aktuelle, nicht verwundbare Version aktualisiert. | Die Aktualisierung von `bunit` auf `1.40.0` und die erwartete transitive Auflösung von `AngleSharp` auf `1.7.3` sind konkret beschrieben; direktes Pinning ist nur als begründeter Fallback vorgesehen. | Restore, Komponenten-/Render-Tests und Vulnerability-Scan sind vorgesehen. Auch hier kann der in Schritt 3 genannte Paketlisten-Befehl ohne `--include-transitive` die konkret aufgelöste AngleSharp-Version nicht nachweisen. | Lücke |
| Die Solution baut erfolgreich. | Umsetzungsschritt 4 sieht den vollständigen Release-Build der Solution und bei Bedarf die CI-nahen Einzelprojekt-Builds vor. | `dotnet build Rezepte.sln --configuration Release --no-restore` sowie die Builds von Web-, Unit- und Browsertestprojekt sind als erfolgreiche Nachweise festgelegt. | Abgedeckt |
| Die vollständige Testsuite läuft erfolgreich durch. | Umsetzungsschritte 5 und 6 berücksichtigen die vollständige Solution-Testsuite, den Publish-Pfad und die vorhandenen Playwright-Browsertests. | `dotnet test Rezepte.sln --configuration Release --no-build` ist verbindlich vorgesehen; die laut Bestandsaufnahme besonders betroffenen Export-, Restore-, Systembackup-, Komponenten-/Render- und Browsertests sind darin enthalten und werden zusätzlich risikoorientiert ausgewertet. | Abgedeckt |
| `dotnet list package --vulnerable --include-transitive` meldet keine verbleibenden Verwundbarkeiten. | Umsetzungsschritt 7 fordert den erneuten blockierenden Scan nach frischem Restore. | `dotnet list Rezepte.sln package --vulnerable --include-transitive` muss insgesamt ohne gemeldete verwundbare Pakete enden. | Abgedeckt |
| Das `static checks`-Gate aus PR #138 wird durch die Paketaktualisierungen grün. | Security-Scan und statischer Release-Build mit `TreatWarningsAsErrors=true` sind eingeplant; die CI-Workflows und die Security-Scan-Action bleiben unverändert. | Der reale `static checks`-Pfad enthält zusätzlich `dotnet format Rezepte.sln --verify-no-changes --no-restore --severity error`. Dieser Nachweis fehlt im Plan, sodass nicht der vollständige Gate-Ablauf abgedeckt ist. | Lücke |

## Fehlende oder unvollständige Testanforderungen

- [ ] Nach dem Restore die konkret aufgelösten Versionen von `SQLitePCLRaw.lib.e_sqlite3` und `AngleSharp` mit einem transitiven Paketnachweis prüfen, beispielsweise mit `dotnet list Rezepte.sln package --include-transitive`, und die erwarteten nicht verwundbaren Zielversionen für Web-, Unit- und Browsertestprojekt bestätigen.
- [ ] Den vollständigen `static checks`-Pfad einschließlich `dotnet format Rezepte.sln --verify-no-changes --no-restore --severity error` als verbindlichen Abnahmenachweis aufnehmen.
- [ ] Für die native SQLite-Abhängigkeit verbindlich festlegen, wie die laut Bestandsaufnahme relevanten Linux- und Windows-Runtime-Varianten nachgewiesen werden, etwa durch Publish/Start beziehungsweise Browsertests auf beiden Plattformen oder durch eine begründete, explizit dokumentierte Plattformabgrenzung.

## E2E-Abdeckung

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| Reine Aktualisierung transitiver Pakete ohne Änderung an UI, Navigation, Sichtbarkeit, Berechtigungen oder Benutzerinteraktion | Kein neuer fachlicher UI-E2E-Test erforderlich. Der vorhandene Playwright-Lauf nach Publish startet die Anwendung mit einer temporären SQLite-Datei und dient als technischer Laufzeit-Regressionstest. | Nicht erforderlich mit Begründung |

## Fehlende oder unvollständige Planbestandteile

- [ ] Den Paketauflösungs-Schritt so korrigieren, dass die transitiven Zielpakete tatsächlich ausgegeben und ihre konkreten Versionen prüfbar werden; der derzeit genannte Befehl ohne `--include-transitive` erfüllt die beschriebene Kontrolle nicht.
- [ ] Den Abnahmenachweis für das Akzeptanzkriterium `static checks` um den im CI-Job vorhandenen Format-Check ergänzen oder die erfolgreiche Ausführung des vollständigen unveränderten CI-Jobs verbindlich vorsehen.
- [ ] Die offene Inventarfrage zur Plattformabdeckung der nativen SQLite-Binaries explizit entscheiden und die gewählte Linux-/Windows-Prüfung in Umsetzungsreihenfolge, Testkriterien und Risiken verankern.

## Hinweise

Die Paketänderungsorte, Zielversionen, vollständigen Builds und Tests, SQLite-spezifischen Regressionstests, der Browser-Startpfad sowie der abschließende Vulnerability-Scan sind ansonsten schlüssig abgedeckt. UI-spezifische neue E2E-Szenarien sind für diese rein technische Abhängigkeitsaktualisierung nicht erforderlich.
