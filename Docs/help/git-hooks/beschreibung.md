← [Zurück zur Übersicht](../index.md)

# Git-Hooks — Beschreibung

## Zweck

Die Git-Hooks sorgen dafür, dass jede Änderung vor dem Commit oder Push automatisch auf Konsistenz, Formatierung, Vollständigkeit der Übersetzungen, XML-Dokumentation und Testabdeckung geprüft wird. Dadurch bleibt der Code im Repository konsistent und wartbar.

## Funktionsweise

Bevor ein neuer Commit geschrieben wird, prüft der `pre-commit`-Hook die gestagten Dateien auf Übersetzungskonsistenz, XML-Dokumentation, Lokalisierung der Benutzeroberfläche, Formatierung und Kodierung. Bevor Änderungen an das Remote-Repository übertragen werden, prüft der `pre-push`-Hook das gesamte Repository auf unvollständige Implementierungen, falsche Verwendung von Razor-Komponenten und nicht abgedeckte Enum-Werte.

## Beispiele

- Ein Entwickler stellt eine neue Razor-Komponente fertig. Der `pre-commit`-Hook überprüft, ob alle sichtbaren Texte über den Localizer aufgelöst sind, bevor der Commit erstellt wird.
- Ein Entwickler baut eine neue API-Methode. Der Hook stellt sicher, dass alle öffentlichen Member mit `///`-Dokumentation versehen sind.
- Beim Push auf einen Feature-Branch prüft der `pre-push`-Hook, dass keine `NotImplementedException`-Stubs und keine ungenutzten Razor-Seiten mehr im Code vorhanden sind.

## Einschränkungen

- Die Hooks verlangen, dass Python, das .NET SDK und PowerShell auf dem Entwicklungsrechner verfügbar sind.
- Direkte Commits und Pushes auf `main` und `staging` werden abgelehnt.
