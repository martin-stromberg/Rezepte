# Persistenz, Kategorien und Rezeptzuordnung

## Aktueller Persistenzpfad

`ImportedRecipePersister.PersistAsync(...)` in `Rezepte.Web/Services/Import/ImportedRecipePersister.cs` speichert alle `ImportedRecipes` aus einem `ImportResult` in dasselbe `targetCookbookId`.

Der Ablauf:

- Titel normalisieren.
- Bei vorhandener `SourceUri` existierendes Rezept per `FindByUri(...)` suchen.
- Existierendes Rezept aktualisieren oder neues Rezept erstellen.
- Bilder hinzufuegen.
- `CreatedRecipeIds` im Ergebnis aktualisieren.

## Bedeutung fuer die Anforderung

Die Anforderung verlangt eine Kategoriezuordnung pro ausgewaehltem Rezept. Der aktuelle Persister kann das nicht, weil er nur einen Zielkontext fuer das gesamte Ergebnis kennt.

Moegliche Interpretation im vorhandenen System:

- Kategorie = Kochbuch.
- Dann muss pro ausgewähltem Sammlungseintrag ein Zielkochbuch gespeichert werden.

Wenn Kategorie nicht Kochbuch bedeutet, existiert in den gelesenen Modellen kein offensichtliches separates Kategorie-Entity. Die Rezeptdomäne arbeitet sichtbar mit `Cookbook`, `RecipeCookbook` und Rezeptdaten, nicht mit einer eigenstaendigen Kategorieverwaltung.

## Naheliegende technische Anpassung

Statt `ImportedRecipePersister.PersistAsync(result, targetCookbookId, userId, ...)` direkt fuer ein gesamtes Massenergebnis zu verwenden, braucht der Sammlungsfluss eine Zuordnung:

- Importiertes Rezept oder Sammlungseintrag
- Zielkochbuch/Kategorie
- Persistenzergebnis
- Fehler je Eintrag

Optionen:

- Neuen Persister-Pfad fuer `IReadOnlyList<SelectedImportedRecipe>` einfuehren.
- `ImportedRecipe` um eine optionale Zielzuordnung erweitern.
- Eine separate Map `SourceUri -> targetCookbookId` an den Persister uebergeben.

Die separate Map oder ein neues Auswahlmodell ist sauberer, weil `ImportedRecipe` ein neutrales Pluginmodell bleiben kann.

## Fehlerbehandlung

Aktuell bricht `PersistAsync(...)` bei einem Erstellungs-/Updatefehler mit globalem Fehler ab. Fuer Massenimport ist erforderlich:

- Fehler pro Rezept speichern.
- Uebrige Rezepte weiter importieren.
- Gesamtresultat als abgeschlossen mit Teilfehlern oder fehlgeschlagen definieren.

Das Akzeptanzkriterium verlangt nur, dass Fehler einzelner Rezepte die Fortschrittsanzeige der anderen nicht verhindern. Der Gesamtstatus ist fachlich offen.

