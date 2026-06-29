# Anforderung: Menueleiste aufraeumen

## Ausgangslage

Die Menueleiste der Anwendung soll uebersichtlicher werden. Sie ist derzeit nicht ausreichend fuer mobile Endgeraete optimiert und enthaelt in der Desktopansicht mehrere Elemente, die Platz beanspruchen oder redundant sind.

## Ziel

Die Navigation soll auf mobilen Endgeraeten besser nutzbar sein und in der Desktopansicht kompakter wirken. Redundante Navigationselemente sollen entfallen, Benutzerinformationen sollen platzsparend in ein Benutzermenue verschoben werden.

## Funktionale Anforderungen

### Mobile Navigation

- Die Menueleiste muss besser an mobile Endgeraete angepasst werden.
- Die Navigation muss auf kleinen Viewports bedienbar bleiben.
- Menueelemente duerfen auf mobilen Endgeraeten nicht unkontrolliert umbrechen, ueberlappen oder ausserhalb des sichtbaren Bereichs liegen.

### Startseite

- Der Menuepunkt `Start` muss entfernt werden.
- Die Startseite muss weiterhin ueber einen Klick auf den Anwendungsnamen erreichbar sein.
- Der Anwendungsname muss in der Menueleiste als Navigation zur Startseite erkennbar und bedienbar bleiben.

### Benutzerbereich

- Die bisher sichtbare Begruessung des Anwenders in der Menueleiste muss entfernt werden.
- Der bisher sichtbare Abmelden-Link in der Menueleiste muss entfernt werden.
- Stattdessen muss in der Menueleiste ein Benutzerlogo angezeigt werden.
- Beim Klick auf das Benutzerlogo muss ein Popup-Menue erscheinen.
- Das Popup-Menue muss die Angaben zum angemeldeten Benutzer anzeigen.
- Das Popup-Menue muss einen Link oder eine Aktion zum Abmelden anbieten.

### Einrichtung

- Der Menuepunkt fuer die Einrichtung muss in der Menueleiste nur noch das Zahnradsymbol anzeigen.
- Der Text des Einrichtungs-Menuepunkts darf in der Menueleiste nicht mehr angezeigt werden.
- Die Einrichtungsfunktion muss weiterhin ueber das Zahnradsymbol erreichbar bleiben.

## Nichtfunktionale Anforderungen

- Die Menueleiste muss in der Desktopansicht kompakter sein, insbesondere im rechten Bereich.
- Die Bedienbarkeit der Navigation darf durch die Umstrukturierung nicht verschlechtert werden.
- Die Umsetzung muss responsiv sein und Desktop- sowie mobile Ansichten beruecksichtigen.
- Interaktive Elemente wie Anwendungsname, Benutzerlogo, Popup-Menue, Abmelden-Aktion und Zahnradsymbol muessen per Maus oder Touch bedienbar sein.

## Akzeptanzkriterien

- In der Desktopansicht ist kein Menuepunkt `Start` mehr sichtbar.
- Ein Klick auf den Anwendungsnamen fuehrt zur Startseite.
- In der Menueleiste werden keine separate Anwenderbegruesung und kein separater Abmelden-Link mehr angezeigt.
- In der Menueleiste wird ein Benutzerlogo angezeigt.
- Ein Klick auf das Benutzerlogo oeffnet ein Popup-Menue mit Benutzerangaben und Abmelden-Link.
- Der Abmelden-Link im Popup-Menue meldet den Benutzer wie bisher ab.
- Der Einrichtungs-Menuepunkt wird in der Menueleiste ausschliesslich als Zahnradsymbol dargestellt.
- Ein Klick auf das Zahnradsymbol oeffnet weiterhin die Einrichtung.
- Die Menueleiste bleibt auf mobilen Endgeraeten nutzbar und verursacht keine sichtbaren Layoutfehler.
- Der rechte Bereich der Menueleiste ist in der Desktopansicht sichtbar kompakter als zuvor.

## Abgrenzung

- Die fachliche Funktion der Startseite wird nicht geaendert.
- Die fachliche Funktion des Abmeldens wird nicht geaendert.
- Die fachliche Funktion der Einrichtung wird nicht geaendert.
- Es werden keine neuen Benutzerprofildaten gefordert; das Popup-Menue zeigt die bereits verfuegbaren Benutzerangaben.

## Offene Punkte

- Welche konkreten Benutzerangaben im Popup-Menue angezeigt werden sollen, richtet sich nach den aktuell verfuegbaren Benutzerdaten der Anwendung.
- Ob fuer mobile Endgeraete ein Hamburger-Menue, ein einklappbarer Bereich oder ein anderes vorhandenes Navigationsmuster genutzt wird, ist anhand der bestehenden Anwendung zu entscheiden.
