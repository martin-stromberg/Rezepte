# Offene Aufgaben

Erstellt am: 2026-08-11
Abbruchgrund: Strategiewechsel nach CI-Fehlern beim privaten Submodule-Checkout

Die folgenden Aufgaben muessen im erneuten Lifecycle-Lauf umgesetzt und verifiziert werden.

## Strategiewechsel msTools.Updater

- [x] `msTools.Updater` wird nicht mehr als Git-Submodule eingebunden. Entferne die Submodule-Einbindung und passe Projekt-/Workflow-Dateien entsprechend an.
- [x] Lege das fertig kompilierte `msTools.Updater`-Kompilat in diesem Projekt ab und referenziere dieses Artefakt aus der Anwendung, statt den externen Quellcode zu bauen.
- [x] Passe die Update-Logik so an, dass zukuenftige Updates die neue Version des Kompilats laden und das vorhandene Kompilat austauschen.
- [x] Entferne die zuvor eingefuehrte CI-Authentifizierung fuer das private Updater-Submodule, sofern sie durch den Wegfall des Submodules nicht mehr benoetigt wird.
- [x] Verifiziere lokal Build, Tests und Formatierung sowie den GitHub-Actions-Checkout ohne privaten Submodule-Zugriff.

## Umsetzungshinweis

- Der Lifecycle wurde als Fortsetzung ausgefuehrt. Separate Unteragenten waren in dieser Umgebung nicht verfuegbar; die Umsetzung und Verifikation erfolgten lokal im Hauptagenten.
- Lokale Verifikation: `dotnet restore Rezepte.sln`, `dotnet build Rezepte.sln --no-restore`, `dotnet test Rezepte.sln --no-build`, `dotnet format Rezepte.sln --verify-no-changes --no-restore`.
