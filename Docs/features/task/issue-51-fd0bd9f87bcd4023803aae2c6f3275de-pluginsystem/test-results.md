# Testergebnisse

Status: Keine Fehler

## Ausgefuehrte Tests

- `dotnet build Rezepte.sln --no-restore`
  - Ergebnis: erfolgreich
  - Warnungen: bekannte `NU1903`-Warnung fuer `SQLitePCLRaw.lib.e_sqlite3` 2.1.11

- `dotnet test Rezepte.sln --no-restore --logger "console;verbosity=minimal"`
  - Ergebnis: erfolgreich
  - Tests: 137 bestanden, 0 fehlgeschlagen, 0 uebersprungen
  - Warnungen: bekannte `NU1903`-Warnung fuer `SQLitePCLRaw.lib.e_sqlite3` 2.1.11

## Fehlgeschlagene Tests

Keine.
