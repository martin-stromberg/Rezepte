# Import-Plugins

Die Anwendung verwendet fuer Rezeptimporte ein Plugin-Framework. Der Import auf der Startseite laeuft nicht mehr ueber eine feste Handlerliste, sondern fragt den `PluginManager` nach den aktivierten Import-Plugins in der gespeicherten Reihenfolge.

## Administration

Administratoren finden die Pluginverwaltung in den Einstellungen unter `Plugins`.

Die Liste zeigt jedes bekannte Plugin mit Name, Plugin-ID, Status, Assembly und Handler-Typ. Plugins koennen dort aktiviert oder deaktiviert werden. Die Reihenfolge wird ueber die Pfeile nach oben und unten geaendert und dauerhaft gespeichert.

Beim Import werden nur aktivierte Plugins mit Status `Loaded` beruecksichtigt. Deaktivierte, fehlende oder fehlerhafte Plugins werden nicht angesprochen.

## Erkennung und Reihenfolge

Beim Programmstart sucht die Anwendung im Programmverzeichnis unter `plugins` nach Plugin-DLLs. Unterstuetzt werden DLLs direkt im Ordner `plugins` sowie DLLs in direkten Unterordnern von `plugins`.

Neu erkannte Plugins werden automatisch in der Datenbank gespeichert und hinten an die bestehende Reihenfolge angehaengt. Bereits konfigurierte Plugins behalten ihre Reihenfolge. Wenn ein zuvor bekanntes Plugin beim Start nicht mehr gefunden wird, bleibt es in der Verwaltung sichtbar und erhaelt den Status `Missing`.

Fehlerhafte oder inkompatible Plugin-DLLs werden nicht fuer Imports genutzt. Sie werden mit Fehlerstatus und Fehlermeldung in der Pluginliste angezeigt.

## Importablauf

Fuer jeden Import werden frische Handlerinstanzen aus den aktivierten Plugins erzeugt. Die Anwendung prueft die Plugins der Reihe nach:

1. Das Plugin erhaelt die Importdatei oder URL und prueft, ob es die Quelle verarbeiten kann.
2. Das erste passende Plugin verarbeitet den Import.
3. Weitere Plugins werden danach nicht mehr probiert.
4. Wenn kein aktiviertes Plugin passt, endet der Import mit der Meldung `No suitable import plugin found for this file or URL.`

Interaktive Importpfade, zum Beispiel KI-Importe mit Bestaetigungsdialog, laufen weiterhin ueber den bestehenden Importdialog.

## Aktueller Umsetzungsstand

Der erreichte Stand ist ein Plugin-Framework mit gemeinsamer Vertragsschicht `Rezepte.Import.Abstractions`, persistierter Plugin-Konfiguration, Start-Erkennung externer Plugin-DLLs, Admin-UI sowie Plugin-basierter Auswahl im Datei- und URL-Import.

Die bestehenden Importquellen sind derzeit weiterhin als Built-in-Plugins im Webprojekt registriert. Es gibt noch keine produktiven separaten Pluginprojekte pro Importquelle, und die vorhandenen Handler speichern weiterhin ueber die bestehenden Host-Services. Die vorbereiteten neutralen Import-DTOs sind noch nicht der produktive Rueckgabeweg fuer die bestehenden Quellen.

## Einschraenkungen

- Produktive Importquellen sind noch nicht aus `Rezepte.Web` in eigene Klassenbibliotheksprojekte ausgelagert.
- Built-in-Plugins haben aktuell Vorrang, wenn ein externes Plugin dieselbe Plugin-ID verwendet.
- Direkte DLLs im Root von `plugins` koennen bei nebenliegenden Abhaengigkeiten noch zu unklaren Fehler- oder Inkompatibilitaetseintraegen fuehren.
- Instanziierungsfehler eines geladenen Handlers werden beim Import protokolliert, aber noch nicht als eigener persistierter Pluginstatus dargestellt.
- Die neutralen Rezept-DTOs und das hostseitige Mapping sind vorbereitet, aber noch nicht vollstaendig in den Importfluss integriert.
