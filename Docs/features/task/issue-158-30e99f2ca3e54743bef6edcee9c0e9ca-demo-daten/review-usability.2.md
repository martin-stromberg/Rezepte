# Usability-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### Register.razor (Registrierungsformular)

- **Erreichbarkeit** — Die neue Checkbox ist sichtbar und anklickbar, aber die Beschriftung „Demo-Daten anlegen“ vermittelt aus Endanwendersicht nicht, welche konkreten Daten beim Registrieren erzeugt werden (Kochbücher, Rezepte, Kalendereinträge, Einkaufsliste). Eine nicht-technische Anwenderin kann nicht abschätzen, was sie aktiviert und ob das sinnvoll ist.

  Empfehlung: Hilfstext unter oder neben der Checkbox ergänzen, der den Umfang beschreibt, z. B. „Legt automatisch fünf Beispiel-Kochbücher mit Rezepten, Terminen und Einkaufslisteneinträgen an.“

## Geprüfte Interaktionen

- Bei der Registrierung per Checkbox wählen, ob Demo-Daten angelegt werden → Befund vorhanden

## Geprüfte Dateien

- `Rezepte.Web/Components/Pages/Register.razor`
- `Rezepte.Web/Resources/UiStrings.resx`
