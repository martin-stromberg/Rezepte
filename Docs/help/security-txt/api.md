← [Zurück zur Übersicht](index.md)

# security.txt — API

## Übersicht

Es gibt zwei Endpunktgruppen:

1. **Öffentliche Endpunkte** — liefern die security.txt im jeweiligen Format, keine Authentifizierung erforderlich.
2. **Admin-Endpunkte** — lesen und schreiben die Konfiguration, erfordern eine JWT-Authentifizierung mit der Rolle `Admin`.

---

## Authentifizierung

**Öffentliche Endpunkte:** Keine Authentifizierung erforderlich.

**Admin-Endpunkte:** Bearer-Token im `Authorization`-Header:
```
Authorization: Bearer <jwt-token>
```
Das Token muss die Rolle `Admin` enthalten.

---

## Öffentliche Endpunkte

### `GET /security.txt`
### `GET /.well-known/security.txt`

**Beschreibung:** Liefert die security.txt im RFC-9116-Plain-Text-Format.

**Antwort:**

| Statuscode | Beschreibung |
|-----------|--------------|
| 200 OK | `Content-Type: text/plain; charset=utf-8`, Body: RFC-9116-Direktiven |
| 404 Not Found | `Enabled` ist `false` |

**Beispielantwort (200):**
```
Contact: mailto:security@example.com
Expires: 2026-12-31T00:00:00.0000000+00:00
Canonical: https://example.com/security.txt
Policy: https://example.com/security-policy
```

---

### `GET /.well-known/security.md`

**Beschreibung:** Liefert die security.txt im Markdown-Format.

**Antwort:**

| Statuscode | Beschreibung |
|-----------|--------------|
| 200 OK | `Content-Type: text/markdown; charset=utf-8` |
| 404 Not Found | `Enabled` ist `false` |

**Beispielantwort (200):**
```markdown
## Contact

mailto:security@example.com

## Expires

2026-12-31T00:00:00.0000000+00:00

## Canonical

https://example.com/.well-known/security.md
```

---

### `GET /.well-known/security.html`

**Beschreibung:** Liefert die security.txt im HTML-Format.

**Antwort:**

| Statuscode | Beschreibung |
|-----------|--------------|
| 200 OK | `Content-Type: text/html; charset=utf-8` |
| 404 Not Found | `Enabled` ist `false` |

**Beispielantwort (200):**
```html
<h2>Contact</h2><p>mailto:security@example.com</p>
<h2>Expires</h2><p>2026-12-31T00:00:00.0000000+00:00</p>
<h2>Canonical</h2><p>https://example.com/.well-known/security.html</p>
```

---

## Admin-Endpunkte

### `GET /api/settings/global/securitytxt`

**Beschreibung:** Liest die aktuelle security.txt-Konfiguration.

**Authentifizierung:** Bearer-Token, Rolle `Admin`

**Rückgabe:**

```json
{
  "enabled": true,
  "contact": "mailto:security@example.com",
  "expires": "2026-12-31T00:00:00+00:00",
  "encryption": null,
  "acknowledgments": null,
  "preferredLanguages": "de, en",
  "canonical": null,
  "policy": null,
  "hiring": null
}
```

| Feld | Typ | Beschreibung |
|------|-----|--------------|
| `enabled` | `bool` | Funktion aktiviert |
| `contact` | `string?` | RFC-9116-Direktive `Contact`; mehrere Werte zeilengetrennt |
| `expires` | `string?` (ISO 8601) | RFC-9116-Direktive `Expires` |
| `encryption` | `string?` | RFC-9116-Direktive `Encryption` |
| `acknowledgments` | `string?` | RFC-9116-Direktive `Acknowledgments`; mehrere Werte zeilengetrennt |
| `preferredLanguages` | `string?` | RFC-9116-Direktive `Preferred-Languages` |
| `canonical` | `string?` | Nicht konfigurierbar; wird in der Antwort aktuell immer `null` geliefert |
| `policy` | `string?` | RFC-9116-Direktive `Policy` |
| `hiring` | `string?` | RFC-9116-Direktive `Hiring` |

| Statuscode | Beschreibung |
|-----------|--------------|
| 200 OK | Konfiguration als JSON |
| 401 Unauthorized | Kein gültiges Token |
| 403 Forbidden | Token vorhanden, aber Rolle `Admin` fehlt |

---

### `PUT /api/settings/global/securitytxt`

**Beschreibung:** Speichert die security.txt-Konfiguration.

**Authentifizierung:** Bearer-Token, Rolle `Admin`

**Body:** JSON mit denselben Feldern wie die GET-Antwort.

Hinweis: Ein übergebener Wert in `canonical` wird serverseitig ignoriert und nicht gespeichert.

**Fehler:**

| Statuscode | Ursache |
|-----------|---------|
| 204 No Content | Erfolgreich gespeichert |
| 400 Bad Request | `Enabled = true` und `Contact` fehlt |
| 400 Bad Request | `Enabled = true` und `Expires` fehlt |
| 400 Bad Request | `Enabled = true` und `Expires` liegt in der Vergangenheit |
| 401 Unauthorized | Kein gültiges Token |
| 403 Forbidden | Rolle `Admin` fehlt |
