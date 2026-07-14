# Chefkoch-Parser und Sammlungsfaehigkeit

## Aktueller Chefkoch-Parser

`ChefkochImportHandler` in `Rezepte.Import.Plugins.Chefkoch/ChefkochImportPlugin.cs:16` erbt von `UrlRecipeImportHandlerBase`.

Der Parser extrahiert fuer Einzelrezepte:

- Titel ueber `FindTitle(...)` ab `ChefkochImportPlugin.cs:18`
- Bilder ueber `FindPicturesAsync(...)` ab `ChefkochImportPlugin.cs:25`
- Einzelrezept ueber `ReadSingleRecipe(...)` ab `ChefkochImportPlugin.cs:49`
- Zutaten ueber `FindIngredients(...)` ab `ChefkochImportPlugin.cs:80`
- Zubereitung ueber `FindInstructions(...)` ab `ChefkochImportPlugin.cs:105`
- Arbeitszeit ueber `FindMetaValue(...)` ab `ChefkochImportPlugin.cs:132`

Der Einzelrezeptparser erwartet im `main`-Bereich unter anderem:

- `h1`
- `section` mit `recipe-ingredients`
- `section` mit `Zubereitung`
- `recipe-meta-property-group__labels`

## Aktuelle Sammlungsunterstuetzung

Im Chefkoch-Handler gibt es aktuell keine Ueberschreibung von `ExtractRecipeUriCollection(...)`. Damit kann der Handler nur Einzelrezepte erkennen.

Die Basis koennte Collections technisch nachladen, aber genau dieses Verhalten ist fuer die Anforderung falsch, weil es vor der Auswahl alle Einzelrezeptseiten abrufen wuerde.

## Umsetzungshinweise

Die Sammlungserkennung sollte vor oder unabhaengig von `ReadRecipeCollection(...)` erfolgen. Fuer Chefkoch-Sammlungen sind wahrscheinlich folgende Daten aus der Sammlungsseite relevant:

- Rezepttitel
- Ziel-URL zum Rezept
- optional Bild/Thumbnail, falls auf der Sammlungsseite vorhanden
- optional Kurztext, Bewertung, Zeit oder Metaangaben, sofern ohne Einzelrezeptabruf verfuegbar

Die konkreten Felder sind fachlich offen und muessen im Plan entweder konservativ festgelegt oder als offene Frage behandelt werden.

## Testbarkeit

`Rezepte.Tests/Services/Import/ProductiveImportPluginParserTests.cs:38` testet bereits den Chefkoch-Einzelrezeptparser. Fuer die Sammlungserweiterung sollten dort HTML-Fixtures fuer Sammlungsseiten ergaenzt werden:

- Sammlungsseite wird als Sammlung erkannt.
- Vorschau enthaelt die erwarteten Items.
- Einzelrezeptseiten werden beim Anzeigen der Vorschau nicht abgerufen.
- Einzelrezeptseite funktioniert weiterhin unveraendert.

Wenn Netzwerkzugriffe vermieden werden sollen, braucht der Chefkoch-Handler eine injizierbare Download-Abstraktion oder eine testbare Methode fuer das Nachladen einzelner Rezeptseiten.

