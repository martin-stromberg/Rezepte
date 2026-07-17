# Tests und manueller Nachweis

## Automatisierte Tests

Relevante Tests im Hauptrepo:

- `PluginManagerTests`: prueft externe Plugin-Discovery direkt unter `plugins/`, in Unterordnern und aus `AppContext.BaseDirectory`.
- `PluginManagerTests.InitializeAsync_ShouldDiscoverProductiveExternalImportPlugins`: prueft, dass alle produktiven Plugin-DLLs als externe Plugins geladen werden koennen.
- `ImportServicePluginTests`: prueft Handler-Reihenfolge und Fehler bei nicht verarbeitbaren Eingaben.
- `ProductiveImportPluginParserTests`: prueft Parserlogik fuer Backup, Chefkoch und SecondSource bis SixthSource anhand lokaler HTML-/JSON-Beispiele.
- `Rezepte.Tests.PluginFixture`: Test-Plugin fuer Discovery-Szenarien.

Diese Tests sind eine gute Grundlage, muessen bei Auslagerung aber teilweise in das neue Plugin-Repository verschoben oder durch Paket-/Artifact-Tests ersetzt werden.

## Bestehender manueller Testpfad

Es gibt `ImportTestController` mit Admin-Endpunkten:

- `GET /api/import/test-urls`
- `POST /api/import/test-delete`

`TestRecipeImportService` liest `test.recipe-import.json` aus dem ContentRoot und liefert gespeicherte URLs. Das ist kein vollstaendiges manuelles Testprogramm im Sinne der Anforderung, weil:

- keine freie URL-Eingabe im Programm vorhanden ist,
- keine Datei-Eingabe vorhanden ist,
- keine explizite Plugin-Auswahl vorhanden ist,
- keine direkte Anzeige des Plugin-Ergebnisses implementiert ist.

## Erwartetes Testprogramm

Ein passender rudimentaerer Runner kann als Console-App im neuen Plugin-Repository umgesetzt werden. Minimal benoetigt:

- Auflisten der verfuegbaren `IImportPlugin`-Implementierungen.
- Auswahl eines Plugins per Nummer oder ID.
- Eingabe einer URL oder eines Dateipfads.
- Bei URL: HTML herunterladen und als Stream mit URL/Dateiname an den Handler geben.
- Bei Datei: Datei als Stream an den Handler geben.
- Aufruf von `CanHandleAsync`; bei `false` klare Meldung, dass keine Verarbeitung moeglich ist.
- Aufruf von `HandleAsync`; Ausgabe von `Success`, `Error` und der importierten Rezeptdaten.

## Demonstrationskandidat

`ChefkochImportHandler` eignet sich als erster manueller Nachweis:

- bekannte fachliche Quelle,
- eigene Plugin-ID `chefkoch`,
- Tests fuer Einzelrezept und Sammlung,
- konkrete Ausgabe mit Titel, Zutaten, Schritten, Quelle und Arbeitszeit.

Fuer einen stabilen manuellen Nachweis sollte zusaetzlich eine lokale Beispiel-HTML-Datei im neuen Repository liegen, damit der Nachweis nicht ausschliesslich von Live-Webseiten abhaengt.
