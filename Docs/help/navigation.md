# Navigation und Benutzerbereich

Die Anwendung nutzt oben eine responsive Menüleiste. Auf kleinen Bildschirmen wird die Navigation über den Menü-Schalter ein- und ausgeklappt, damit Links, Suche und Benutzeraktionen bedienbar bleiben.

## Startseite

Die Startseite ist über den Anwendungsnamen `Rezepte` links in der Menüleiste erreichbar. Einen separaten Menüpunkt `Start` gibt es nicht.

## Hauptnavigation

Die Menüleiste enthält direkte Links zu den wichtigsten Bereichen:

- `Kochbücher`
- `Kalender`
- Rezeptsuche

Die Rezeptsuche bleibt auch in der mobilen Ansicht innerhalb der ausgeklappten Navigation nutzbar.

## Anmeldung und Benutzerkonto

Nicht angemeldete Benutzer sehen in der Menüleiste den Link `Anmelden`.

Angemeldete Benutzer sehen rechts in der Menüleiste ein Benutzerlogo. Ein Klick auf das Benutzerlogo öffnet ein Benutzermenü mit den verfügbaren Angaben zum angemeldeten Benutzer, zum Beispiel Benutzername, Rolle und falls vorhanden die Benutzer-ID.

Im Benutzermenü befindet sich auch die Aktion `Abmelden`. Die Abmeldung funktioniert wie bisher und fuehrt anschliessend zur Login-Seite.

Hinweise zur Registrierung, Profilbearbeitung und Benutzerverwaltung stehen in `Docs/help/user-accounts.md`.

## Ladeanimation

Unter der Navigationsleiste wird während der Navigation automatisch eine schmale, horizontale Ladeanimation angezeigt. Diese gibt dem Benutzer visuelles Feedback, dass seine Interaktion erkannt wurde — besonders wichtig auf langsamen Servern, wo Navigationen verzögert sein können.

Die Animation wird ausgeloest durch:
- Klick auf einen Navigationslink
- Absenden der Suchleiste
- Absenden von Formularen: Login, Registrierung, Abmeldung oder andere Formulare

Der Ladebalken wird mit einer zufällig gewählten Farbe aus einer vorkonfigurierten Palette angezeigt und bewegt sich von rechts nach links. Er verschwindet nach Abschluss der Navigation. Bei wiederholten, schnellen Navigationen wird die Farbe bei jedem Klick neu gewählt, damit der Farbwechsel dem Benutzer signalisiert, dass seine Interaktion registriert wurde.

Konfiguration: Ein Administrator kann das Feature vollständig deaktivieren, die Hoehe, die Animationsdauer, die Verzögerung bis zum Ausblenden und die verfügbare Farbliste in der `appsettings.json` unter dem Abschnitt `LoadingBar` anpassen.

Barrierefreiheit: Der Ladebalken wird von Screenreadern ausgeblendet (`aria-hidden="true"`). Sie respektiert die Benutzereinstellung `prefers-reduced-motion: reduce` — statt einer kontinuierlichen Bewegung erscheint in diesem Fall ein statischer, farbiger Balken.

## Einrichtung

Die Einrichtung ist für angemeldete Benutzer über das Zahnradsymbol in der Menüleiste erreichbar. Der frühere Text `Einrichtung` wird in der Menüleiste nicht mehr angezeigt; das Symbol öffnet weiterhin die Einstellungsseite.

Administratoren finden dort auch den Bereich `Plugins` zur Verwaltung der Import-Plugins. Details stehen in `Docs/help/import-plugins.md`.

