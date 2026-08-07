← [Zurück zur Übersicht](index.md)

# security.txt — Business Rules

## Pflichtfelder bei aktivierter Funktion

**Beschreibung:** RFC 9116 schreibt `Contact` und `Expires` als Pflichtfelder vor. Da eine reine UI-Validierung direkten API-Aufrufen gegenüber nicht robust wäre, wird die Validierung serverseitig erzwungen.

**Bedingungen:**
- `Enabled == true`

**Verhalten:**
- Wenn `Contact` fehlt oder ein Leerstring ist: HTTP 400, Fehlermeldung „Contact ist ein Pflichtfeld, wenn security.txt aktiviert ist."
- Wenn `Expires` fehlt (`null`): HTTP 400, Fehlermeldung „Expires ist ein Pflichtfeld, wenn security.txt aktiviert ist."
- Wenn `Expires` in der Vergangenheit liegt: HTTP 400, Fehlermeldung „Expires muss in der Zukunft liegen."
- Wenn `Enabled == false`: Keine Validierung der anderen Felder — die Konfiguration wird ohne `Contact`/`Expires` gespeichert.

**Umsetzung:** `SettingsController.SetGlobalSecurityTxt` — Validierung vor dem Delegieren an `ISettingsService.SetSecurityTxtSettingsAsync`.

---

## Deaktivierungslogik (HTTP 404)

**Beschreibung:** Die security.txt-Funktion soll ohne Code-Änderung ab- und eingeschaltet werden können.

**Bedingungen:**
- `SecurityTxtSettings.Enabled == false` (Standardwert: `false`, wenn kein `AppSetting`-Eintrag existiert)

**Verhalten:**
- Alle vier öffentlichen Endpunkte (`/security.txt`, `/.well-known/security.txt`, `/.well-known/security.md`, `/.well-known/security.html`) geben HTTP 404 zurück.
- Die gespeicherten Direktiven bleiben in der Datenbank erhalten.

**Umsetzung:** `SecurityTxtController.GetSecurityTxt`, `GetSecurityMd`, `GetSecurityHtml` — `if (!settings.Enabled) return NotFound();`

---

## Mehrfachwerte (Contact, Acknowledgments)

**Beschreibung:** RFC 9116 erlaubt mehrere `Contact`- und `Acknowledgments`-Einträge (je Direktive mehrere Zeilen). Die Speicherung erfolgt als einzelner `AppSetting`-Eintrag; Mehrfachwerte werden durch Zeilenumbrüche getrennt.

**Verhalten:**
- Beim Speichern: Mehrere Werte werden zeilengetrennt in einem einzigen `AppSetting`-String hinterlegt.
- Beim Plain-Text-Rendering: Der String wird an `\n` gesplittet; jede Zeile erzeugt eine eigene `Key: Value`-Zeile im RFC-9116-Format.
- Beim Markdown- und HTML-Rendering: Der gesamte mehrzeilige String wird als Textkörper des Abschnitts ausgegeben (keine weitere Aufteilung).

**Umsetzung:** `SecurityTxtRenderer.AppendMultiline` (Plain-Text), `AppendMarkdownSection`/`AppendHtmlSection` (Markdown/HTML).

---

## Null-Werte löschen AppSetting-Einträge

**Beschreibung:** Optionale Direktiven, die nicht gesetzt sind (`null`), sollen keinen leeren Eintrag in der `AppSetting`-Tabelle hinterlassen.

**Verhalten:**
- Wird ein Feld mit `null` übergeben, wird der zugehörige `AppSetting`-Eintrag gelöscht (falls vorhanden).
- Felder mit Leerstring verhalten sich wie `null`.

**Umsetzung:** `SettingsService.SetSecurityTxtSettingsAsync` — `Upsert`-Hilfsmethode: `if (value == null) { if (kv != null) _db.Remove(kv); return; }`

---

## Expires-Format

**Beschreibung:** `Expires` wird intern als `DateTimeOffset` verarbeitet und im ISO-8601-Round-Trip-Format (`"O"`) in der `AppSetting`-Tabelle gespeichert, damit eine verlustfreie Rundreise zwischen Lesen und Schreiben gewährleistet ist.

**Umsetzung:**
- Speichern: `settings.Expires?.ToString("O")` in `SettingsService.SetSecurityTxtSettingsAsync`
- Lesen: `DateTimeOffset.TryParseExact(expiresRaw, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, ...)` in `SettingsService.GetSecurityTxtSettingsAsync`
