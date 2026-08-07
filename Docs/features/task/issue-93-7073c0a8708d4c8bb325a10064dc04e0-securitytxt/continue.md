# Offene Aufgaben

Erstellt am: 2026-08-07
Abbruchgrund: Maximale Iterationsanzahl erreicht

Die folgenden Aufgaben konnten im automatisierten Zyklus nicht abgeschlossen werden
und müssen manuell oder in einem erneuten Lauf bearbeitet werden.

## Offene Planelemente

(Keine — Plan vollständig umgesetzt)

## Code-Review-Befunde

- [ ] `SettingsService.cs` — **Toter Code:** `WriteNullableString` ist definiert, aber nirgends aufgerufen → entfernen
- [ ] `SettingsService.cs` — **Doppelter Code:** Das Array der 9 `SecurityTxt.*`-Schlüssel wird in `Get-` und `Set-`Methode identisch wiederholt → in ein `static readonly`-Feld auslagern
- [ ] `SettingsService.cs` — **Primitive Obsession:** `SecurityTxtSettings`-Record wird mit 9 positionalen `string?`-Parametern instanziiert → benannte Argumente verwenden
- [ ] `SettingsController.cs` — **Fehlende Eingabevalidierung:** `maxrequestsperhour` / `maxrequestsperday` akzeptieren negative Werte ohne Guard-Bedingung
- [ ] `SettingsViewModel.cs` — **Synchroner Block:** `GetAuthenticationStateAsync().GetAwaiter().GetResult()` im Konstruktor ist ein Deadlock-Risiko in Blazor Server
- [ ] `SettingsServiceTests.cs` — **Fehlende Testabdeckung:** 15+ öffentliche Methoden (alle globalen Toggle-Setter/-Getter außer AI und SecurityTxt) haben keine Tests

## Fehlgeschlagene Tests

(Keine — alle 284 Unit-Tests bestanden; 13 Browser-Tests übersprungen, da Browser-Infrastruktur nicht verfügbar)
