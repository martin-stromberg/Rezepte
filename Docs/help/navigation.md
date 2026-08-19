# Navigation und Benutzerbereich

Die Anwendung nutzt oben eine responsive MenÃ¼leiste. Auf kleinen Bildschirmen wird die Navigation Ã¼ber den MenÃ¼-Schalter ein- und ausgeklappt, damit Links, Suche und Benutzeraktionen bedienbar bleiben.

## Startseite

Die Startseite ist Ã¼ber den Anwendungsnamen `Rezepte` links in der MenÃ¼leiste erreichbar. Einen separaten MenÃ¼punkt `Start` gibt es nicht.

## Hauptnavigation

Die MenÃ¼leiste enthÃ¤lt direkte Links zu den wichtigsten Bereichen:

- `KochbÃ¼cher`
- `Kalender`
- Rezeptsuche

Die Rezeptsuche bleibt auch in der mobilen Ansicht innerhalb der ausgeklappten Navigation nutzbar.

## Anmeldung und Benutzerkonto

Nicht angemeldete Benutzer sehen in der MenÃ¼leiste den Link `Anmelden`.

Angemeldete Benutzer sehen rechts in der MenÃ¼leiste ein Benutzerlogo. Ein Klick auf das Benutzerlogo Ã¶ffnet ein BenutzermenÃ¼ mit den verfÃ¼gbaren Angaben zum angemeldeten Benutzer, zum Beispiel Benutzername, Rolle und falls vorhanden die Benutzer-ID.

Im BenutzermenÃ¼ befindet sich auch die Aktion `Abmelden`. Die Abmeldung funktioniert wie bisher und fuehrt anschliessend zur Login-Seite.

Hinweise zur Registrierung, Profilbearbeitung und Benutzerverwaltung stehen in `Docs/help/user-accounts.md`.

## Ladeanimation

Unter der Navigationsleiste wird wÃ¤hrend der Navigation automatisch eine schmale, horizontale Ladeanimation angezeigt. Diese gibt dem Benutzer visuelles Feedback, dass seine Interaktion erkannt wurde — besonders wichtig auf langsamen Servern, wo Navigationen verzÃ¶gert sein kÃ¶nnen.

Die Animation wird ausgeloest durch:
- Klick auf einen Navigationslink
- Absenden der Suchleiste
- Absenden von Formularen: Login, Registrierung, Abmeldung oder andere Formulare

Der Ladebalken wird mit einer zufÃ¤llig gewÃ¤hlten Farbe aus einer vorkonfigurierten Palette angezeigt und bewegt sich von rechts nach links. Er verschwindet nach Abschluss der Navigation. Bei wiederholten, schnellen Navigationen wird die Farbe bei jedem Klick neu gewÃ¤hlt, damit der Farbwechsel dem Benutzer signalisiert, dass seine Interaktion registriert wurde.

Konfiguration: Ein Administrator kann das Feature vollstÃ¤ndig deaktivieren, die Hoehe, die Animationsdauer, die Verzögerung bis zum Ausblenden und die verfÃ¼gbare Farbliste in der `appsettings.json` unter dem Abschnitt `LoadingBar` anpassen.

Barrierefreiheit: Der Ladebalken wird von Screenreadern ausgeblendet (`aria-hidden="true"`). Sie respektiert die Benutzereinstellung `prefers-reduced-motion: reduce` — statt einer kontinuierlichen Bewegung erscheint in diesem Fall ein statischer, farbiger Balken.

## Einrichtung

Die Einrichtung ist fÃ¼r angemeldete Benutzer Ã¼ber das Zahnradsymbol in der MenÃ¼leiste erreichbar. Der frÃ¼here Text `Einrichtung` wird in der MenÃ¼leiste nicht mehr angezeigt; das Symbol Ã¶ffnet weiterhin die Einstellungsseite.

Administratoren finden dort auch den Bereich `Plugins` zur Verwaltung der Import-Plugins. Details stehen in `Docs/help/import-plugins.md`.

