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

Backup und die klassischen Webseitenquellen laufen als separate produktive Pluginprojekte:

- `Rezepte.Import.Plugins.Backup`
- `Rezepte.Import.Plugins.Chefkoch`
- `Rezepte.Import.Plugins.SecondSource`
- `Rezepte.Import.Plugins.ThirdSource`
- `Rezepte.Import.Plugins.FourthSource`
- `Rezepte.Import.Plugins.FifthSource`
- `Rezepte.Import.Plugins.SixthSource`

Diese Plugins referenzieren die gemeinsame Vertragsschicht und liefern neutrale Rezeptdaten zurueck. Der Host persistiert daraus Rezepte, Zutaten, Schritte, Bilder und Kochbuchzuordnungen.

KI-Foto und KI-URL sind bewusst Hostadapter im Webprojekt. Diese Entscheidung vermeidet einen kuenstlich aufgeweiteten Pluginvertrag fuer hostinterne Services wie AI-Konfiguration, Usage-Limits, Google Vision, Gemini, Cache und interaktive Bestaetigung. Die AI-Handler nehmen trotzdem am Plugin-Auswahlmodell teil, liefern ihre Ergebnisse als neutrale Import-DTOs und nutzen denselben zentralen Persistenzpfad wie externe Plugins.

## Qualitaetssicherung

Die produktiven externen Pluginparser sind mit dedizierten Fixture-Tests abgedeckt. Die Tests pruefen Backup-ZIP-Dateien sowie repraesentative HTML-/JSON-Strukturen fuer Chefkoch, SecondSource, ThirdSource, FourthSource, FifthSource und SixthSource ueber den oeffentlichen Importvertrag.
