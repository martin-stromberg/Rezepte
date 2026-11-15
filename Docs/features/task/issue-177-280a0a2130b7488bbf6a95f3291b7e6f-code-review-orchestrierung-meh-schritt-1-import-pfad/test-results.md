# Test-Ergebnisse

## Ergebnis

**Status:** Keine Fehler (automatisierte Tests) — Reale End-to-End-Abnahme: Nicht durchführbar, Anwender-Entscheidung erforderlich (siehe unten).

## Fehlgeschlagene Tests

Keine.

## E2E-Abdeckung

Der Plan sieht keine neuen E2E-Tests vor (begründet: kein geänderter UI-/Benutzerfluss; API-Vertrag der synchronen Endpunkte unverändert). Es existiert auch im Ausgangszustand kein Browser-E2E-Test für die Import-Endpunkte.

## Reale End-to-End-Abnahme

Die Anforderung berührt Persistenz und HTTP-Endpunkte → reale Abnahme grundsätzlich erforderlich.

| Szenario | Durchgeführt über | Ergebnis |
|----------|-------------------|----------|
| 1. Happy Path Datei-Import (`POST api/cookbooks/{id}/import` → `200 { created }`) | Gestartete Anwendung + HTTP | **Nicht durchführbar** — siehe Begründung |
| 2. URL-Import setzt `SourceUri`, Re-Import dedupliziert | Gestartete Anwendung + HTTP | **Nicht durchführbar** — `RemoteUrlGuard` lehnt interne/Loopback-Ziele ab; ohne externen Rezept-URL-Endpunkt ist der Abrufpfad nicht auslösbar; externe Netzverfügbarkeit nicht garantiert |
| 3. Interaktionsbedarf → `400` mit `import-session/*`-Hinweis | Gestartete Anwendung + HTTP | **Nicht durchführbar** — benötigt ein real geladenes interaktives/Collection-Plugin in der laufenden App |
| 4. Kein passendes Plugin → `400` | Gestartete Anwendung + HTTP | **Nicht durchführbar** — siehe Begründung |
| 5. Request-Abbruch → `499` | Gestartete Anwendung + HTTP | **Nicht durchführbar** — siehe Begründung |

**Begründung:** `Rezepte.Tests.Browser` (Playwright) ist in dieser Umgebung nicht lauffähig (fehlende Referenzen/Playwright-Installation — vom Auftraggeber bestätigt). Ein manueller realer Lauf der Blazor-App wäre für den Datei-Import grundsätzlich denkbar (Registrierung → Kochbuch → Export → Re-Import über `POST import`), erfordert aber einen funktionierenden Plugin-Durchlauf der produktiven Plugins sowie den vollständigen Auth-Fluss außerhalb des Test-Frameworks und wurde in dieser Session nicht durchgeführt. Alle Szenarien sind durch Unit-/Integrationstests auf dem einzigen verbliebenen Ausführungspfad abgesichert (siehe unten); die Endpunkt-Verträge sind unverändert.

**Offene Anwender-Entscheidung (Eskalation gemäß lifecycle.md):** Für jedes `Nicht durchführbar`-Szenario steht aus: manuelle Abnahme durch den Anwender (`Bestanden (manuell)`) oder dokumentierter Verzicht (`Verzichtet (Anwender, …)`). Bis dahin gelten diese Szenarien als nicht erfolgreich nachgewiesen.

## Zusammenfassung

- Gesamt: 707
- Bestanden: 707
- Fehlgeschlagen: 0
- Übersprungen: 0

Läufe:
| Lauf | Befehl | Ergebnis | Nachweis |
|------|--------|----------|----------|
| Final (nach Implementierung) | `dotnet test Rezepte.Tests/Rezepte.Tests.csproj --no-build` | 707/707 bestanden, Exit 0 | [inventory/test-results/final-dotnet-test.txt](inventory/test-results/final-dotnet-test.txt) |
| Coverage-Lauf | `dotnet test ... --collect:"XPlat Code Coverage"` | 707/707, Cobertura-Report erzeugt | `Rezepte.Tests/TestResults/*/coverage.cobertura.xml` |
| Baseline (vor Implementierung) | identisch | 703/703 | [inventory/test-results/baseline-dotnet-test.txt](inventory/test-results/baseline-dotnet-test.txt) |

Differenz zur Baseline: −2 (`ImportServicePluginTests` entfernt) + 6 (neue Orchestrator-Tests) = +4 → 707.

## Testabdeckung

**Abdeckung:** 68,2 % Zeilen gesamt (Cobertura).

| Datei | Abdeckung |
|-------|-----------|
| `Rezepte.Web/Services/Import/ImportOrchestrator.cs` | ~66–100 % je Member (Hauptpfad vollständig abgedeckt) |
| `Rezepte.Web/Controllers/CookbooksController.cs` | 0 % (pre-existing: es existieren projektweit keine Controller-Tests) |

## Fehlende Tests

Quelle: `Coverage-Daten`

- `Rezepte.Web/Controllers/CookbooksController.cs` — 0 % Abdeckung (pre-existing Zustand; die geänderten Methoden sind reine Delegation an den testabgedeckten `ImportOrchestrator`; Einführung von Controller-Tests wäre eine projektweite Initiative, nicht Teil dieser Anforderung)

## Neu abgesicherte Verhaltensnachweise

- `StartImportAsync_ShouldPassUriToHandler` — `uri`-Durchreichung bis Handler + `SourceUri` beim Persister (Dedup-Kette)
- `ImportAndWaitAsync_ShouldReturnResult_WhenHandlerSucceeds` — synchroner Happy Path
- `ImportAndWaitAsync_ShouldReturnError_WhenNoPluginCanHandle` — identische Fehlermeldung wie Session-Pfad
- `ImportAndWaitAsync_ShouldAbortAndReturnInteractionError_WhenConfirmationRequired` — Interaktionsabbruch + `import-session/*`-Hinweis
- `ImportAndWaitAsync_ShouldAbortAndReturnInteractionError_WhenSelectionRequired` — Collection-Abbruch ohne Teilanlage
- `ImportAndWaitAsync_ShouldHonorCancellation` — Request-Cancellation

## Nicht lauffähige Suite

`Rezepte.Tests.Browser` (Playwright): baut in der Solution, ist in dieser Umgebung nicht ausführbar (fehlende Playwright-Installation/Referenzen; vom Auftraggeber als nicht lauffähig deklariert). Von den Änderungen nicht betroffen (kein Import-Browser-Test vorhanden).
