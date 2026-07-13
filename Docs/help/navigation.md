# Navigation und Benutzerbereich

Die Anwendung nutzt oben eine responsive Menueleiste. Auf kleinen Bildschirmen wird die Navigation ueber den Menue-Schalter ein- und ausgeklappt, damit Links, Suche und Benutzeraktionen bedienbar bleiben.

## Startseite

Die Startseite ist ueber den Anwendungsnamen `Rezepte` links in der Menueleiste erreichbar. Einen separaten Menuepunkt `Start` gibt es nicht.

## Hauptnavigation

Die Menueleiste enthaelt direkte Links zu den wichtigsten Bereichen:

- `Kochbuecher`
- `Kalender`
- Rezeptsuche

Die Rezeptsuche bleibt auch in der mobilen Ansicht innerhalb der ausgeklappten Navigation nutzbar.

## Anmeldung und Benutzerkonto

Nicht angemeldete Benutzer sehen in der Menueleiste den Link `Anmelden`.

Angemeldete Benutzer sehen rechts in der Menueleiste ein Benutzerlogo. Ein Klick auf das Benutzerlogo oeffnet ein Benutzermenue mit den verfuegbaren Angaben zum angemeldeten Benutzer, zum Beispiel Benutzername, Rolle und falls vorhanden die Benutzer-ID.

Im Benutzermenue befindet sich auch die Aktion `Abmelden`. Die Abmeldung funktioniert wie bisher und fuehrt anschliessend zur Login-Seite.

Hinweise zur Registrierung, Profilbearbeitung und Benutzerverwaltung stehen in `Docs/help/user-accounts.md`.

## Einrichtung

Die Einrichtung ist fuer angemeldete Benutzer ueber das Zahnradsymbol in der Menueleiste erreichbar. Der fruehere Text `Einrichtung` wird in der Menueleiste nicht mehr angezeigt; das Symbol oeffnet weiterhin die Einstellungsseite.

Administratoren finden dort auch den Bereich `Plugins` zur Verwaltung der Import-Plugins. Details stehen in `Docs/help/import-plugins.md`.

