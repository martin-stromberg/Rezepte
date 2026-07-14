# Tests, Risiken und Rueckwaertskompatibilitaet

## Vorhandene Tests

Relevante Testdateien:

- `Rezepte.Tests/Services/Import/ImportOrchestratorTests.cs`
- `Rezepte.Tests/Services/Import/ImportServicePluginTests.cs`
- `Rezepte.Tests/Services/Import/ProductiveImportPluginParserTests.cs`
- `Rezepte.Tests/Services/Import/PluginManagerTests.cs`

Abgedeckte Verhaltensweisen:

- Plugin-Reihenfolge im Orchestrator.
- Interaktive Bestaetigung im Orchestrator (`StartImportAsync_ShouldSupportInteractivePluginConfirmation`).
- Abbruch nach fehlerhaftem passendem Plugin.
- Plugin-Reihenfolge im synchronen `ImportService`.
- Chefkoch-Einzelrezeptparser (`ChefkochPlugin_ShouldParseChefkochHtml`).

## Empfohlene neue Tests

- Chefkoch-Einzelrezept bleibt unveraendert importierbar.
- Chefkoch-Sammlungsseite wird als Sammlung erkannt.
- Vorschau verwendet nur Daten aus der Sammlungsseite.
- Keine Einzelrezeptseite wird vor Auswahl abgerufen.
- Auswahl von mehreren Rezepten laedt nur diese URLs.
- Nicht ausgewaehlte Rezepte werden nicht abgerufen.
- Fehler bei einem ausgewaehlten Rezept werden nur fuer dieses Item gespeichert.
- Erfolgreiche Items werden trotz anderer Fehler persistiert.
- Zielkochbuch/Kategorie wird pro ausgewähltem Rezept angewendet.
- Nicht-Chefkoch-Plugins behalten ihr Einzelrezeptverhalten.
- API-Status liefert `selectionRequired`, `importing`, `completed`/Teilfehler korrekt.
- UI sperrt Auswahl und Kategoriezuordnung nach Absenden.

## Hauptrisiken

- Chefkoch-HTML ist extern und kann sich aendern; Parser sollte robust und durch HTML-Fixtures abgesichert sein.
- Die bestehende `UrlRecipeImportHandlerBase.ReadRecipeCollection(...)` ist verlockend, verletzt aber die No-Prefetch-Anforderung.
- `IImportInteraction` ist zu schwach fuer strukturierte Auswahl; eine halbstrukturierte Nutzung des Prompt-Strings waere schwer testbar.
- Der Orchestrator speichert Sessions nur im Arbeitsspeicher. Das passt zum heutigen Stand, bedeutet aber Verlust von Fortschrittsanzeige bei Serverneustart.
- Ein geschlossener Dialog soll den Import nicht abbrechen. UI-Code darf daher keine Cancellation an die Session koppeln.
- Der Begriff "Kategorie" ist im Code nicht eindeutig; wahrscheinlich ist `Cookbook` gemeint, muss aber fachlich bestaetigt werden.

## Rueckwaertskompatibilitaet

Fuer bestehende Plugins sollte gelten:

- `IImportHandler` bleibt funktionsfaehig.
- `IInteractiveImportHandler` mit einfacher Bestaetigung bleibt funktionsfaehig.
- `ImportResult` fuer Einzelrezeptimporte bleibt kompatibel.
- Nicht-Chefkoch-Plugins muessen keine Sammlungsinterfaces implementieren.
- Alte synchrone Import-Endpunkte koennen unveraendert bleiben, sollten aber fuer Sammlungen nicht der primaere UI-Pfad sein.

