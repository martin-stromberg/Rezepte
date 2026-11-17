# Detail: Betroffene Dokumentation

## `Docs/help/user-accounts.md`

Abschnitt „Demo-Daten anlegen" → „Technischer Hintergrund" (Zeilen 40–44) beschreibt den aktuellen Ablauf inkl. der Komposition im `DemoDataSeedingJobHandler`:

- Zeile 42: beschreibt, dass `BackgroundJobHostedService` den `DemoDataSeedingJobHandler` ausführt, „der nacheinander `ICookbookService.CreateAsync`, `IRecipeService.CreateAsync`, `ICalendarService.CreateEventAsync` … sowie `IShoppingListService.EnsureDefaultGroupAsync`/`AddItemAsync` … aufruft" und dass der Handler bei Fehlern/Exception den Job auf `Failed` setzt.
- Bei Kapselung in `IDemoDataSeeder` ist dieser Abschnitt anzupassen: Handler = Auslöser (Nutzlast, Benutzerprüfung, Aufruf), Sequenz = `IDemoDataSeeder.SeedForUserAsync`.

## Weitere Fundstellen

- `Docs/RELEASE_NOTES.md` — Release-Notes-Datei (Schritt 12c prüft Ergänzungsbedarf; interne Refaktorisierung ohne Nutzer-Änderung).
- `README.md` — prüfen auf Demo-Daten-/Architektur-Abschnitte (Schritt 12b).
- Keine weiteren Treffer für `DemoDataSeedingJobHandler`/Demo-Seeding in `Docs/` außer den Projekt-/Feature-Artefakten.
