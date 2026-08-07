← [Zurück zur Übersicht](index.md)

# security.txt — Beschreibung

## Zweck

`security.txt` ist ein standardisiertes Format (RFC 9116), das Sicherheitsforschern mitteilt, wie Sicherheitslücken an den Betreiber einer Webanwendung gemeldet werden können. Die Datei wird unter definierten Pfaden öffentlich ohne Authentifizierung ausgeliefert.

## Funktionsweise

Administratoren aktivieren und konfigurieren die Funktion im Einstellungsbereich unter dem Menüpunkt **security.txt** (🔒). Die eingetragenen Direktiven werden als Key-Value-Einträge in der `AppSetting`-Tabelle persistiert (Schlüsselpräfix `SecurityTxt.*`).

Die `Canonical`-Direktive wird nicht im Admin-Formular gepflegt. Stattdessen berechnet der Server beim Ausliefern automatisch die kanonische URL passend zum jeweils angeforderten Ausgabeformat.

Ist die Funktion aktiviert, liefert die Anwendung den Inhalt unter vier öffentlichen Endpunkten aus:

| Pfad | Format | Content-Type |
|------|--------|--------------|
| `/security.txt` | RFC-9116-Plain-Text | `text/plain; charset=utf-8` |
| `/.well-known/security.txt` | RFC-9116-Plain-Text | `text/plain; charset=utf-8` |
| `/.well-known/security.md` | Markdown (Abschnitte mit `## Key`) | `text/markdown; charset=utf-8` |
| `/.well-known/security.html` | HTML (`<h2>`/`<p>`) | `text/html; charset=utf-8` |

Ist die Funktion deaktiviert (`Enabled = false`), antworten alle vier Endpunkte mit **HTTP 404**.

## Direktiven

| Direktive | Pflicht bei Aktivierung | Beschreibung |
|-----------|------------------------|--------------|
| `Contact` | Ja | Kontakt-URI oder E-Mail für Sicherheitsmeldungen; mehrere Werte möglich (ein Wert pro Zeile) |
| `Expires` | Ja | Ablaufdatum der Datei im ISO-8601-Format; muss in der Zukunft liegen |
| `Encryption` | Nein | URL zu einem PGP-Public-Key |
| `Acknowledgments` | Nein | URL zu einer Danksagungsseite; mehrere Werte möglich (ein Wert pro Zeile) |
| `Preferred-Languages` | Nein | Bevorzugte Sprachen für Sicherheitsmeldungen |
| `Canonical` | Nein | Wird serverseitig automatisch je Ausgabeformat gesetzt |
| `Policy` | Nein | URL zur Sicherheitsrichtlinie |
| `Hiring` | Nein | URL zu sicherheitsrelevanten Stellenangeboten |

## Beispiele

**Minimalbeispiel (nur Pflichtfelder):**

```
Contact: mailto:security@example.com
Expires: 2026-12-31T00:00:00+00:00
```

**Vollständiges Beispiel mit Mehrfachwerten:**

```
Contact: mailto:security@example.com
Contact: https://example.com/security-report
Expires: 2026-12-31T00:00:00+00:00
Encryption: https://example.com/pgp-key.asc
Acknowledgments: https://example.com/thanks
Preferred-Languages: de, en
Canonical: https://example.com/security.txt
Policy: https://example.com/security-policy
Hiring: https://example.com/jobs/security
```

## Einschränkungen

- PGP-Signierung der Datei (laut RFC 9116 empfohlen) ist nicht implementiert.
- Die `Canonical`-URL kann nicht manuell gesetzt werden; sie wird aus Request-Schema, Host, PathBase und Ausgabeformat erzeugt.
- Mehrfachwerte für `Contact` und `Acknowledgments` werden als mehrzeiliger Text eingegeben (ein Wert pro Zeile) und beim Rendering in separate Direktiv-Zeilen aufgeteilt.
