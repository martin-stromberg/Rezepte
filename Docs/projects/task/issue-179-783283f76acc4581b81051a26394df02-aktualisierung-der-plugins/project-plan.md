# Projektplan: Aktualisierung der Plugins (Issue #179)

## Übersicht

Die Update-Prüfung der GitHub-Pluginquellen wird für den Anwender transparent gemacht und um eine manuelle, quellenbezogene Prüfung in der Admin-Oberfläche erweitert. Gefundene Updates werden künftig in versionsbezogenen Pluginverzeichnissen installiert und über den koordinierten Austausch ohne Anwendungsneustart wirksam; nicht löschbare Altverzeichnisse werden zur späteren Löschung markiert und aus der Erkennung ausgeschlossen. Ergänzend werden die Aktionsbuttons der Pluginquellen als Symbolbuttons dargestellt, und als vermisst geführte Plugins können aus der Pluginverwaltung entfernt werden. Betroffen sind die Bereiche Plugin-Installation und -Austausch, Update-Prüfung, Pluginverwaltung (Admin-Oberfläche) und Benutzerdokumentation.

## Grobe Vorgehensentscheidungen

| Bereich | Entscheidung | Begründung |
|---------|-------------|------------|
| Reihenfolge | Mechanik vor Oberfläche: Zuerst versionsbezogene Verzeichnisse und Einzelquellen-Prüfung, dann die Bedienoberfläche. | Der manuelle Update-Check soll Updates ohne Neustart wirksam machen — das setzt den versionsbezogenen Austausch voraus. Die UI-Schritte bauen auf den Service-Funktionen auf. |
| Neustartfreier Austausch | Der Austausch erfolgt über versionsbezogene Verzeichnisse; auf echtes Entladen geladener Plugin-Assemblies wird verzichtet. Die alte Version bleibt im Prozess, erhält aber keine neuen Aufgaben mehr. | Echtes Entladen ist mit dem bisherigen Ladeansatz nicht möglich; die Anforderung verlangt nur, dass Updates ohne Neustart wirksam werden und Altverzeichnisse nicht im wirksamen Bestand verbleiben. Eine Umstellung auf entladbare Kontexte ist ausdrücklich nicht Voraussetzung. |
| Nicht löschbare Altverzeichnisse | Werden zur Löschung markiert, sofort aus der Erkennung ausgeschlossen und beim nächsten Anwendungsstart bzw. beim nächsten Austausch bereinigt — kein Installationsabbruch mit Rücksicherung allein wegen offener Handles. | Die Anforderung verlangt, dass Altverzeichnisse nicht im wirksamen Pluginbestand bleiben; die Markierung trennt „wirksam" von „physisch noch vorhanden". |
| Vermisste Plugins | Das Entfernen vermisster Plugins wird als unabhängiger Schritt umgesetzt; es hängt nicht vom Update-Ablauf ab. | Die Funktion betrifft nur den Verwaltungseintrag und die bestehende Wiedererfassungslogik; sie ist eigenständig lieferbar und testbar. |
| Umfang | Keine automatische periodische Hintergrundprüfung, kein Downgrade-Angebot, keine Änderungen an Paketprüfung, GitHub-Anbindung, Token-Verwaltung oder mitgelieferten Plugins (Scope-Ausschlüsse der Anforderung). | Die Anforderung verlangt ausdrücklich nur Transparenz plus manuelle Prüfung; die genannten Ausschlüsse sind festgelegt. Eine periodische Prüfung ist verbindlich nicht Teil dieses Projekts; sie kann später als separate Folgeanforderung eingebracht werden. |

## Entwicklungsschritte

### Schritt 1: Versionsbezogene Pluginverzeichnisse und Löschmarkierung für Altbestände

