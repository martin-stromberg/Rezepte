# Anforderung - Einkaufsliste

## Ziel

Die Anwendung erhaelt eine benutzerbezogene Einkaufsliste, die ueber einen neuen Hauptmenuepunkt erreichbar ist. Zutaten koennen direkt in der Einkaufsliste gepflegt oder aus einer Rezeptdetailseite uebernommen werden.

## Funktionale Anforderungen

- Im Hauptmenue gibt es den Menuepunkt `Einkaufsliste`, der zu einer Verwaltungsseite fuehrt.
- Die Einkaufsliste besteht aus Gruppen und Zutaten.
- Fuer neue oder leere Listen wird standardmaessig eine Standardgruppe angezeigt, in die sofort Eintraege eingegeben werden koennen.
- Die Pflege darf keine mehrstufigen Pflichtablaeufe fuer Gruppe anlegen, Gruppe speichern, Eintrag anlegen und Eintrag speichern erzwingen.
- Eingabefelder fuer Gruppen und Eintraege sind fruehzeitig sichtbar und direkt nutzbar.
- Von der Rezeptdetailseite koennen Zutaten in die Einkaufsliste uebernommen werden.
- Vor der Uebernahme aus einem Rezept erscheint ein Zwischendialog mit vorausgewaehlten Zutaten zur Bestaetigung.
- Bei Uebernahme aus einem Rezept wird eine neue Gruppe mit dem Namen des Rezepts angelegt.
- Die aus einem Rezept erzeugte Gruppe ist mit dem Rezept verknuepft.
- Die UI zeigt Zutaten an und erlaubt das Abhaken einzelner Zutaten.

## Nicht-funktionale Anforderungen

- Daten muessen benutzerbezogen isoliert sein.
- Die Umsetzung soll in die bestehende Blazor-Server- und EF-Core-Struktur passen.
- Die Bedienung soll auf Desktop und Mobilgeraeten ergonomisch sein.

## Akzeptanzkriterien

- Angemeldete Benutzer sehen im Hauptmenue `Einkaufsliste` und erreichen `/shopping-list`.
- Eine neue Liste zeigt automatisch eine nutzbare Standardgruppe.
- Zutaten koennen ohne separaten Speichern-Dialog hinzugefuegt, bearbeitet, abgehakt und geloescht werden.
- Gruppen koennen direkt benannt, erstellt und geloescht werden.
- Auf einer Rezeptdetailseite koennen Zutaten mit vorausgewaehlter Auswahl bestaetigt und als Rezeptgruppe in die Einkaufsliste uebernommen werden.
- Die erstellte Rezeptgruppe enthaelt Mengen, Einheiten und Namen der uebernommenen Zutaten.
- Tests decken zentrale Service-Regeln ab.
