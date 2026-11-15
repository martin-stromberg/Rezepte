# Projektplan-Prüfung — Issue #177

## Ergebnis

**Status:** Projektplan vollständig

## Abgleich Anforderung ↔ Entwicklungsschritte

| Anforderung (requirement.md) | Entwicklungsschritt | Abdeckung |
|---|---|---|
| FA-1: Ein einziger Ausführungspfad über `ImportOrchestrator` als einzigem Einstiegspunkt | Schritt 1 | Vollständig — Ziel „ein einziger Ausführungspfad", Orchestrator bleibt einziger Einstieg explizit benannt. |
| FA-1: Alle bisherigen produktiven Einstiege führen über diesen Pfad (`POST api/cookbooks/{id}/import`, `.../import-url`, Session-Endpunkte unverändert) | Schritt 1 | Vollständig — synchrone Endpunkte inkl. der Varianten ohne Kochbuch (Fundstellen `CookbooksController.cs` ca. 165–272, 575–602) sowie die unveränderten `import-session/*`-Endpunkte benannt; Aufrufer Kochbuch-Seite und Rezept-Anlegen-Dialog genannt. |
| FA-1: Intern darf eine Import-Session gestartet und synchron auf das Ergebnis gewartet werden (API-Kompatibilität) | Schritt 1 | Vollständig — „starten intern jedoch eine Import-Session und warten synchron auf deren Endzustand" bei unveränderter API-Signatur/Antwortverhalten. |
| FA-1: `IImportService`/`ImportService` als eigenständiger Pfad beseitigen (Entfernen oder reine Delegation) | Schritt 1 | Vollständig — beide Varianten explizit als zulässig benannt, inkl. Dateifundstellen `ImportService.cs`, `IImportService.cs`. |
| FA-1 (Mindestanforderung): Quell-URI bis in den Handler durchreichen → `SourceUri` setzen → Dedup über `FindByUri` greift; Doppel-Importe erkannt | Schritt 1 | Vollständig — als Muss formuliert und als Akzeptanzkriterium wiederholt („erneuter Import derselben URL wird als Duplikat erkannt"). |
| FA-1: Orchestrator-Nebenbedingungen gelten für alle Einstiege (Collection-Preview, `IInteractiveImportHandler`, Item-Status/Mehrfach-Import) | Schritt 1 | Vollständig — im Ist-/Soll-Teil und in den Akzeptanzkriterien enthalten (inkl. der geklärten Einschränkung: tatsächlicher Interaktionsbedarf → Abbruch + Fehler statt Interaktion). |
| FA-2: Service-Einstiegspunkt Richtung `IDemoDataSeeder.SeedForUserAsync(userId, ct)` nach Muster `ExecutePassAsync` | Schritt 2 | Vollständig — Review-Vorgabe wörtlich übernommen. |
| FA-2: `DemoDataSeedingJobHandler` löst nur noch aus; fachliches Sequenzwissen (Kochbücher → Rezepte+Bilder → Kalender → Einkaufsliste) inkl. Nebenbedingungen im Service | Schritt 2 | Vollständig — Sequenz, Fehlerabbrüche je Schritt und tolerante Bildbehandlung beschrieben; Handler-Rest „Nutzlast lesen, Benutzer prüfen, Service aufrufen" benannt. |
| FA-2: Auslösender Aufrufweg unverändert (Registrierung stellt weiterhin Job ein) | Schritt 2 | Vollständig — explizit als Randbedingung. |
| FA-2: Fachliches Ergebnis unverändert (gleiche Kochbücher/Rezepte+Bilder, 5 Kalenderevents 18:00, Einkaufsliste Standardgruppe) | Schritt 2 | Vollständig — als Akzeptanzkriterium. |
| Geklärte Randbedingung: Interaktionsbedarf über Legacy-Endpunkt → Session abbrechen + klare Fehlermeldung mit Hinweis auf `import-session/*`, keine neue Interaktionsmöglichkeit | Schritt 1 | Vollständig — Auslöser eindeutig (Sammlungsauswahl bei Collections, Bestätigung durch interaktiven Handler), Reaktion eindeutig (Abbruch + Fehlermeldung mit Hinweis), Ausschluss (kein neuer Interaktionsbau) benannt; als Akzeptanzkriterium überprüfbar. |
| Geklärte Randbedingung: Kein transaktionaler Umbau des Seedings; Teilanlage bei Fehlern ohne Retry bleibt | Schritt 2 | Vollständig — als geklärte Randbedingung und Akzeptanzkriterium. |
| AK: Gleiches Ausführungsverhalten an allen Einstiegen | Schritt 1 | Vollständig — Akzeptanzkriterium deckt Handler-Reihenfolge, Persistierung, Nebenbedingungen ab. |
| AK: Solution baut, Testsuite grün; Tests an konsolidierten Einstieg angepasst, keine nachgebauten mehrstufigen Sequenzen | Schritt 1 + 2 | Vollständig — Schritt 1: `ImportServicePluginTests` entfallen/ziehen um, `ImportOrchestratorTests` Referenz + ggf. URI-Assertion. Schritt 2: `DemoDataSeedingJobHandlerTests` verlagern Sequenz-Assertions auf Service-Tests, `UserServiceTests` und Browser-`RegisterTests` bleiben als E2E-Nachweis. |
| AK: Pull Request mit beiden Korrekturen | Übersicht | Abgedeckt — als Plan-Ziel im Kopf genannt (PR-Erstellung liegt außerhalb der Entwicklungsschritte, workflowseitig abgedeckt). |
| Nicht-Ziele (Plugin-Update-Pipeline, legitime Orchestrierung, neue Endpunkte/Funktionalität, Plugin-Vertrag, Demo-Daten-Inhalte, Registrierungsablauf) | Schritt 1 + 2 | Nicht verletzt — Schritt 1 nennt explizit „keine neuen Endpunkte, keine neue Import-Funktionalität, Plugin-Vertrag/-Registrierung/Handler-Reihenfolge unverändert"; Schritt 2 nennt unveränderte Demo-Daten, Job-Registrierung und Registrierungsauslöser. Plugin-Update-Pipeline und Export-Handler werden nicht angefasst. |

## Abhängigkeitsprüfung

- Schritt 1: „Keine" — korrekt, kein Verweis auf spätere Schritte.
- Schritt 2: „Keine" — korrekt, fachlich unabhängig von Schritt 1 (unterschiedliche Codebereiche: Import vs. BackgroundJob/Service-Layer). Keine Vorwärtsverweise, zyklenfrei.
- Reihenfolge konsistent mit der Benutzervorgabe „nacheinander": Schritt 1 = Import-Konsolidierung (Befund 1), Schritt 2 = Demo-Seeding (Befund 2); die Begründung im Plan (fachlich unabhängig, Reihenfolge = Vorgabe) ist schlüssig. „Abhängigkeiten: Keine" widerspricht der sequenziellen Abarbeitung nicht, da die Schrittnummerierung die Ausführungsreihenfolge festlegt.

## Fehlende oder unvollständige Punkte

Keine blockierenden Lücken. Alle Muss-Anforderungen sind mindestens einem Schritt zugeordnet und in dessen Beschreibung als Lifecycle-Eingabe verwendbar (Kontext, Zielrichtung, Randbedingungen, überprüfbare Akzeptanzkriterien vorhanden).

Nicht-blockierende Unschärfen:

1. **Fehlende Warte-Mechanik nicht als Kontext benannt (Schritt 1):** Der `ImportOrchestrator` hat heute keinen Completion-Mechanismus — `session.Result` wird im Hintergrund-`Task.Run` gesetzt, Konsumenten pollen (Inventory `import-pfade.md`, Abschnitt „`session.Result`-Setzung und Warte-Mechanik"). Das „synchron auf deren Endzustand warten" muss also neu gebaut werden (Polling auf `session.Result`/`State` oder neues Completion-Primitive) und der Abbruch muss über die vorhandenen Orchestrator-APIs (`CancelSelection`, `Confirm(…, accepted: false)`) erfolgen. Der Plan beschreibt das Zielverhalten eindeutig und umsetzbar, nennt aber diese Ausgangslage nicht — die Lifecycle-Umsetzung wird sie im Code finden, ein expliziter Hinweis hätte die Schrittbeschreibung noch vollständiger gemacht.
2. **Testabdeckung des neuen Abbruchverhaltens (Schritt 1):** Die Akzeptanzkriterien fordern „Interaktionsbedarf → Abbruch + Fehlermeldung", der Absatz „Anzupassende Tests" nennt aber nur Umzug/Entfall bestehender Tests und eine optionale URI-Durchreich-Assertion — keine explizite Testanforderung für das neue Warten/Abbruch-Verhalten an den synchronen Endpunkten. Über die Akzeptanzkriterien indirekt abgedeckt, aber nicht als Testpunkt benannt.
3. **DI-Verkabelung nicht erwähnt:** Weder das Entfernen/Anpassen der Registrierung `AddScoped<IImportService, ImportService>` (`ServiceCollectionExtensions.cs:201`) noch die Registrierung des neuen `IDemoDataSeeder`-Services (und dessen scoped Abhängigkeiten) ist benannt. Reine Umsetzungsdetails, keine Anforderungslücke.
4. **Doku-Anpassung Schritt 1 nur „bei Bedarf":** `Docs/help/import-plugins.md` beschreibt laut Inventory einen Ist-Importablauf, der sich faktisch ändert — die Formulierung „bei Bedarf" ist schwächer als nötig. Schritt 2 benennt die Anpassung von `user-accounts.md` dagegen verbindlich.

## Hinweise

- Die Beschreibung des Interaktions-Abbruchs in Schritt 1 ist eindeutig und umsetzbar: Beide Auslösezustände (Collection-Sammlungsauswahl, interaktive Bestätigung) sind benannt, die Reaktion (Session abbrechen, klare Fehlermeldung mit Hinweis auf `import-session/*`) ist prüfbar, und der Orchestrator bietet mit `CancelSelection`/`Confirm(id, user, false)` bereits die nötigen Abbruch-Einstiege — kein neuer Interaktionspfad erforderlich.
- Leichte Spannung, aber konsistent: Das Akzeptanzkriterium „gleiche Nebenbedingungen inkl. Collection-Preview und interaktiver Handler" und die Randbedingung „Interaktionsbedarf → Abbruch" lesen sich zusammen korrekt als „gleiche Pipeline-Stufen laufen, bei tatsächlichem Interaktionsbedarf greift die definierte Abbruch-Semantik statt Interaktion".
- UI-Aufrufer: `CookbookPage.razor` braucht voraussichtlich keine Anpassung (API-Signatur und Antwortverhalten bleiben, Fehlermeldung läuft über bestehende JS-Alert-Fehlerbehandlung); der Plan nennt die Seite korrekt als betroffenen Einstieg, ohne einen unbegründeten UI-Umbau zu fordern.
- Alle in `requirement.md` genannten Betroffenen Bereiche (Orchestrator, ImportService/IImportService, Controller-Endpunkte inkl. Varianten ohne Kochbuch, `BaseAIImportHandler`/`ImportedRecipePersister` als Dedup-Kontext, `DemoDataSeedingJobHandler`, `UserService.RegisterAsync`, Tests) sind in den Schritten direkt oder über Fundstellen abgedeckt.
