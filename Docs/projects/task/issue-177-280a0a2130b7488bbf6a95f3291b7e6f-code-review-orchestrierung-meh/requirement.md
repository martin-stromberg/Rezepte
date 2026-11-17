# Anforderungsübersetzung

## Metadaten

- Aufgaben-ID: `280a0a21-30b7-488b-bf6a-95f3291b7e6f`
- Branch: `task/issue-177-280a0a2130b7488bbf6a95f3291b7e6f-code-review-orchestrierung-meh`
- Issue: #177 — Code-Review: Orchestrierung mehrstufiger Datenpflege ausserhalb des Services
- Kundenanforderung: „@softwarewerkstatt erledige die Korrekturen für beide Befunde nacheinander und stelle einen pr"

## Ziel

Die beiden Befunde des im Issue #177 dokumentierten außerordentlichen Code-Reviews sollen nacheinander korrigiert und das Ergebnis als Pull Request bereitgestellt werden:

1. **Befund 1 — Divergente Import-Pfade:** Für den Geschäftsvorfall „Rezept importieren" existieren heute zwei produktive Ausführungspfade mit unterschiedlicher Komposition und Nebenbedingungen. Diese sind auf einen einzigen Einstiegspunkt zu konsolidieren.
2. **Befund 2 — Orchestriertes Demo-Seeding im Job-Handler:** Der Geschäftsvorfall „Demo-Daten für einen neuen Benutzer anlegen" wird heute fachlich sequenziert im Infrastruktur-Job-Handler komponiert. Dieser Vorgang ist in einen Service-Einstiegspunkt zu kapseln; der Handler löst nur noch aus.

## Fachliche Anforderungen

### FA-1: Ein einziger Ausführungspfad für „Rezept importieren"

- Es darf produktiv nur noch einen Ausführungspfad für den Geschäftsvorfall „Rezept importieren" geben. Die Konsolidierung erfolgt auf den `ImportOrchestrator` als einzigem Einstiegspunkt (Vorgabe aus dem Review).
- Alle bisherigen produktiven Einstiege führen danach über diesen Pfad:
  - die synchronen Endpunkte `POST api/cookbooks/{id}/import` und `POST api/cookbooks/{id}/import-url` (bisher Pfad B über `IImportService`),
  - die Session-Endpunkte `import-session/*` (bisher Pfad A, unverändert Orchestrator-basiert).
- Für die API-Kompatibilität der synchronen Endpunkte darf intern eine Import-Session gestartet und synchron auf das Ergebnis gewartet werden (Vorgabe aus dem Review).
- `IImportService`/`ImportService` ist als eigenständiger Ausführungspfad zu beseitigen — entweder durch Entfernen oder durch reine Delegation an den Orchestrator-Einstiegspunkt; beide Varianten erfüllen die Vorgabe.
- Mindestanforderung unabhängig von der Umsetzungsform: Die Quell-URI (`uri`) muss auch über die bisher synchronen Endpunkte bis in den Handler durchgereicht werden, damit `SourceUri` gesetzt wird und die Duplikatserkennung über `FindByUri(SourceUri)` greift. Doppel-Importe derselben URL müssen erkannt werden.
- Die bislang nur im Orchestrator-Pfad wirksamen Nebenbedingungen gelten anschließend für alle Einstiege: Collection-Preview (`ICollectionImportHandler`/`TryReadCollectionPreviewAsync`), interaktive Handler (`IInteractiveImportHandler`) und Item-Status/Mehrfach-Import aus Collections.

### FA-2: Service-Einstiegspunkt für „Demo-Daten seeden"

- Der Geschäftsvorfall „Demo-Daten für einen Benutzer anlegen" wird als ein Einstiegspunkt im Service-Layer gekapselt — in Richtung `IDemoDataSeeder.SeedForUserAsync(userId, ct)` nach dem Muster `ExecutePassAsync` (Vorgabe aus dem Review).
- Der `DemoDataSeedingJobHandler` löst den Geschäftsvorfall nur noch aus; fachliches Sequenzwissen (Kochbücher → Rezepte inkl. Bilder → Kalendereinträge → Einkaufsliste) und die zugehörigen Nebenbedingungen liegen im Service.
- Der ausgelöste Aufrufweg bleibt fachlich unverändert: Registrierung (`UserService.RegisterAsync`) stellt weiterhin den Hintergrund-Job ein.
- Das fachliche Ergebnis des Seedings bleibt unverändert: gleiche Demo-Kochbücher, -Rezepte inklusive Bilder, fünf Kalenderevents (18:00 Uhr) sowie Einkaufsliste in der Standardgruppe.

## Betroffene Bereiche

- `Rezepte.Web/Services/Import/ImportOrchestrator.cs` — Ziel-Einstiegspunkt (StartImportAsync, Zeilen ca. 104–278, Collection-Verarbeitung ca. 404–508)
- `Rezepte.Web/Services/Import/ImportService.cs` und `IImportService.cs` — bisheriger zweiter Ausführungspfad (ImportAsync, Zeilen 33–79; `uri` hart `null` in Zeile 66)
- `Rezepte.Web/Controllers/CookbooksController.cs` — Legacy-Endpunkte `import`/`import-url` (ca. Zeilen 165–195, 205–255, 575–602) sowie Session-Endpunkte `import-session/*` (ca. Zeile 630)
- `Rezepte.Web/Components/.../CookbookPage.razor` (Aufrufe ca. Zeilen 286, 322) und `CreateRecipeDialog.razor` (ca. Zeilen 305, 431, 814) — produktive Aufrufer
- `Rezepte.Web/Services/Import/BaseAIImportHandler.cs` (Zeile 293: `SourceUri = uri`) und `ImportedRecipePersister.cs` (Zeilen 63–65: Dedup über `FindByUri`)
- `Rezepte.Web/Services/BackgroundJobs/Handlers/DemoDataSeedingJobHandler.cs` (Komposition in `HandleAsync`, ca. Zeilen 55–78) und `Rezepte.Web/Services/UserService.cs` (Job-Anlage bei Registrierung, ca. Zeilen 154–159)
- Zugehörige Tests in `Rezepte.Tests` (u. a. `ImportServicePluginTests`, `ImportOrchestratorTests`)

