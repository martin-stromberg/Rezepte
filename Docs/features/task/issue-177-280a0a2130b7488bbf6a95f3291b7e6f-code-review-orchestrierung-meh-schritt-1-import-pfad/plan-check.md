# Plan-Gegenprüfung

## Ergebnis

**Status:** Plan vollständig

## Abgleich Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| Synchrone Endpunkte = gleiches Ausführungsverhalten wie Session-API (Handler-Reihenfolge, Persistierung, Collection-Preview, interaktive Handler) | Alle vier Endpunkte delegieren an `ImportOrchestrator.ImportAndWaitAsync` → intern `StartImportAsync` (dieselbe Schleife, Reihenfolge, Persistierung) | `ImportAndWaitAsync_ShouldReturnResult_WhenHandlerSucceeds`, `..._WhenNoPluginCanHandle`, bestehende `StartImportAsync_*`-Tests bleiben Referenz | Abgedeckt |
| `SourceUri` bei URL-Importen über jeden bisherigen Einstieg; Re-Import derselben URL → Duplikat statt zweitem Rezept | `ImportFromUrlAsync` reicht `uri.ToString()` an `ImportAndWaitAsync` → `HandleAsync`/`TryReadCollectionPreviewAsync` → `SourceUri` → `PersistRecipeAsync`-Dedup (`FindByUri`) | `StartImportAsync_ShouldPassUriToHandler` (inkl. `ReceivedUri`-Aufzeichnung); Dedup-Mechanik unverändert in `ImportedRecipePersister` | Abgedeckt |
| Benötigte Interaktion → Session-Abbruch + klare Fehlermeldung mit `import-session/*`-Hinweis | `InteractionRequiredSource`-Signal an beiden Interaktionsübergängen; `AbortInteractiveSession` cancelt `SelectionTcs`/`ConfirmationTcs`; `InteractionRequiredMessage` mit Hinweis | `ImportAndWaitAsync_ShouldAbortAndReturnInteractionError_WhenConfirmationRequired`, `..._WhenSelectionRequired` | Abgedeckt |
| Bisheriger zweiter Pfad kein eigenständiger Ausführungspfad mehr | `IImportService`/`ImportService` entfernt, DI-Registrierung entfernt, `ImportServicePluginTests` gelöscht | Build (Referenzbruch würde sofort auffallen) + Suite-Lauf | Abgedeckt |
| Tests des zweiten Pfads entfallen/ziehen um; Orchestrator-Tests Referenz + URI-Assertion; keine abweichenden mehrstufigen Sequenzen | Löschung `ImportServicePluginTests`; Erweiterung `ImportOrchestratorTests` | Neue Tests im Plan genannt | Abgedeckt |
| Solution baut, Testsuite grün, Feature-Doku bei Bedarf angepasst | Umsetzungsschritt 6 + Task 11 (`import-plugins.md`) | Build-/Testlauf in Schritt 10 | Abgedeckt |

## Fehlende oder unvollständige Testanforderungen

Keine — Status `Plan vollständig`.

## E2E-Abdeckung

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| Import über `CookbookPage` (Datei/URL) | Kein neuer E2E-Test — API-Vertrag der Endpunkte unverändert, kein geänderter UI-Fluss; es existiert auch heute kein Browser-Import-Test | Nicht erforderlich mit Begründung |
| `import-session/*`-Fluss (`CreateRecipeDialog`) | Unverändert — kein Testbedarf | Nicht erforderlich mit Begründung |

## Fehlende oder unvollständige Planbestandteile

Keine — Status `Plan vollständig`.

## Hinweise

- Die Duplikatserkennung selbst (`ImportedRecipePersister.PersistRecipeAsync` → `FindByUri`) ist heute nicht direkt unit-testabgedeckt; der Nachweis des Schritts ruht auf der `uri`-Durchreichung. Empfehlung für die Implementierung: den Passthrough-Persister in `ImportOrchestratorTests` so erweitern, dass er die `SourceUri` persistierter `ImportedRecipe`s aufzeichnet — dann belegt `StartImportAsync_ShouldPassUriToHandler` die vollständige Kette `uri`-Parameter → `ImportedRecipe.SourceUri` → Persister.
- `ImportAndWaitAsync` wartet ohne eigenes Timeout; Abbruch erfolgt über den Request-`CancellationToken`. Entspricht dem bisherigen synchronen Verhalten (Handler wurde direkt awaited) und ist im Plan als Risiko dokumentiert.
