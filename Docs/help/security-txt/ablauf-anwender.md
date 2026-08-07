← [Zurück zur Übersicht](index.md)

# security.txt — Ablauf für Anwender

## Voraussetzungen

- Der Benutzer ist mit der Rolle **Admin** angemeldet.
- Die Einstellungsseite ist erreichbar.

---

## security.txt aktivieren und konfigurieren

### 1. Einstellungen öffnen

Navigieren Sie in der Anwendung zur Einstellungsseite und klicken Sie auf den Menüpunkt **security.txt** (🔒).

> **Hinweis:** Dieser Menüpunkt ist nur für Benutzer mit der Rolle „Admin" sichtbar.

### 2. Funktion aktivieren

Setzen Sie den Schalter **Aktiviert** auf „Ein". Erst dann sind die Felder für die Direktiven verpflichtend auszufüllen.

### 3. Pflichtfelder ausfüllen

Wenn die Funktion aktiviert ist, müssen folgende Felder ausgefüllt werden:

- **Contact** — Kontaktadresse für Sicherheitsmeldungen, z. B. `mailto:security@example.com`. Mehrere Adressen können eingetragen werden, eine pro Zeile.
- **Expires** — Ablaufdatum der security.txt-Datei. Das Datum muss in der Zukunft liegen.

### 4. Optionale Felder ausfüllen

Folgende Felder sind optional:

| Feld | Beispielwert | Bedeutung |
|------|-------------|-----------|
| **Encryption** | `https://example.com/pgp-key.asc` | URL zu einem PGP-Public-Key |
| **Acknowledgments** | `https://example.com/thanks` | URL zu einer Danksagungsseite; mehrere URLs möglich (eine pro Zeile) |
| **Preferred-Languages** | `de, en` | Bevorzugte Sprachen für Meldungen |
| **Canonical** | `https://example.com/.well-known/security.txt` | Kanonische URL dieser Datei (öffentliche Adresse der eigenen Instanz) |
| **Policy** | `https://example.com/security-policy` | URL zur Sicherheitsrichtlinie |
| **Hiring** | `https://example.com/jobs` | URL zu sicherheitsrelevanten Stellenangeboten |

### 5. Speichern

Klicken Sie auf **Speichern**. Bei Erfolg wird eine Bestätigung angezeigt.

> **Hinweis:** Wenn **Aktiviert** eingeschaltet ist und **Contact** oder **Expires** fehlen (oder **Expires** in der Vergangenheit liegt), wird das Speichern mit einer Fehlermeldung abgewiesen.

---

## Ergebnis

Nach dem Speichern ist die `security.txt`-Datei ohne Anmeldung unter folgenden Adressen erreichbar:

- `https://ihre-instanz.example.com/security.txt`
- `https://ihre-instanz.example.com/.well-known/security.txt`
- `https://ihre-instanz.example.com/.well-known/security.md` (Markdown-Format)
- `https://ihre-instanz.example.com/.well-known/security.html` (HTML-Format)

---

## Funktion deaktivieren

Setzen Sie den Schalter **Aktiviert** auf „Aus" und speichern Sie. Alle vier Endpunkte antworten daraufhin mit „Nicht gefunden" (HTTP 404). Die eingetragenen Direktiven bleiben gespeichert und können jederzeit reaktiviert werden.
