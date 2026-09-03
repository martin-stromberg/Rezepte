# CI- und Validierungsflaeche

## Blockierender PR-/Staging-Pfad

`.github/workflows/pr-staging-ci.yml` und `.github/workflows/staging-ci.yml` restaurieren `Rezepte.sln`, rufen die gemeinsame Action `.github/actions/security-scan/action.yml` auf und bauen anschliessend mit `TreatWarningsAsErrors=true`. Die Action nutzt `dotnet list "${solution-path}" package --vulnerable --include-transitive` und beendet den Job bei Vulnerability-Ausgabe mit Exit Code 1.

## Weitere Nachweise

- Beide Workflows bauen Anwendung, Tests und Browsertests in Release.
- Beide installieren Playwright-Browser, publizieren `Rezepte.Web` und fuehren `dotnet test Rezepte.sln` aus.
- `.github/workflows/security-scan.yml` fuehrt denselben Scan woechentlich und manuell unabhangig von Codeaenderungen aus.

## Konsequenz fuer die Umsetzung

Die CI-Gate-Logik ist nicht der Fix-Ort. Der Nachweis muss nach Restore in derselben Solution erfolgen wie in CI. Die Test- und Buildkommandos aus den Workflows sind Bestandteil der Abnahme; ein lokaler Einzelprojekt-Scan reicht nicht aus.
