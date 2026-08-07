# Offene Aufgaben

Erstellt am: 2026-08-07
Abbruchgrund: Kein Fortschritt zwischen den letzten zwei Iterationen (neue Befunde aus Code-Review Iteration 2 können in automatisiertem Zyklus nicht weiter reduziert werden)

Die folgenden Aufgaben konnten im automatisierten Zyklus nicht abgeschlossen werden
und müssen manuell oder in einem erneuten Lauf bearbeitet werden.

## Offene Planelemente

(Keine — Plan vollständig umgesetzt)

## Code-Review-Befunde (Iteration 1 – erledigt)

- [x] `SettingsService.cs` — **Toter Code:** `WriteNullableString` entfernt
- [x] `SettingsService.cs` — **Doppelter Code:** Array der 9 `SecurityTxt.*`-Schlüssel in `SecurityTxtKeys`-Feld ausgelagert
- [x] `SettingsService.cs` — **Primitive Obsession:** Benannte Argumente beim `SecurityTxtSettings`-Konstruktor verwendet
- [x] `SettingsController.cs` — **Eingabevalidierung:** Guard-Bedingungen für negative Werte bei `maxrequestsperhour` / `maxrequestsperday` ergänzt
- [x] `SettingsViewModel.cs` — **Synchroner Block:** `GetAwaiter().GetResult()` entfernt, async `InitializeAsync()` eingeführt
- [x] `SettingsServiceTests.cs` — **Fehlende Testabdeckung:** 15 neue Tests für globale Toggle-Methoden ergänzt

## Code-Review-Befunde (Iteration 2 – offen)

- [ ] `SecurityTxtController.cs` — **Doppelter Code:** Die drei Action-Methoden folgen exakt demselben Muster → private Hilfsmethode extrahieren
- [ ] `SecurityTxtRenderer.cs` — **God-Methode / Doppelter Code:** Feld-Katalog dreifach repliziert in `RenderPlainText`, `RenderMarkdown`, `RenderHtml` → gemeinsamen Durchlauf extrahieren
- [ ] `SecurityTxtRenderer.cs` — **Fehlende HTML-Struktur:** `RenderHtml` liefert kein gültiges HTML-Dokument (kein `<html><body>`) → minimales HTML-Grundgerüst ergänzen
- [ ] `SecurityTxtRenderer.cs` — **Primitive Obsession in `AppendHtmlSection`:** Mehrzeilige Werte werden als einzelner `<p>` ausgegeben, Zeilenumbrüche ignoriert → `Split('\n')` wie in `AppendMultiline` verwenden
- [ ] `SettingsController.cs` — **Fehlende Null-Prüfung:** `SetGlobalSecurityTxt` prüft `settings` nicht auf `null` → `if (settings == null) return BadRequest(...)` ergänzen
- [ ] `SecurityTxtSettings.razor` — **Doppelter Code / Primitive Obsession:** `SecurityTxtForm` ist 1:1-Kopie des DTOs → `SecurityTxtForm` entfernen und direkt `SecurityTxtSettings` binden
- [ ] `SecurityTxtSettings.razor` — **Fragiles Event-Handling:** `OnEnabledChanged` parst `args.Value` manuell → `@bind` mit `@bind:event="onchange"` verwenden
- [ ] `SecurityTxtControllerTests.cs` — **Doppelter Testfall:** `GetWellKnownSecurityTxt_ReturnsOk_WhenEnabled` ist inhaltliches Duplikat → entfernen oder durch echten Integrationstest ersetzen
- [ ] `SettingsServiceTests.cs` — **Mehrere fachliche Fälle in einem Test:** `ShoppingListEditMode_ShouldPersistInitialValuePerUser` prüft drei Fälle → in drei separate `[Fact]`-Methoden aufteilen

## Fehlgeschlagene Tests

(Keine — alle 284 Unit-Tests bestanden; 13 Browser-Tests übersprungen, da Browser-Infrastruktur nicht verfügbar)
