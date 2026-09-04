# Plan-Review

## Status

Vollstaendig umgesetzt

## Bewertung

Die zuvor offenen Planpunkte wurden im Plan nachgezogen:

- Die Paketstrategie setzt keine ungeprueften transitiven Zielversionen mehr
  voraus. Die tatsaechlich per Restore aufgeloesten Versionen muessen
  projektbezogen dokumentiert und gegen den Vulnerability-Scan geprueft
  werden.
- Die aktuell belegte sichere Aufloesung ist im Plan benannt:
  `SQLitePCLRaw.lib.e_sqlite3` `2.1.12` in `Rezepte.Web`, `Rezepte.Tests` und
  `Rezepte.Tests.Browser`; `AngleSharp` `1.7.3` in `Rezepte.Tests`.
- Die CI-nahen Testschritte umfassen jetzt Format-Check, Security-Scan,
  Release-Builds, Testlauf mit Coverage-Collection, ReportGenerator-Auswertung
  mit 70-%-Schwelle und den `Export-ImportContract.ps1`-Lauf mit ApiCompat.
- Der Browsernachweis benennt den Publish-Pfad
  `Rezepte.Web/bin/Release/net10.0/publish`, den gestarteten publizierten
  Prozess und die temporaere SQLite-Datei als nachzuweisende Details.
- Die Linux-Abnahme ist an die konkreten PR-CI-Jobs `static checks` und
  `build & test` mit ihren relevanten Steps gebunden. Ohne realen gruenen
  PR-CI-Lauf bleibt dieser Nachweis offen.

## Offene Aufgaben

Keine.

## Bereits abgedeckt

- [x] Direkte Aenderungsorte und ausdruecklich unveraenderte CI-Dateien sind
  benannt.
- [x] Restore vor Versionspruefung und erneuter Vulnerability-Scan sind
  vorgesehen.
- [x] Die relevante SQLite- und bUnit-Testabdeckung ist benannt.
- [x] Kein neuer fachlicher UI-E2E-Test wird faelschlich als erforderlich
  eingeplant; der vorhandene Browserlauf bleibt als technischer
  Regressionsnachweis erhalten.
- [x] Abweichende, aber sichere Restore-Aufloesungen muessen dokumentiert und
  gegen die Sicherheitsdatenbank geprueft werden.
- [x] Coverage, Contract-Export und PR-CI-Linux-Nachweis sind als eigene
  Abnahmeschritte beschrieben.
