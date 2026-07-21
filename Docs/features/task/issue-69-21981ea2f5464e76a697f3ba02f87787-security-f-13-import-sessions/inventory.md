# Bestandsaufnahme: Security F-13 - Import-Sessions an Initiator binden

## Eingaben

- Anforderung: [requirement.md](requirement.md)
- Feature-Verzeichnis: `docs/features/task/issue-69-21981ea2f5464e76a697f3ba02f87787-security-f-13-import-sessions/`

## Kurzfazit

Die Import-Session-Verwaltung ist aktuell rein session-id-basiert. `ImportOrchestrator.StartImportAsync` erhaelt zwar die aktuelle `userId`, speichert sie aber nicht in `ImportSession`. Die nachgelagerten API-Endpunkte fuer Status, Confirm, Selection und Selection-Cancel greifen ueber `sessionId` auf den Singleton-Orchestrator zu, ohne die Identitaet des aktuellen Benutzers gegen einen Session-Besitzer zu pruefen.

Damit bestaetigt die Bestandsaufnahme die in der Anforderung beschriebene Sicherheitsluecke. Die passende technische Loesung liegt voraussichtlich in einer serverseitigen Besitzbindung im `ImportOrchestrator.ImportSession` und in userId-aware Zugriffsmethoden fuer alle bestehenden Session-Zugriffe.

## Detaildokumente

- [API-Endpunkte](inventory/api-endpoints.md)
- [Import-Orchestrator und Session-State](inventory/import-orchestrator.md)
- [Sicherheitsbefund und Risiken](inventory/security-findings.md)
- [Tests und Testluecken](inventory/tests.md)
- [Umsetzungsrelevante Dateien](inventory/affected-files.md)

## Betroffene Kernbereiche

- `Rezepte.Web/Controllers/CookbooksController.cs`
  - Startet Import-Sessions mit authentifizierter `userId`.
  - Liest und mutiert vorhandene Sessions derzeit ueber reine `sessionId`.
  - Enthält Varianten mit und ohne `cookbookId`.
- `Rezepte.Web/Services/Import/ImportOrchestrator.cs`
  - Haelt Sessions in einem `ConcurrentDictionary<string, ImportSession>`.
  - Erstellt Sessions ohne Besitzerfeld.
  - Bietet `GetSession`, `Confirm`, `SubmitSelection` und `CancelSelection` ohne Benutzerkontext an.
- `Rezepte.Tests/Services/Import/ImportOrchestratorTests.cs`
  - Deckt Importablauf, Confirmation und Collection-Selection ab.
  - Enthält noch keine Negativtests mit zwei Benutzern.

## Relevante Beobachtungen

- Der Controller liest die UserId ueber `ClaimTypes.NameIdentifier` und prueft bei Start-Endpunkten bereits `Unauthorized`, wenn kein Benutzer vorhanden ist.
- Bei `StartImportSession` mit `cookbookId` wird vor dem Start der Kochbuchbesitz geprueft; bei spaeterem Sessionzugriff wird weder Kochbuch noch Session-Besitz geprueft.
- Die Statusantwort enthaelt sensible Sessiondetails wie Confirmation-Prompt, Importresultat, Collection-Preview, einzelne Collection-Items, Fehler und Rezept-IDs.
- Selection-Endpunkte validieren zwar Zielkochbuecher gegen den aktuellen Benutzer, pruefen aber nicht, ob die Session selbst dem Benutzer gehoert.
- Das Fehlerverhalten ist fuer fremde Session-IDs noch nicht modelliert, weil fremde Sessions aktuell nicht unterscheidbar sind.

## Offene Punkte fuer die Planung

- Welcher HTTP-Status soll fuer fremde Sessions verwendet werden? Aus Sicht der Anforderung ist ein einheitliches Verhalten mit unbekannten Sessions sinnvoll, also voraussichtlich `404 NotFound`.
- Soll der Orchestrator die bestehende API ersetzen oder ergaenzen? Fuer geringe Bruchwirkung bietet sich eine neue userId-aware API an, ggf. mit kontrollierter interner Weiterverwendung bestehender Methoden in Tests.
- Soll das Fehlerobjekt fuer fremde und unbekannte Sessions komplett leer bleiben oder eine generische Meldung enthalten? Wichtig ist, dass keine Sessiondetails preisgegeben werden.