**Beschreibung:** Neu installierte Pluginversionen aus einer GitHub-Pluginquelle werden künftig in eigenen, versionsbezogenen Verzeichnissen unterhalb des Plugin-Stammverzeichnisses abgelegt, statt das bisherige Verzeichnis an Ort und Stelle zu ersetzen. Beim koordinierten Austausch des Pluginbestands werden die Verzeichnisse der bisherigen Versionen entfernt. Lässt sich ein Altverzeichnis wegen noch offener Datei- oder Verzeichnishandles nicht löschen, wird es dauerhaft als zu löschen markiert, ab sofort von der Plugin-Erkennung ausgeschlossen und beim nächsten Anwendungsstart bzw. beim nächsten koordinierten Austausch bereinigt — es bleibt damit nicht im wirksamen Pluginbestand. Plugins, die im neuen Release nicht mehr enthalten sind, gelten anschließend als vermisst. Das bisherige Fehlerverhalten (Rücksicherung des vorherigen Stands bei fehlgeschlagener Installation oder fehlgeschlagenem Austausch) bleibt erhalten und wird auf das neue Verzeichnismodell angepasst. Das Namensschema der versionsbezogenen Verzeichnisse (Ableitung aus dem Release-Tag) und die konkrete Form der Löschmarkierung im Dateisystem sind im Rahmen der Umsetzung festzulegen; mitgelieferte Plugins aus dem Anwendungs-Publish sind von dieser Behandlung ausgenommen.

**Abhängigkeiten:** Keine

**Betroffene Bereiche:** Plugin-Installation und -Austausch, Plugin-Erkennung, Anwendungsstart

### Schritt 2: Manuelle Update-Prüfung für eine einzelne Pluginquelle

**Beschreibung:** Für eine einzelne erfasste GitHub-Pluginquelle kann eine Update-Prüfung gezielt ausgelöst werden — zusätzlich zur bestehenden automatischen Prüfung aller Quellen beim Anwendungsstart. Die Einzelprüfung folgt denselben Vorbedingungen wie die Start-Prüfung (Quelle aktiviert und Vertrauen bestätigt; bei privaten Quellen muss ein Zugangstoken hinterlegt sein) und durchläuft den bekannten Ablauf: neuestes Release abrufen, Archiv herunterladen, Paket prüfen, installieren, Release-Historie fortschreiben. Bereits installierte Releases werden wie bisher übersprungen, zuvor fehlgeschlagene werden erneut versucht. Die bekannten Statusangaben der Quelle (Zeitpunkt der letzten Prüfung, letztes erfolgreich installiertes Release, letzter Fehler samt Zeitpunkt) werden fortgeschrieben. Parallel laufende Prüfungen — ob manuell ausgelöst oder durch den Start-Lauf — werden serialisiert, sodass es keine gleichzeitigen Installationen gibt. Ein gefundenes Update wird so installiert, dass es ohne Anwendungsneustart wirksam wird. Die Auslösung ist Administratoren vorbehalten; das Ergebnis der Prüfung (kein Update gefunden / installiertes Release / Fehler) wird als Rückgabe bereitgestellt, damit die Oberfläche es dem Anwender unmittelbar rückmelden kann — zusätzlich zur Fortschreibung der Statusfelder der Quelle.

**Abhängigkeiten:** 1

**Betroffene Bereiche:** Update-Prüfung, Plugin-Installation und -Austausch, Admin-Berechtigungen

### Schritt 3: Vermisste Plugins aus der Verwaltung entfernen

**Beschreibung:** Plugins, die in der Pluginverwaltung als vermisst geführt werden (sie wurden bei der Erkennung nicht mehr gefunden), können dauerhaft aus der Verwaltung entfernt werden. Die Entfernen-Funktion steht nur für Einträge im Vermisst-Status zur Verfügung und ist Administratoren vorbehalten; entfernt wird ausschließlich der Verwaltungseintrag. Wird das Plugin zu einem späteren Zeitpunkt wieder entdeckt, wird es wie ein neues Plugin behandelt: Es erhält einen frischen Ersterfassungszeitpunkt und wird an das Ende der Reihenfolge angehängt. Zu beachten ist, dass die Pluginverwaltung Teil des Systemexports ist — ein wiederhergestelltes Backup kann entfernte Einträge zurückbringen; dieses Verhalten bleibt unverändert.

**Abhängigkeiten:** Keine

**Betroffene Bereiche:** Pluginverwaltung, Admin-Berechtigungen

### Schritt 4: Pluginverwaltung — Update-Prüfung je Quelle auslösen und Prüfprinzip transparent machen