## Abläufe

### Ist-Ablauf Import

- **Pfad A (Session, vollständig):** `CreateRecipeDialog` → `import-session/*` → `ImportOrchestrator.StartImportAsync` → Handler-Scan → Collection-Preview → `CanHandleAsync` → `HandleInteractiveAsync`/`HandleAsync` mit `uri` → `IImportedRecipePersister`.
- **Pfad B (synchron, reduziert):** `CookbookPage` → `api/cookbooks/{id}/import` bzw. `/import-url` → `IImportService.ImportAsync` → `CanHandleAsync` → `HandleAsync` mit `uri: null` → `PersistAsync`. Folgen: keine `SourceUri`, wirkungslose Dedup, mögliche Doppel-Importe; kein Collection-Preview; `IInteractiveImportHandler` wird ignoriert.
- Externe API-Konsumenten können heute beide Pfade wählen.

### Soll-Ablauf Import

- Alle genannten Einstiege führen auf den Orchestrator-Pfad. Die synchronen Endpunkte behalten ihre API-Signatur und ihr Antwortverhalten nach außen, delegieren die Ausführung aber an den einzigen Ausführungspfad (ggf. Session starten und Ergebnis synchron abwarten).

### Ist-Ablauf Demo-Seeding

- `UserService.RegisterAsync` → Job-Anlage → `DemoDataSeedingJobHandler.HandleAsync` komponiert `ICookbookService.CreateAsync` → `IRecipeService.CreateAsync`/`AddImageAsync` → `ICalendarService.CreateEventAsync` → `IShoppingListService.EnsureDefaultGroupAsync`/`AddItemAsync` in fachlicher Reihenfolge.

### Soll-Ablauf Demo-Seeding

- `UserService.RegisterAsync` → Job-Anlage (unverändert) → `DemoDataSeedingJobHandler` ruft `IDemoDataSeeder.SeedForUserAsync(userId, ct)`; die fachliche Sequenz liegt im Service-Layer.

## Konfiguration

- Keine Konfigurationsänderungen vorgesehen oder aus dem Review gefordert.
- Plugin-Registrierung und konfigurierte Handler-Reihenfolge bleiben unverändert.
- Job-Registrierung (`DemoDataSeedingJobHandler.JobTypeName`) und Auslöser bei der Registrierung bleiben unverändert.

## Akzeptanzkriterien

- Importe über `api/cookbooks/{id}/import` und `/import-url` zeigen dasselbe Ausführungsverhalten wie Importe über die Session-API: gleiche Handler-Reihenfolge, gleiche Persistierung, gleiche Nebenbedingungen (Collection-Preview, interaktive Handler).
- Bei URL-Importen — über jeden der bisherigen Einstiege — wird `SourceUri` auf dem importierten Rezept gesetzt; ein erneuter Import derselben URL wird als Duplikat erkannt statt ein zweites Rezept anzulegen.
- `IImportService`/`ImportService` bildet keinen eigenständigen Ausführungspfad mehr (entfernt oder reine Delegation an den Orchestrator).
- Für den Geschäftsvorfall „Demo-Daten anlegen" existiert ein Service-Einstiegspunkt (Richtung `IDemoDataSeeder.SeedForUserAsync`); der Job-Handler enthält keine fachliche Sequenzkomposition mehr und löst nur noch aus.
- Die per Registrierung erzeugten Demo-Daten sind fachlich unverändert (Kochbücher, Rezepte mit Bildern, fünf Kalenderevents um 18:00, Einkaufsliste in Standardgruppe).
- Die Solution baut erfolgreich; die vollständige Testsuite läuft grün. Bestehende Tests werden an den konsolidierten Einstiegspunkt angepasst und bauen keine abweichenden mehrstufigen Sequenzen nach.
- Es wird ein Pull Request mit den Korrekturen beider Befunde bereitgestellt.

## Explizite Nicht-Ziele

- Kein Umbau der Plugin-Update-Pipeline (Plugin-Updates, Update-Prüfung).
- Keine Änderung an legitimer Orchestrierung, z. B. Export-Job-Handler/Export-Cleanup.
- Keine neue Import-Funktionalität (keine neuen Formate, Quellen oder API-Endpunkte).
- Keine Änderung am Plugin-Vertrag (`IImportHandler`-Signatur) oder an der Plugin-Architektur.
- Keine inhaltliche Änderung der Demo-Daten.
- Keine Änderung des Registrierungsablaufs selbst (Job-Auslösung bleibt bestehen).

## Offene Fragen

- Synchrones Abwarten auf interaktive Imports: Wenn über die Legacy-Endpunkte ein Collection-Import läuft, der eine Benutzerauswahl erfordert, kann synchrones Warten auf das Session-Ergebnis blockieren. Aus dem Review ist nur „ggf. Session starten und synchron auf session.Result warten" vorgegeben; das Verhalten in diesem Fall (Fehlerrückgabe, Nicht-Interaktiv-Verhalten des Handlers) ist nicht festgelegt.
- Atomarität des Demo-Seedings: Das Review benennt die fehlende Atomarität als Beobachtung, der Korrekturvorschlag verlangt nur die Kapselung. Ob der gekapselte Vorgang zusätzlich transaktional abgesichert werden soll, ist nicht vorgegeben.
