# Anforderungsübersetzung

## Metadaten

- Aufgaben-ID: `280a0a21-30b7-488b-bf6a-95f3291b7e6f` (Projekt Issue #177, Schritt 1 von 2)
- Branch: `task/issue-177-280a0a2130b7488bbf6a95f3291b7e6f-code-review-orchestrierung-meh-schritt-1-import-pfad`
- Basisbranch: `task/issue-177-280a0a2130b7488bbf6a95f3291b7e6f-code-review-orchestrierung-meh`
- Kundenanforderung: „Ein einziger Ausführungspfad für ‚Rezept importieren'" (Entwicklungsschritt 1 des Projektplans zu Issue #177)

## Fachliche Zusammenfassung

Der Geschäftsvorfall „Rezept importieren" wird von zwei produktiven Ausführungspfaden auf einen einzigen konsolidiert: den Session-basierten Pfad über `ImportOrchestrator`. Die synchronen Legacy-Endpunkte (`POST api/cookbooks/{id}/import`, `POST api/cookbooks/{id}/import-url` sowie die Varianten ohne Kochbuch) behalten ihre API-Signatur und ihr Antwortverhalten, führen die Ausführung aber intern über eine Import-Session aus und warten synchron auf deren Endzustand. Der bisherige eigenständige Ausführungspfad (`IImportService`/`ImportService`) wird beseitigt; die Quell-URI wird auch über die bisher synchronen Endpunkte bis in den Handler durchgereicht, sodass `SourceUri` gesetzt wird und die Duplikatserkennung über `FindByUri` greift.

## Fachlicher Scope

- **Verarbeitungseinheit:** Eine Import-Ausführung des Geschäftsvorfalls „Rezept importieren" — ausgelöst über einen der HTTP-Endpunkte, unabhängig davon, ob Datei- oder URL-Import, mit oder ohne Ziel-Kochbuch.
- **Auslösende Ereignisse:** HTTP-POST auf die synchronen Endpunkte `api/cookbooks/{id}/import`, `api/cookbooks/{id}/import-url`, `api/cookbooks/import`, `api/cookbooks/import-url` (Aufrufer u. a. `CookbookPage.razor`, externe API-Konsumenten) sowie unverändert die `import-session/*`-Endpunkte (Aufrufer u. a. `CreateRecipeDialog.razor`).
- **Erwartete Wirkung:** Importe über die synchronen Endpunkte erzeugen dasselbe fachliche Ergebnis wie über die Session-API: gleiche Handler-Reihenfolge, gleiche Persistierung über `IImportedRecipePersister`, `SourceUri` auf dem importierten Rezept gesetzt, Duplikatserkennung bei erneutem Import derselben URL (Update statt zweitem Rezept). Benötigt die Ausführung eine Benutzerinteraktion (Sammlungsauswahl bei Collections, Bestätigung durch `IInteractiveImportHandler`), wird die interne Session abgebrochen und der Aufrufer erhält eine klare Fehlerrückmeldung mit Hinweis auf die `import-session/*`-Endpunkte.
- **Umgang mit fehlenden Objekten:**
  - Ziel-Kochbuch nicht vorhanden: verwerfen — bisheriges `NotFound`-Verhalten der Endpunkte bleibt unverändert.
  - Import-Session: erzeugen — die synchronen Endpunkte erzeugen intern eine Session über `ImportOrchestrator.StartImportAsync` und verwerfen sie nach dem Endzustand (kein neues dauerhaftes Objekt nach außen).
  - Bereits importiertes Rezept mit gleicher `SourceUri`: wiederverwenden — Duplikatserkennung aktualisiert das bestehende Rezept statt ein zweites anzulegen (bestehendes `ImportedRecipePersister`-Verhalten).
- **Scope-Ausschlüsse:**
  1. Keine neue Interaktionsmöglichkeit in den synchronen Endpunkten: Anforderungen, die Sammlungsauswahl oder Bestätigung benötigen, werden über die synchronen Endpunkte nicht ausgeführt, sondern mit Fehlerhinweis auf `import-session/*` abgelehnt. — Status: bestätigt (im Entwicklungsschritt explizit als „geklärte Randbedingung" vorgegeben).
  2. Externe API-Konsumenten, die bisher interaktionsbedürftige Inhalte synchron importiert hätten (bisher: stiller Fallback auf nicht-interaktives Verhalten), erhalten künftig eine Fehlerrückmeldung statt eines Imports. — Status: bestätigt (direkte Folge der geklärten Randbedingung).

## Betroffene Klassen und Komponenten

- `Rezepte.Web/Services/Import/ImportService.cs` + `IImportService.cs` — bisheriger zweiter Ausführungspfad; wird entfernt oder zu reiner Delegation an den Orchestrator.
- `Rezepte.Web/Services/Import/ImportOrchestrator.cs` — einziger verbleibender Ausführungseinstieg; benötigt ggf. einen Mechanismus zum synchronen Abwarten des Session-Endzustands (heute: `session.Result` nur per Polling).
- `Rezepte.Web/Controllers/CookbooksController.cs` — vier synchrone Endpunkte (ca. Zeilen 165–272) und privater Helfer `ImportFromUrlAsync` (ca. Zeilen 575–602) werden auf den Orchestrator-Einstieg umgestellt.
- `Rezepte.Web/Extensions/ServiceCollectionExtensions.cs` — DI-Registrierung von `IImportService` entfällt bzw. ändert sich.
- `Rezepte.Web/Services/Import/ImportedRecipePersister.cs` — unverändert (Dedup über `FindByUri`).
- `Rezepte.Web/Services/Import/BaseAIImportHandler.cs` — unverändert (setzt `SourceUri = uri`).
- Tests: `Rezepte.Tests/Services/Import/ImportServicePluginTests.cs` (entfällt oder zieht auf Orchestrator-Einstieg um), `ImportOrchestratorTests.cs` (Referenz, ggf. URI-Durchreich-Assertion), ggf. neue Tests für das Abbruch-Verhalten bei Interaktionsbedarf über synchrone Endpunkte.
- Feature-Doku: `Docs/help/import-plugins.md` (Abschnitt „Importablauf") ggf. an konsolidierten Pfad anpassen.

## Implementierungsansatz

- Die synchronen Endpunkte delegieren an `ImportOrchestrator.StartImportAsync` (bereits vorhandener Einstieg, der `uri` durchreicht) und warten synchron auf `session.Result`.
- Der Orchestrator erhält einen Wartemechanismus auf den Session-Endzustand (z. B. Completion-`TaskCompletionSource` oder Poll-Warte); bei `SelectionRequired`/interaktiver Bestätigung wird die Session abgebrochen und eine Fehlerrückmeldung mit Hinweis auf `import-session/*` zurückgegeben.
- `ImportFromUrlAsync` im Controller reicht die validierte `uri` weiter (heute verworfen).
- `IImportService`/`ImportService` und deren DI-Registrierung werden entfernt (Entfernen statt Delegation ist zulässig und einfacher; finale Entscheidung im Plan).
- Plugin-Vertrag (`IImportHandler` u. a.), Plugin-Registrierung und konfigurierte Handler-Reihenfolge bleiben unverändert — eingefrorener Vertrag, kein Nicht-Ziel-Bruch.

## Konfiguration

Keine Konfigurationsänderungen erforderlich.

## Akzeptanzkriterien

1. Importe über die synchronen Endpunkte zeigen dasselbe Ausführungsverhalten wie über die Session-API (gleiche Handler-Reihenfolge, gleiche Persistierung, gleiche Nebenbedingungen inkl. Collection-Preview und interaktiver Handler).
2. Bei URL-Importen über jeden bisherigen Einstieg wird `SourceUri` gesetzt; ein erneuter Import derselben URL wird als Duplikat erkannt statt ein zweites Rezept anzulegen.
3. Eine benötigte Interaktion über einen synchronen Endpunkt führt zu Session-Abbruch plus klarer Fehlermeldung mit Hinweis auf `import-session/*`.
4. Der bisherige zweite Pfad (`IImportService`/`ImportService`) bildet keinen eigenständigen Ausführungspfad mehr.
5. Tests des bisherigen zweiten Pfads entfallen oder ziehen auf den Orchestrator-Einstieg um; Orchestrator-Tests bleiben Referenz und werden ggf. um eine URI-Durchreich-Assertion ergänzt; bestehende Tests bauen keine abweichenden mehrstufigen Sequenzen nach.
6. Die Solution baut, die Testsuite läuft grün; die Feature-Doku zum Importablauf wird bei Bedarf an den konsolidierten Pfad angepasst.

## Explizite Nicht-Ziele

- Keine neuen Endpunkte und keine neue Import-Funktionalität (keine neuen Formate, Quellen).
- Keine Änderung am Plugin-Vertrag (`IImportHandler`-Signatur), an der Plugin-Registrierung oder an der konfigurierten Handler-Reihenfolge.
- Keine Änderung der `import-session/*`-Endpunkte und deren API-Verhalten.
- Keine neue Interaktionsmöglichkeit in den synchronen Endpunkten.
- Kein Umbau der Demo-Seeding-Orchestrierung (eigener Entwicklungsschritt 2).

## Offene Fragen

- Keine. Die aus dem Projekt-Review offen gebliebene Frage zum synchronen Abwarten interaktiver Importe wurde im Entwicklungsschritt geklärt (Session abbrechen, Fehler mit Hinweis auf `import-session/*`).
