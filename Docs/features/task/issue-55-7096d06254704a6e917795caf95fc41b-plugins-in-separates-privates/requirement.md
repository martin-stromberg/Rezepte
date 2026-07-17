# Uebersetzte Anforderung

## Ausgangslage

Die bestehenden Plugins zum Abruf von Rezepten aus bekannten Quellen befinden sich aktuell im bestehenden Repository. Sie sollen kuenftig in ein separates Repository ausgelagert werden. Gleichzeitig soll weiterhin nachweisbar sein, dass die ausgelagerten Plugins technisch nutzbar sind und Rezeptdaten abrufen koennen.

## Ziel

Die Rezeptabruf-Plugins werden aus dem bestehenden Repository herausgeloest und in ein neues Repository ueberfuehrt. Zusaetzlich wird ein rudimentaeres Testprogramm bereitgestellt, mit dem die Plugins manuell gegen eine eingegebene URL oder Datei ausgefuehrt werden koennen.

## Funktionale Anforderungen

1. Die bestehenden Plugins fuer den Abruf von Rezepten aus bekannten Quellen muessen in ein neues Repository ausgelagert werden.
2. Die ausgelagerten Plugins muessen weiterhin nutzbar sein, um Rezeptdaten aus unterstuetzten Quellen abzurufen.
3. Es muss ein rudimentaeres Programm fuer manuelle Plugin-Tests bereitgestellt werden.
4. Das Testprogramm muss die Eingabe einer URL oder einer Datei ermoeglichen.
5. Das Testprogramm muss die Auswahl eines auszufuehrenden Plugins ermoeglichen.
6. Das Testprogramm muss das Ergebnis der Plugin-Ausfuehrung anzeigen.
7. Als Ergebnis muss sowohl eine Meldung moeglich sein, dass keine Verarbeitung moeglich ist, als auch die Anzeige erfolgreich abgerufener Rezeptdaten.

## Nicht-funktionale Anforderungen

1. Das Testprogramm muss nicht optisch gestaltet oder komfortabel bedienbar sein.
2. Der Fokus liegt auf der technischen Nachweisbarkeit, dass der Rezeptabruf funktioniert und das Plugin verwendbar ist.
3. Die Loesung soll fuer manuelle Tests geeignet sein, nicht zwingend fuer produktive Endanwender.

## Akzeptanzkriterien

1. Die Rezeptabruf-Plugins liegen in einem separaten Repository oder sind so vorbereitet, dass sie als eigenes Repository betrieben werden koennen.
2. Ein manueller Testlauf kann durch Eingabe einer URL gestartet werden.
3. Ein manueller Testlauf kann durch Eingabe einer Datei gestartet werden.
4. Vor dem Testlauf kann ein Plugin ausgewaehlt werden.
5. Das Testprogramm zeigt bei nicht unterstuetzten Eingaben nachvollziehbar an, dass keine Verarbeitung moeglich ist.
6. Das Testprogramm zeigt bei erfolgreichen Abrufen die ermittelten Rezeptdaten an.
7. Mindestens ein bekannter Rezeptabruf kann mit dem Testprogramm demonstriert werden.

## Abgrenzungen

1. Eine ansprechende Benutzeroberflaeche ist nicht erforderlich.
2. Erweiterte Bedienfunktionen, Validierungen oder Komfortfunktionen sind nicht erforderlich, sofern der manuelle Funktionsnachweis moeglich ist.
3. Die Anforderung beschreibt keine Veraenderung der fachlichen Rezeptdatenstruktur, ausser sie ist fuer die Auslagerung oder Testbarkeit zwingend notwendig.

## Offene Punkte

1. Zielort und Name des neuen Repositories sind noch nicht benannt.
2. Es ist noch nicht festgelegt, ob das rudimentaere Testprogramm im neuen Plugin-Repository oder im bestehenden Repository liegen soll.
3. Es ist noch nicht festgelegt, welche konkreten bekannten Quellen als Mindestumfang fuer den manuellen Nachweis verwendet werden sollen.
