# Strukturierte Anforderung: Massenimport

## Metadaten

- Aufgaben-ID: `3aeba260-fd68-4f0e-b16f-962687c56960`
- Branch: `task/issue-50-3aeba260fd684f0eb16f962687c56960-massenimport`
- Erstellt: `2026-07-14`
- Thema: Verbesserter Import neuer Rezepte mit Zwischenauswahl fuer Rezeptsammlungen

## Ziel

Der Rezeptimport soll Seiten erkennen koennen, die keine einzelnen Rezepte, sondern eine Sammlung mehrerer Rezepte enthalten. In diesem Fall soll der Anwender zuerst eine Liste der in der Sammlung gefundenen Rezepte sehen, einzelne Rezepte fuer den Import auswaehlen und je ausgewaehltem Rezept eine Kategorie zuordnen koennen. Erst nach Bestaetigung dieser Zwischenansicht werden die gewaehlten Rezepte tatsaechlich abgerufen und importiert.

## Ausgangssituation

- Der bisherige Import erwartet pro Plugin eine Eingabeseite, die genau ein Rezept liefert.
- Alle bestehenden Plugins ausser Chefkoch sollen dieses Verhalten unveraendert beibehalten.
- Das Chefkoch-Plugin soll zusaetzlich Rezeptsammlungen verarbeiten koennen, zum Beispiel:
  - `https://www.chefkoch.de/rezeptsammlung/3212418/Erdbeerzeit.html`

## Akteure

- Anwender: Startet den Import, waehlt Rezepte aus einer Sammlung aus, weist Kategorien zu und kann den Fortschrittsdialog schliessen.
- Importsystem: Erkennt Einzelrezept oder Sammlung, liest Sammlungsinformationen, ruft ausgewaehlte Rezepte ab und meldet Fortschritt oder Fehler.
- Import-Plugin Chefkoch: Liefert fuer Chefkoch neben Einzelrezepten auch Sammlungsinformationen und kann einzelne Rezepte aus einer Sammlung nachladen.
- Andere Import-Plugins: Bleiben beim bisherigen Einzelrezept-Verhalten.

## Funktionale Anforderungen

### Sammlungserkennung

- Wenn die angegebene Importseite ein einzelnes Rezept enthaelt, bleibt der Importablauf wie bisher.
- Wenn die angegebene Importseite eine Rezeptsammlung enthaelt, darf das System die einzelnen Rezepte nicht sofort vollstaendig laden.
- Bei einer Sammlung sollen nur die auf der Sammlungsseite vorhandenen Informationen ausgelesen und angezeigt werden.
- Die Sammlungserkennung wird zuerst fuer das Chefkoch-Plugin umgesetzt.

### Zwischenauswahl fuer Sammlungen

- Dem Anwender wird bei erkannter Sammlung ein Zwischendialog angezeigt.
- Der Zwischendialog zeigt die Liste der in der Sammlung gefundenen Rezepte.
- Die angezeigten Rezeptinformationen stammen aus der Sammlung und nicht aus einem Abruf der einzelnen Rezeptseiten.
- Der Anwender kann aus der Liste auswaehlen, welche Rezepte importiert werden sollen.
- Fuer jedes ausgewaehlte Rezept kann der Anwender eine Kategoriezuordnung festlegen.
- Nicht ausgewaehlte Rezepte werden nicht abgerufen und nicht importiert.
- Erst beim Absenden des Zwischendialogs startet der Abruf der ausgewaehlten Rezepte.

### Abruf und Fortschrittsanzeige

- Nach dem Absenden wird der Dialog schreibgeschuetzt.
- Waehrend des Abrufs zeigt der Dialog den Fortschritt pro ausgewaehltem Rezept.
- Jedes erfolgreich abgerufene Rezept wird in der Auswahlliste mit einem Erfolgshaken markiert.
- Wenn beim Abruf eines Rezepts ein Fehler auftritt, wird dieses Rezept mit einem Warnsymbol markiert.
- Ueber das Warnsymbol kann der Anwender die konkrete Fehlermeldung einsehen.
- Fehler bei einzelnen Rezepten duerfen die Fortschrittsanzeige fuer die uebrigen Rezepte nicht verhindern.

