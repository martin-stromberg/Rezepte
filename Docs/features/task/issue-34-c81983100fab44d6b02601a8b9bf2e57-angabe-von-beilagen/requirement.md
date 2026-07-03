# Anforderung: Angabe von Beilagen

## Metadaten

- Aufgaben-ID: c8198310-0fab-44d6-b026-01a8b9bf2e57
- Branch: task/issue-34-c81983100fab44d6b02601a8b9bf2e57-angabe-von-beilagen
- Erstellt: 2026-07-02

## Ziel

Benutzer sollen zu Rezepten mögliche Beilagen verwalten und bei Kalender- sowie Einkaufslisten-Aktionen komfortabel übernehmen können.

## Fachliche Anforderungen

### Beilagenverwaltung

- Es muss eine Liste möglicher Beilagen verwaltet werden können.
- Eine Beilage muss als Verweis auf ein anderes Rezept hinterlegt werden können.
- Ein Rezept kann dadurch andere Rezepte als mögliche Beilagen referenzieren.

### Kalenderintegration

- Wenn ein Rezept in den Kalender eingefügt wird, sollen die zu diesem Rezept hinterlegten Beilagen mit verlinktem Rezept vorgeschlagen werden.
- Vorgeschlagene Beilagen sollen einzeln auswählbar sein.
- Ausgewählte Beilagen sollen direkt zusätzlich zum Kalender hinzugefügt werden können.

### Einkaufslistenintegration

- Wenn ein Rezept in die Einkaufsliste eingetragen wird, sollen im Dialog auch die Zutaten der verlinkten Beilagen angezeigt werden.
- Die Zutaten der Beilagen sollen initial nicht ausgewählt sein.
- Die Zutaten sollen nach Rezept gruppiert dargestellt werden.
- Die Gruppierung muss erkennbar machen, zu welchem Rezept die jeweiligen Zutaten gehören.

## Akzeptanzkriterien

- Benutzer können Beilagen als Rezeptverlinkungen zu einem Rezept verwalten.
- Beim Einfügen eines Rezepts in den Kalender werden verlinkte Beilagen vorgeschlagen.
- Benutzer können jede vorgeschlagene Beilage einzeln zum Kalender hinzufügen.
- Beim Eintragen eines Rezepts in die Einkaufsliste werden Zutaten der verlinkten Beilagen im Dialog angezeigt.
- Zutaten der Beilagen sind im Einkaufslisten-Dialog standardmäßig nicht ausgewählt.
- Zutaten im Einkaufslisten-Dialog sind nach dem jeweiligen Rezept gruppiert.

## Offene Punkte

- Keine.
