# Strukturierte Anforderung: Gesperrte Benutzernamen

## Ziel

Bei der Registrierung und beim administrativen Anlegen oder Bearbeiten von Benutzern sollen unzulässige Benutzernamen abgelehnt werden. Dadurch sollen Kollisionen mit Systemfunktionen, Sicherheitsrisiken, Täuschung, Verwechslung und missbräuchliche Namen verhindert werden.

## Ausgangslage

Die Anwendung besitzt bereits Registrierung, Login, Benutzerverwaltung und Admin-Funktionen. Benutzernamen werden bei der Benutzeranlage verwendet und müssen künftig zentral nach einheitlichen Regeln validiert werden.

## Funktionale Anforderungen

### FR-001: Zentrale Validierung von Benutzernamen

Die Anwendung muss eine zentrale serverseitige Validierung für Benutzernamen bereitstellen, die von allen Wegen zur Benutzeranlage oder Änderung genutzt wird.

Betroffene Wege:

- Registrierung neuer Benutzer
- Anlegen von Benutzern durch Administratoren
- Bearbeiten von Benutzernamen durch Administratoren, sofern diese Funktion vorhanden ist
- API-Endpunkte, die Benutzernamen entgegennehmen

### FR-002: Technische Zeichen- und Längenregeln

Benutzernamen müssen folgende technische Regeln erfüllen:

- Mindestlänge: 3 Zeichen
- Maximallänge: 20 Zeichen
- Erlaubte Zeichen: Buchstaben, Ziffern, Unterstrich und Bindestrich
- Nicht erlaubt: Leerzeichen, Emojis und sonstige Sonderzeichen wie `/`, `@`, `#`, `%`
- Leere, nur aus Leerraum bestehende oder fehlende Werte sind ungültig

### FR-003: Verbotene reservierte Namen

Benutzernamen dürfen nicht exakt einem reservierten oder sicherheitskritischen Namen entsprechen. Der Vergleich muss unabhängig von Groß- und Kleinschreibung erfolgen.

Mindestens zu sperrende Namen:

- `admin`
- `administrator`
- `root`
- `system`
- `support`
- `guest`
- `test`
- `null`
- `moderator`
- `superuser`
- `owner`
- `help`
- `contact`
- `info`
- `about`
- `login`
- `signup`
- `me`
- `you`
- `self`
- `someone`
- `anyone`
- `webmaster`
- `security`

### FR-004: App- und domainbezogene Namen sperren

Benutzernamen dürfen nicht mit Namen kollidieren, die wie offizielle App-, Betreiber- oder Domain-Konten wirken.

Mindestens zu sperren:

- `rezepte`
- `rezepteapp`
- `rezepte-admin`
- `rezepte_support`
- `webmaster`

Die Liste soll erweiterbar sein, damit projektspezifische App-, Domain-, Rollen- oder API-Namen ergänzt werden können.

### FR-005: IP-Adressen und Domains als Benutzernamen ablehnen

Benutzernamen, die wie IP-Adressen oder Domains aussehen, müssen abgelehnt werden.

Beispiele für ungültige Werte:

- `127.0.0.1`
- `192.168.1.1`
- `example.com`
- `rezepte.local`

### FR-006: Offiziell wirkende Support- und Sicherheitsnamen ablehnen

Benutzernamen, die wie offizielle Support-, Admin- oder Sicherheitskonten wirken, müssen abgelehnt werden.

Beispiele:

- `support_team`
- `security_admin`
- `admin-support`
- `microsoftsupport`

### FR-007: Beleidigende oder missbräuchliche Inhalte verhindern

Die Validierung soll eine erweiterbare Sperrliste für beleidigende, diskriminierende, gewaltverherrlichende oder anderweitig missbräuchliche Begriffe unterstützen.

Die konkrete Wortliste kann als initiale, wartbare Liste umgesetzt werden und muss später ohne größere Codeänderungen erweitert werden können.

### FR-008: Ähnlichkeit zu verbotenen Namen prüfen

Die Anwendung soll Benutzernamen ablehnen, die verbotenen Namen sehr ähnlich sind und offensichtlich zur Umgehung der Sperrliste dienen.

Beispiele:

- `adm1n` statt `admin`
- `r00t` statt `root`
- `supp0rt` statt `support`

Die Ähnlichkeitsprüfung muss so umgesetzt werden, dass normale, legitime Benutzernamen nicht unverhältnismäßig oft abgelehnt werden. Die konkrete Schwelle ist im Rahmen der Umsetzung fachlich und technisch zu prüfen.

