# Risiken und offene technische Entscheidungen

## Aehnlichkeitspruefung

Die Anforderung verlangt Ablehnung offensichtlicher Umgehungsschreibweisen, ohne legitime Namen uebermaessig oft abzulehnen. Ein zu aggressiver Levenshtein-Ansatz kann viele normale Namen treffen. Konservativer ist:

- Normalisierung typischer Leetspeak-Zeichen (`0 -> o`, `1 -> i/l`, `3 -> e`, `4 -> a`, `5 -> s`, `7 -> t`)
- Vergleich der normalisierten Eingabe gegen kurze Hochrisiko-Namen wie `admin`, `root`, `support`
- optional nur sehr kleine Distanzschwelle fuer kurze reservierte Namen

Diese Entscheidung sollte in der Planung konkretisiert und testgetrieben abgesichert werden.

## Domains und IP-Adressen

Die technische Zeichenregel erlaubt keine Punkte. Dadurch werden `127.0.0.1`, `example.com` und `rezepte.local` bereits als ungueltige Zeichen abgelehnt. Wenn eine spezifischere Meldung gewuenscht ist, muss die Reihenfolge der Pruefungen Domains/IPs vor der allgemeinen Zeichenregel erkennen.

## Support-/Security-Muster

Beispiele wie `support_team`, `security_admin`, `admin-support` und `microsoftsupport` sprechen fuer Pattern-/Tokenpruefungen. Zu breite Substring-Regeln koennen legitime Namen blockieren. Eine moegliche Regel ist, sicherheitskritische Tokens am Anfang/Ende oder in Kombination mit Trennern zu sperren; `microsoftsupport` waere dann eine bewusst breit gesperrte Sonderregel oder faellt unter `support` als Suffix.

## Bestehende Daten

Die Anforderung schliesst automatische Umbenennung bestehender Benutzer aus. Falls die DB-MaxLength von 64 auf 20 reduziert wird, muss vorher geprueft werden, ob bestehende Daten laengere Namen enthalten koennen. Ohne Migration bleibt die fachliche Grenze trotzdem im Service erzwingbar.

## Case-Sensitivity

Reservierte Namen muessen case-insensitive verglichen werden. Die bestehende Eindeutigkeitspruefung ist exakt/case-sensitive. Eine Umstellung auf case-insensitive Eindeutigkeit koennte bestehende Nutzer betreffen und ist nicht explizit gefordert.

## Fehlertexte und UI

API-Fehler koennen zentral deutsch geliefert werden. Die Formularregistrierung leitet derzeit aber nur auf `/register?error=1` um und zeigt in `Register.razor` keinen ausdifferenzierten Fehler aus dem Service. Falls Akzeptanzkriterien streng auf sichtbare UI-Fehler fuer Registrierung zielen, braucht dieser Bereich eine UI-Anpassung.