### Dialog schliessen

- Der Dialog bietet einen Schliessen-Button.
- Der Schliessen-Button blendet den Dialog aus.
- Ein Schliessen des Dialogs bricht den laufenden Import nicht ab.
- Nach dem Schliessen laeuft der Import ohne weitere Fortschrittsdarstellung weiter.

### Plugin-Verhalten

- Das Chefkoch-Plugin muss Sammlungsseiten als Importquelle unterstuetzen.
- Das Chefkoch-Plugin muss weiterhin einzelne Chefkoch-Rezepte wie bisher importieren koennen.
- Alle anderen Plugins sollen weiterhin genau ein Rezept erwarten und liefern.
- Fuer andere Plugins ist keine Sammlungsunterstuetzung erforderlich.

## Nicht-funktionale Anforderungen

- Der Abruf einzelner Rezepte aus einer Sammlung soll erst nach expliziter Auswahl und Bestaetigung durch den Anwender erfolgen.
- Die Benutzeroberflaeche muss waehrend des laufenden Abrufs unbeabsichtigte Aenderungen an Auswahl und Kategoriezuordnung verhindern.
- Fehler muessen pro Rezept nachvollziehbar sein.
- Die Erweiterung soll rueckwaertskompatibel zu bestehenden Einzelrezept-Importen bleiben.

## Akzeptanzkriterien

- Wird eine Chefkoch-Einzelrezeptseite importiert, funktioniert der Import wie bisher.
- Wird eine Chefkoch-Sammlungsseite importiert, erscheint ein Zwischendialog mit den in der Sammlung gefundenen Rezepten.
- Beim Anzeigen des Zwischendialogs wurden die einzelnen Rezeptseiten der Sammlung noch nicht geladen.
- Der Anwender kann mehrere, einzelne oder keine Rezepte aus der Sammlung auswaehlen.
- Fuer jedes ausgewaehlte Rezept kann eine Kategorie zugeordnet werden.
- Nach dem Absenden werden nur die ausgewaehlten Rezepte abgerufen.
- Der Dialog ist nach dem Absenden schreibgeschuetzt.
- Erfolgreich abgerufene Rezepte werden mit einem Erfolgshaken markiert.
- Fehlgeschlagene Abrufe werden mit einem Warnsymbol markiert.
- Die Fehlermeldung eines fehlgeschlagenen Abrufs ist ueber das Warnsymbol abrufbar.
- Der Dialog kann waehrend des laufenden Imports geschlossen werden.
- Nach dem Schliessen des Dialogs laeuft der Import im Hintergrund weiter.
- Nicht-Chefkoch-Plugins verhalten sich weiterhin wie bisher und liefern nur ein einzelnes Rezept.

## Abgrenzungen

- Eine Sammlungsunterstuetzung fuer andere Plugins als Chefkoch ist nicht Bestandteil dieser Anforderung.
- Die Anforderung beschreibt keinen Abbruchmechanismus fuer laufende Importe.
- Die Anforderung beschreibt keine nachtraegliche Wiederanzeige eines geschlossenen Fortschrittsdialogs.
- Die Anforderung beschreibt keine globale Kategoriezuordnung fuer alle ausgewaehlten Rezepte, sondern eine Zuordnung pro Rezept.

## Offene Punkte

- Welche konkreten Rezeptinformationen aus einer Chefkoch-Sammlung angezeigt werden sollen, ist nicht festgelegt.
- Wie der Anwender bei Auswahl von keinem Rezept gefuehrt werden soll, ist nicht festgelegt.
- Ob Fehler beim Import einzelner Rezepte den Gesamtimportstatus beeinflussen sollen, ist nicht festgelegt.
- Ob nach geschlossenem Dialog eine Benachrichtigung ueber Abschluss oder Fehler erfolgen soll, ist nicht festgelegt.
