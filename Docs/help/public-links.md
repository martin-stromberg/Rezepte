# Öffentliche Links

Mit der Teilen-Funktion kann der Besitzer eines Rezepts einen zeitlich begrenzten öffentlichen Link erzeugen. Personen ohne Benutzerkonto können das Rezept über diesen Link aufrufen.

## Link erzeugen

Auf der Rezeptseite öffnet die Schaltfläche `Teilen` (Freigabesymbol) den Teilen-Dialog. Dort wird mit `Link erstellen` ein neuer öffentlicher Link erzeugt und samt Gültigkeitsdatum angezeigt. Der Link kann über `Kopieren` in die Zwischenablage übernommen und beliebig weitergegeben werden.

Jeder Link ist unabhängig und besitzt einen eigenen, nicht erratbaren Token. Je Teilen-Vorgang wird ein neuer Link erzeugt; eine Verwaltung, Erneuerung oder Sperrung bestehender Links gibt es nicht.

## Was der Empfänger sieht

Der öffentliche Aufruf unter `/share/<token>` zeigt ausschließlich das Rezept in einer schreibgeschützten Ansicht ohne Anwendungsnavigation. Es gibt keine Möglichkeit zum Bearbeiten, keinen Einkaufswagen, keine Favoritenfunktion und keine Terminplanung. Als einzige Aktion ist die originale Quellseite des Rezepts verlinkt, sofern am Rezept eine Quell-URL hinterlegt ist.

Rezeptbilder werden über den token-gebundenen Endpunkt `api/share/<token>/image/<bild-id>` anonym ausgeliefert.

Ungültige, abgelaufene oder gelöschte Links führen zu einer generischen Fehlermeldung.

## Beilagen freigeben

Besitzt das Rezept verlinkte Beilagenrezepte, kann im Teilen-Dialog der Haken `Beilagen freigeben` gesetzt werden. Der Link listet die direkten Beilagen dann auf der öffentlichen Seite auf; jede Beilage ist über `/share/<token>/sidedish/<rezept-id>` aufrufbar.

Ohne diesen Haken werden Beilagen weder angezeigt noch serverseitig ausgeliefert. Beilagen der Beilagen werden nicht transitiv freigegeben.

## Gültigkeitsdauer

Jeder Link ist standardmäßig 30 Tage gültig. Die Anzahl der Tage ist eine Programmeinstellung, die der Administrator im Einstellungsbereich unter `Öffentliche Links` (`api/settings/global/sharelinkvalidity`) ändert. Der Wert muss größer als 0 sein; fehlende oder ungültige gespeicherte Werte fallen auf 30 zurück.

Der Ablaufzeitpunkt wird beim Erzeugen des Links festgeschrieben. Spätere Änderungen der Einstellung wirken nur auf neu erzeugte Links.
