# Anforderung: Mobile Ansicht fuer Einkaufsliste

## Metadaten

| Feld | Wert |
|------|------|
| Aufgaben-ID | `0c78fade-a4a8-4c71-8354-bcf61ad6cf36` |
| Branch | `task/issue-71-0c78fadea4a84c718354bcf61ad6cf36-mobile-ansicht-fuer-einkaufsli` |
| Erstellt | `2026-07-18` |

## Ausgangssituation

Die mobile Bearbeitungsansicht der Einkaufsliste ist derzeit schwer nutzbar. Die Eingabefelder einzelner Einkaufslisten-Eintraege werden untereinander dargestellt, wodurch nicht klar erkennbar ist, welche Felder zu welchem Eintrag gehoeren. Zusaetzlich ist der Seiteninhalt breiter als das Display, sodass horizontales Scrollen notwendig wird.

## Ziel

Die mobile Ansicht der Einkaufsliste soll so angepasst werden, dass Einkaufslisten-Eintraege im Bearbeitungsmodus kompakt, eindeutig gruppiert und ohne horizontales Scrollen bearbeitet werden koennen.

## Funktionale Anforderungen

1. Im Bearbeitungsmodus der Einkaufsliste sollen Menge und Einheit nicht mehr als getrennte Felder dargestellt werden.
2. Menge und Einheit sollen zu einem gemeinsamen Feld zusammengefasst werden.
3. In der mobilen Bearbeitungsansicht sollen das Mengenfeld, das Zutatenfeld und der Loeschen-Button fuer einen Eintrag nebeneinander angezeigt werden.
4. Die nebeneinander angeordneten Elemente eines Eintrags duerfen zusammen nicht breiter als der Bildschirm sein.
5. Die Checkbox zum Abhaken eines Eintrags darf im Bearbeitungsmodus nicht angezeigt werden.
6. Die Buttons zum Wechsel zwischen Ansichtsmodus und Bearbeitungsmodus sollen statt Text jeweils ein passendes Symbol anzeigen.
7. Die Seite darf in der mobilen Ansicht kein horizontales Scrollen verursachen.

## Akzeptanzkriterien

1. Auf einem mobilen Viewport ist pro Einkaufslisten-Eintrag im Bearbeitungsmodus klar erkennbar, welche Eingabefelder und welcher Loeschen-Button zusammengehoeren.
2. Das bisherige Feld fuer die Einheit ist im Bearbeitungsmodus nicht mehr separat vorhanden; Menge und Einheit werden gemeinsam in einem Feld bearbeitet.
3. Mengenfeld, Zutatenfeld und Loeschen-Button eines Eintrags stehen auf mobilen Viewports in einer Zeile nebeneinander.
4. Die Zeile eines Eintrags passt vollstaendig in die Bildschirmbreite, ohne dass horizontales Scrollen entsteht.
5. Im Bearbeitungsmodus wird keine Abhaken-Checkbox angezeigt.
6. Der Wechsel in den Bearbeitungsmodus erfolgt ueber einen Symbol-Button ohne sichtbaren Text.
7. Der Wechsel zurueck in den Ansichtsmodus erfolgt ueber einen Symbol-Button ohne sichtbaren Text.
8. Die Einkaufsliste ist auf mobilen Viewports ohne horizontales Scrollen bedienbar.

## Nicht-Ziele

1. Es soll keine neue Trennung von Menge und Einheit eingefuehrt werden.
2. Es ist keine fachliche Aenderung am Abhaken von Eintraegen ausserhalb des Bearbeitungsmodus gefordert.
3. Es ist keine grundlegende Neugestaltung der Einkaufsliste ausserhalb der beschriebenen mobilen Bearbeitungsansicht gefordert.

## Offene Punkte

Keine.
