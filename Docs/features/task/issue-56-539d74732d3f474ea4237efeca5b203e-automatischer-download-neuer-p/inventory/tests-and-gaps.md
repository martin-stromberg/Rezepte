# Tests und offene technische Lücken

## Vorhandene Tests

`Rezepte.Tests/Services/Import/PluginManagerTests.cs` prüft Discovery, inkompatible oder nicht ladbare Assemblies, Synchronisierung der Pluginsettings und fehlende Plugins. `PluginSettingsServiceTests.cs` prüft Aktivierung und Reihenfolge. SQLite wird in anderen Tests über eine geöffnete In-Memory-Verbindung und den bestehenden `RezepteDbContext` verwendet.

## Nicht abgedeckte Anforderungen

Es fehlen Tests für:

- Quellenpersistenz, Vertrauensbestätigung, Benutzer-/Adminberechtigungen und private Quellen;
- GitHub-Releaseauswahl, exaktes `release.zip`, Versionsermittlung, Authentifizierung, Timeouts, Rate-Limits und Retries;
- geheime PAT-Behandlung sowie Ausschluss aus DTOs und Logs;
- ZIP-Pfadüberläufe, absolute Pfade, unerwartete Inhalte, beschädigte Archive und erlaubte Meta-Dateien;
- isolierte temporäre Discovery und Laufzeitprüfung;
- Installation, Überschreiben, Rollback und Schutz des alten Bestands bei jedem Fehler;
- parallele manuelle/periodische Läufe, Cancellation und Statuspersistenz;
- Reload-Verhalten mit `AssemblyLoadContext` und laufenden Import-Handlern.

## Testbare Architekturgrenzen

Der GitHub-Zugriff sollte über ein Interface mockbar sein. ZIP-Validierung, Runtime-Validierung, Installation und Statuspersistenz sollten ebenfalls isoliert testbar sein. Der Hosted-Service sollte nur die Intervallsteuerung übernehmen und den eigentlichen Updateprozess an einen scoped Dienst delegieren.

Die Tests müssen ausdrücklich verifizieren, dass ein fehlgeschlagener Download, eine ungültige ZIP oder ein fehlgeschlagener Reload weder die aktive Version ersetzt noch eine bereits funktionierende Version als erfolgreich verarbeitet markiert.
