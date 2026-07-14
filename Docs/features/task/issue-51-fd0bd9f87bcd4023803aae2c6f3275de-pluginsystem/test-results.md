# Testergebnisse

Status: Keine Fehler

## Ausgefuehrte Tests

- `dotnet build Rezepte.sln --no-restore`
  - Ergebnis: erfolgreich
  - Hinweise: 38 Warnungen; darunter NU1903 zu `SQLitePCLRaw.lib.e_sqlite3` und bestehende Nullable-/Obsolete-Warnungen

- `dotnet test Rezepte.sln --no-restore --logger "console;verbosity=minimal"`
  - Ergebnis: erfolgreich
  - Tests: 137 bestanden, 0 fehlgeschlagen, 0 uebersprungen
  - Hinweise: 2 NU1903-Warnungen zu `SQLitePCLRaw.lib.e_sqlite3`

## Fehlgeschlagene Tests

Keine.