### FR-009: Bestehende Eindeutigkeitsprüfung beibehalten

Die neue Validierung darf bestehende Regeln zur Eindeutigkeit von Benutzernamen nicht ersetzen. Ein Benutzername muss weiterhin abgelehnt werden, wenn er bereits vergeben ist.

### FR-010: Verständliche Fehlermeldungen

Wird ein Benutzername abgelehnt, muss die Anwendung dem Benutzer eine klare, deutschsprachige Fehlermeldung anzeigen.

Die Fehlermeldung soll den Grund nachvollziehbar machen, ohne unnötig sensible Sperrlisten- oder Sicherheitsdetails offenzulegen.

Beispiele:

- `Der Benutzername ist reserviert.`
- `Der Benutzername darf nur Buchstaben, Zahlen, Unterstrich und Bindestrich enthalten.`
- `Der Benutzername muss zwischen 3 und 20 Zeichen lang sein.`
- `Dieser Benutzername kann nicht verwendet werden. Bitte wählen Sie einen anderen Namen.`

## Nicht-funktionale Anforderungen

### NFR-001: Serverseitige Durchsetzung

Die Validierung muss serverseitig erfolgen. Eine zusätzliche clientseitige Validierung ist optional, darf aber die serverseitige Prüfung nicht ersetzen.

### NFR-002: Wiederverwendbarkeit

Die Validierungslogik soll an einer zentralen Stelle gekapselt werden, damit Registrierung, Admin-UI und API dieselben Regeln verwenden.

### NFR-003: Testbarkeit

Die Validierung muss mit automatisierten Tests abgedeckt werden.

Mindestens zu testen:

- gültige Benutzernamen
- zu kurze und zu lange Benutzernamen
- ungültige Zeichen
- Groß-/Kleinschreibung bei gesperrten Namen
- reservierte Namen
- App- und Support-Namen
- IP-Adress- und Domainmuster
- ähnliche Schreibweisen verbotener Namen
- bestehende Benutzername-ist-bereits-vergeben-Prüfung bleibt wirksam

### NFR-004: Wartbarkeit der Sperrliste

Die Liste verbotener Namen und Muster soll nachvollziehbar gepflegt werden können. Neue Einträge sollen ohne Änderungen an mehreren Stellen ergänzt werden können.

## Akzeptanzkriterien

- Registrierung mit `admin`, `Admin`, `ADMIN`, `root`, `support`, `guest`, `test` oder `null` wird abgelehnt.
- Registrierung mit `administrator`, `moderator`, `superuser`, `owner`, `help`, `contact`, `info`, `about`, `login`, `signup`, `me`, `you`, `self`, `someone` oder `anyone` wird abgelehnt.
- Registrierung mit `rezepte`, `rezepteapp`, `webmaster`, `support_team` oder `security_admin` wird abgelehnt.
- Registrierung mit einem Namen unter 3 Zeichen wird abgelehnt.
- Registrierung mit einem Namen über 20 Zeichen wird abgelehnt.
- Registrierung mit Leerzeichen, Emojis oder Sonderzeichen außerhalb von Buchstaben, Ziffern, `_` und `-` wird abgelehnt.
- Registrierung mit IP-Adressen oder Domain-ähnlichen Eingaben wird abgelehnt.
- Registrierung mit offensichtlich ähnlichen Umgehungsschreibweisen gesperrter Namen wird abgelehnt, soweit die implementierte Ähnlichkeitsprüfung dies zuverlässig erkennt.
- Registrierung mit einem normalen gültigen Namen wie `max_mustermann`, `anna-2026` oder `kochbuchFan1` ist möglich, sofern der Name noch nicht vergeben ist.
- Admin-Benutzeranlage nutzt dieselben Validierungsregeln wie die öffentliche Registrierung.
- API-Antworten und UI-Fehlermeldungen sind deutschsprachig und verständlich.
- Automatisierte Tests decken die zentralen Validierungsregeln ab.

## Abgrenzungen

- Eine vollständige Erkennung aller Marken-, Prominenten- oder Phishing-Varianten ist nicht erforderlich.
- Eine redaktionell vollständige Liste beleidigender Begriffe ist nicht Bestandteil dieser Anforderung; erforderlich ist eine erweiterbare technische Grundlage.
- Bestehende Benutzer mit künftig gesperrten Namen müssen nicht automatisch umbenannt oder deaktiviert werden, sofern dies nicht bereits durch bestehende Datenmigrationen vorgesehen ist.
- Eine clientseitige Live-Prüfung während der Eingabe ist optional.