**Beschreibung:** In der Pluginverwaltung der Admin-Oberfläche wird für den Anwender sichtbar erklärt, nach welchem Prinzip die Update-Prüfung stattfindet: automatisch einmalig beim Anwendungsstart für alle aktivierten und vertrauensbestätigten Quellen sowie manuell je Quelle. Jede Pluginquelle erhält eine Aktion „Auf Updates prüfen", die den manuellen Check für genau diese Quelle auslöst; die Aktion steht unter denselben Vorbedingungen wie die Start-Prüfung (Quelle aktiviert und Vertrauen bestätigt), wird bei nicht erfüllten Vorbedingungen deaktiviert dargestellt und ist während eines laufenden Checks gesperrt bzw. als beschäftigt angezeigt. Das Ergebnis wird dem Anwender direkt rückgemeldet (z. B. „kein Update gefunden", „Version X installiert" oder die Fehlermeldung); zusätzlich bleiben die je Quelle vorhandenen Statusangaben (letzte Prüfung, letztes erfolgreiches Release, letzter Fehler) sichtbar und aktualisieren sich. Die bisherigen Textbuttons der Quellen-Aktionen (Bearbeiten, Löschen) und der neue Prüf-Button werden einheitlich als Symbolbuttons mit den bereits eingebundenen Bootstrap-Icons sowie lokalisierten Tooltips und Accessible-Labels dargestellt. Alle neuen Texte sind zu lokalisieren.

**Abhängigkeiten:** 2

**Betroffene Bereiche:** Pluginverwaltung (Admin-Oberfläche), Lokalisierung

### Schritt 5: Pluginverwaltung — vermisste Plugins entfernen

**Beschreibung:** In der Pluginliste der Admin-Oberfläche erhalten Einträge mit Vermisst-Status eine Entfernen-Aktion, die den Verwaltungseintrag löscht. Die Aktion wird als Symbolbutton mit lokalisiertem Tooltip und Accessible-Label umgesetzt und steht nur bei vermissten Plugins zur Verfügung. Nach dem Entfernen verschwindet der Eintrag aus der Liste; wird das Plugin später wieder entdeckt, erscheint es als neuer Eintrag am Ende der Reihenfolge.

**Abhängigkeiten:** 3

**Betroffene Bereiche:** Pluginverwaltung (Admin-Oberfläche), Lokalisierung

### Schritt 6: Dokumentation der Update-Prüfung und Pluginverwaltung

**Beschreibung:** Die Benutzerdokumentation zur Pluginverwaltung (Plugin-Hilfe und Feature-Übersicht) wird aktualisiert: Sie beschreibt das Prüfprinzip (automatisch einmalig beim Anwendungsstart sowie manuell je Quelle), die versionsbezogenen Pluginverzeichnisse samt Markierung und späterer Bereinigung nicht löschbarer Altverzeichnisse sowie die Entfernen-Aktion für vermisste Plugins.

**Abhängigkeiten:** 1, 4, 5

**Betroffene Bereiche:** Benutzerdokumentation

## Übersichtstabelle

| # | Titel | Abhängigkeiten | Betroffene Bereiche |
|---|-------|-----------------|----------------------|
| 1 | Versionsbezogene Pluginverzeichnisse und Löschmarkierung für Altbestände | Keine | Plugin-Installation und -Austausch, Plugin-Erkennung, Anwendungsstart |
| 2 | Manuelle Update-Prüfung für eine einzelne Pluginquelle | 1 | Update-Prüfung, Plugin-Installation und -Austausch, Admin-Berechtigungen |
| 3 | Vermisste Plugins aus der Verwaltung entfernen | Keine | Pluginverwaltung, Admin-Berechtigungen |
| 4 | Pluginverwaltung — Update-Prüfung je Quelle auslösen und Prüfprinzip transparent machen | 2 | Pluginverwaltung (Admin-Oberfläche), Lokalisierung |
| 5 | Pluginverwaltung — vermisste Plugins entfernen | 3 | Pluginverwaltung (Admin-Oberfläche), Lokalisierung |
| 6 | Dokumentation der Update-Prüfung und Pluginverwaltung | 1, 4, 5 | Benutzerdokumentation |

## Offene Punkte

Keine.
