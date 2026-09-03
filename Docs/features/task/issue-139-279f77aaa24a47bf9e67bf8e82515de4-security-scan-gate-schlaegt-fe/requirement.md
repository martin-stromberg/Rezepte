# Anforderungsuebersetzung

## Metadaten

- Aufgaben-ID: `279f77aa-a24a-47bf-9e67-bf8e82515de4`
- Branch: `task/issue-139-279f77aaa24a47bf9e67bf8e82515de4-security-scan-gate-schlaegt-fe`
- Thema: Security-Scan-Gate schlaegt wegen verwundbarer Pakete fehl

## Ziel

Die durch den Security-Scan-Gate der CI angezeigten Verwundbarkeiten in den transitiven Paketen `SQLitePCLRaw.lib.e_sqlite3` und `AngleSharp` sollen durch Aktualisierung auf aktuelle, nicht verwundbare Versionen behoben werden. Danach soll der Security-Scan ohne Befunde erfolgreich sein.

## Ausgangslage

- Der Security-Scan-Gate in `pr-staging-ci.yml` und `staging-ci.yml` wurde im Rahmen der CI-Standardisierung aus PR #138 scharf geschaltet.
- Der Gate verwendet `dotnet list package --vulnerable --include-transitive` und wertet das Ergebnis nun als Fehler aus.
- Die beiden Verwundbarkeiten bestanden bereits vor der CI-Migration und wurden zuvor nur informativ gemeldet.
- Betroffen sind:
  - `SQLitePCLRaw.lib.e_sqlite3` Version `2.1.11`, Severity `High`
  - `AngleSharp` Version `1.2.0`, Severity `Moderate`; nur in Testprojekten referenziert

## Umfang

1. Mit `dotnet list Rezepte.sln package --vulnerable --include-transitive` die betroffenen direkten Referenzen und verfügbare, nicht verwundbare Versionen ermitteln.
2. Die Referenzen von `SQLitePCLRaw.lib.e_sqlite3` und `AngleSharp` auf geeignete nicht verwundbare Versionen anheben.
3. Die Solution vollständig neu bauen.
4. Die vollständige Testsuite ausführen.
5. Den Vulnerability-Scan mit `dotnet list package --vulnerable --include-transitive` erneut ausführen und das Ergebnis prüfen.

## Randbedingungen

- Das Upgrade von `SQLitePCLRaw` kann die SQLite-Datenzugriffsschicht und damit das Laufzeitverhalten beeinflussen.
- Paketaktualisierungen müssen deshalb bewusst getestet und freigegeben werden.
- Die Behebung soll nicht stillschweigend als Nebenwirkung der CI-Umstellung erfolgen.
- Der bestehende CI-Security-Gate soll unverändert bleiben; angepasst werden die verwundbaren Paketreferenzen und erforderliche Folgeänderungen.

## Akzeptanzkriterien

- `SQLitePCLRaw.lib.e_sqlite3` ist auf eine aktuelle, nicht verwundbare Version aktualisiert.
- `AngleSharp` ist auf eine aktuelle, nicht verwundbare Version aktualisiert.
- Die Solution baut erfolgreich.
- Die vollständige Testsuite läuft erfolgreich durch.
- `dotnet list package --vulnerable --include-transitive` meldet keine verbleibenden Verwundbarkeiten.
- Das `static checks`-Gate aus PR #138 wird durch diese Paketaktualisierungen grün.

## Referenz

- PR #138: https://github.com/martin-stromberg/Rezepte/pull/138
