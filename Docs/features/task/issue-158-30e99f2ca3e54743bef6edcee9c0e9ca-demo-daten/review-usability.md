# Usability-Review

## Ergebnis

**Status:** Keine Befunde

## Geprüfte Interaktionen

Liste der aus der Anforderung geprüften Benutzerinteraktionen:
- Registrierung mit optionaler Wahl „Demo-Daten anlegen" über eine Checkbox im Registrierungsformular → unauffällig. Die Checkbox (`id="register-create-demo-data"`) hat ein verständlich beschriftetes Label („Demo-Daten anlegen") und einen Hilfstext in Klartext („Legt automatisch fünf Beispiel-Kochbücher mit Rezepten, Terminen und Einkaufslisteneinträgen an."), der den Umfang der Beispieldaten ohne Fachbegriffe beschreibt.
- Absenden des Registrierungsformulars → unauffällig. Vorhandener, klar beschrifteter Submit-Button „Registrieren"; der Checkbox-Wert wird über das `name`-Attribut automatisch mit übertragen — keine zusätzliche Handlung oder technische Eingabe nötig.
- Keine internen Kennungen erforderlich: Der Anwender muss weder Ids, Codes noch andere technische Werte kennen oder eingeben.
- Keine Auswahl-/Identifikationsaufgabe: Die Anforderung verlangt keine Auswahl aus benannten Entitäten (kein Dropdown, keine Suche nötig); die einzige Entscheidung ist ja/nein.
- Musterwiederverwendung: Die Checkbox folgt dem etablierten Bootstrap-`form-check`-Muster und dem bestehenden `EditForm`-Ansatz der Seite; Lokalisierung erfolgt konsistent über `IStringLocalizer<UiStrings>` wie bei den übrigen Feldern.

## Geprüfte Dateien

Liste aller geprüften UI-Dateien:
- `Rezepte.Web/Components/Pages/Register.razor`
- `Rezepte.Web/Resources/UiStrings.resx` (lokalisierte Texte `CreateDemoData`, `CreateDemoDataHelp`)
