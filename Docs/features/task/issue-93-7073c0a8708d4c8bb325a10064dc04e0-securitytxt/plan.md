# Umsetzungsplan: security.txt (RFC 9116)

## Übersicht

Die Anwendung wird um eine `security.txt`-Auslieferung gemäß RFC 9116 erweitert. Administratoren konfigurieren die Direktiven über den bestehenden Einstellungsbereich; alle Felder werden als `AppSetting`-Einträge mit dem Schlüsselpräfix `SecurityTxt.*` persistiert. Vier öffentlich erreichbare Endpunkte liefern die Datei im Plain-Text-, Markdown- und HTML-Format aus.

---

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| `SecurityTxtSettings` (Aggregat) | Value Object / DTO (Record) | Das Objekt ist zustandslos und wird nur zur Übertragung zwischen Service, Controller und Renderer verwendet — kein Identitätsbedarf, kein eigenes Datenbankschema. |
| `ISecurityTxtRenderer` | Service-Layer-Interface mit drei Render-Methoden | Kapselt die Formatierungslogik vollständig von der HTTP-Schicht; erleichtert isolierte Unit-Tests ohne HTTP-Kontext. |
| Mehrfachwerte (`Contact`, `Acknowledgments`) | Ein Wert pro Zeile, gespeichert als einzelner `AppSetting`-String; Rendering splittet nach Zeilenumbrüchen | Einfachstes Vorgehen ohne Schemaänderung; RFC 9116 erlaubt mehrere Direktiven mit gleichem Namen — ein Wert pro Zeile wird beim Rendering in separate Zeilen (`Key: Wert1\nKey: Wert2`) aufgeteilt. |
| Validierung `Contact`/`Expires` | Servereitige Validierung im `PUT`-Endpunkt (HTTP 400 bei fehlendem Pflichtfeld, wenn `Enabled = true`) | RFC 9116 schreibt beide Felder als Pflichtfelder vor; reine UI-Validierung wäre nicht robust gegenüber direkten API-Aufrufen. |
| `Canonical`-Befüllung | Manuell durch den Administrator | Keine automatische Ableitung aus der Basis-URL, da keine globale Basis-URL-Konfiguration existiert; der Admin kennt die öffentliche URL seiner Instanz. |

---

## Programmabläufe

### 1. Öffentlicher Abruf von security.txt (Plain Text)

1. Client sendet `GET /security.txt` oder `GET /.well-known/security.txt`.
2. `RedirectToRegisterMiddleware.IsExcluded` gibt `true` zurück → kein Redirect auf `/login`.
3. `SecurityTxtController.GetSecurityTxt` wird aufgerufen.
4. Controller ruft `ISettingsService.GetSecurityTxtSettingsAsync` auf.
5. Ist `SecurityTxtSettings.Enabled == false` → Rückgabe `404 Not Found`.
6. Controller ruft `ISecurityTxtRenderer.RenderPlainText(settings)` auf.
7. Rückgabe als `ContentResult` mit `Content-Type: text/plain; charset=utf-8`, HTTP 200.

Beteiligte Klassen/Komponenten: `RedirectToRegisterMiddleware`, `SecurityTxtController`, `ISettingsService`, `SettingsService`, `ISecurityTxtRenderer`, `SecurityTxtRenderer`, `SecurityTxtSettings`

### 2. Öffentlicher Abruf von security.md / security.html

1. Client sendet `GET /.well-known/security.md` oder `GET /.well-known/security.html`.
2. `RedirectToRegisterMiddleware.IsExcluded` gibt `true` zurück.
3. `SecurityTxtController.GetSecurityMd` bzw. `GetSecurityHtml` wird aufgerufen.
4. Ablauf wie in Ablauf 1, Schritte 4–5.
5. Controller ruft `ISecurityTxtRenderer.RenderMarkdown(settings)` bzw. `RenderHtml(settings)` auf.
6. Rückgabe als `ContentResult` mit `Content-Type: text/markdown` bzw. `text/html`, HTTP 200.

Beteiligte Klassen/Komponenten: `SecurityTxtController`, `ISecurityTxtRenderer`, `SecurityTxtRenderer`, `SecurityTxtSettings`

### 3. Admin liest security.txt-Konfiguration

1. Authentifizierter Admin ruft `GET /api/settings/global/securitytxt` auf.
2. `SettingsController.GetGlobalSecurityTxt` prüft `[Authorize(Roles = "Admin")]`.
3. Controller ruft `ISettingsService.GetSecurityTxtSettingsAsync` auf.
4. `SettingsService` liest alle `AppSetting`-Einträge mit Präfix `SecurityTxt.*`.
5. Einträge werden in ein `SecurityTxtSettings`-Record gemappt und als JSON zurückgegeben.

Beteiligte Klassen/Komponenten: `SettingsController`, `ISettingsService`, `SettingsService`, `SecurityTxtSettings`, `AppSetting`

### 4. Admin schreibt security.txt-Konfiguration

1. Authentifizierter Admin sendet `PUT /api/settings/global/securitytxt` mit `SecurityTxtSettings`-JSON-Body.
2. `SettingsController.SetGlobalSecurityTxt` prüft `[Authorize(Roles = "Admin")]`.
3. Ist `Enabled == true` und `Contact` oder `Expires` fehlt → HTTP 400 zurückgeben.
4. Controller ruft `ISettingsService.SetSecurityTxtSettingsAsync(settings, ct)` auf.
5. `SettingsService` schreibt jeden Feldwert als eigenen `AppSetting`-Eintrag (`SecurityTxt.<Direktive>`); null-Werte werden gelöscht oder als Leerstring gespeichert.
6. HTTP 204 No Content.

Beteiligte Klassen/Komponenten: `SettingsController`, `ISettingsService`, `SettingsService`, `SecurityTxtSettings`, `AppSetting`

### 5. Admin konfiguriert security.txt im UI

1. Admin öffnet Einstellungen-Seite.
2. `SettingsViewModel` enthält ein `Item` mit `ComponentType = typeof(SecurityTxtSettings)` und `Visible = isAdmin`.
3. Admin klickt auf den Menüpunkt → `SecurityTxtSettings.razor` wird gerendert.
4. Komponente lädt aktuelle Einstellungen über `GET /api/settings/global/securitytxt`.
5. Admin füllt Formular aus und speichert → `PUT /api/settings/global/securitytxt`.

Beteiligte Klassen/Komponenten: `SettingsViewModel`, `SecurityTxtSettings.razor`, `SettingsController`

---

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `SecurityTxtSettings` | Record (DTO / Value Object) | Aggregiert alle security.txt-Direktiven: `bool Enabled`, `string? Contact`, `DateTimeOffset? Expires`, `string? Encryption`, `string? Acknowledgments`, `string? PreferredLanguages`, `string? Canonical`, `string? Policy`, `string? Hiring` |
| `ISecurityTxtRenderer` | Interface | Definiert `RenderPlainText`, `RenderMarkdown`, `RenderHtml` mit `SecurityTxtSettings` als Parameter |
| `SecurityTxtRenderer` | Klasse | Implementiert `ISecurityTxtRenderer`; Plain-Text nach RFC 9116, Markdown mit `## Key`-Überschriften, HTML mit `<h2>`/`<p>` |
| `SecurityTxtController` | Controller (`ControllerBase`, kein `[Authorize]`) | Bedient die vier öffentlichen Endpunkte; delegiert Rendering an `ISecurityTxtRenderer` |
| `SecurityTxtSettings.razor` | Blazor-Komponente | Admin-Formular unter `Components/Settings/`; lädt und speichert über die Admin-API |
| `SecurityTxtRendererTests` | Testklasse | Unit-Tests für alle drei Render-Methoden |
| `SecurityTxtControllerTests` | Testklasse (Integrationstests) | Tests für alle vier öffentlichen Endpunkte |

---

## Änderungen an bestehenden Klassen

### `ISettingsService` (Interface)

- **Neue Methoden:**
  - `GetSecurityTxtSettingsAsync(CancellationToken ct)` — liest alle `SecurityTxt.*`-Einträge und gibt ein `SecurityTxtSettings`-Record zurück
  - `SetSecurityTxtSettingsAsync(SecurityTxtSettings settings, CancellationToken ct)` — schreibt alle Felder als `AppSetting`-Einträge

### `SettingsService` (Klasse)

- **Neue Methoden:** Implementierung von `GetSecurityTxtSettingsAsync` und `SetSecurityTxtSettingsAsync` analog zu den bestehenden `GetGlobal*`/`SetGlobal*`-Methoden
- **Neue Konstanten:** Schlüsselkonstanten `SecurityTxtEnabledKey`, `SecurityTxtContactKey`, `SecurityTxtExpiresKey`, `SecurityTxtEncryptionKey`, `SecurityTxtAcknowledgmentsKey`, `SecurityTxtPreferredLanguagesKey`, `SecurityTxtCanonicalKey`, `SecurityTxtPolicyKey`, `SecurityTxtHiringKey`

### `SettingsController` (Klasse)

- **Neue Methoden:**
  - `GetGlobalSecurityTxt` (`GET global/securitytxt`, `[Authorize(Roles = "Admin")]`) — ruft `GetSecurityTxtSettingsAsync` auf, gibt `SecurityTxtSettings` als JSON zurück
  - `SetGlobalSecurityTxt` (`PUT global/securitytxt`, `[Authorize(Roles = "Admin")]`) — validiert Pflichtfelder, ruft `SetSecurityTxtSettingsAsync` auf, gibt 204 zurück

### `SettingsViewModel` (Klasse)

- **Geänderte Methoden:** Konstruktor erhält ein neues `Item` für `SecurityTxtSettings.razor` mit `isAdmin`-Sichtbarkeit, analog zu `"Benutzer"` und `"Plugins"`

### `RedirectToRegisterMiddleware` (Klasse)

- **Geänderte Methoden:** `IsExcluded` — die Pfade `/security.txt`, `/.well-known/security.txt`, `/.well-known/security.md` und `/.well-known/security.html` werden in die Ausnahmeliste aufgenommen

---

## Datenbankmigrationen

Keine. Das bestehende Key-Value-Schema der `AppSetting`-Tabelle wird ohne Schemaänderung genutzt.

---

## Validierungsregeln

| Feld / Objekt | Regel | Fehlerfall |
|---------------|-------|------------|
| `SecurityTxtSettings.Contact` | Pflichtfeld, wenn `Enabled == true` | HTTP 400 mit Fehlermeldung |
| `SecurityTxtSettings.Expires` | Pflichtfeld, wenn `Enabled == true`; muss in der Zukunft liegen | HTTP 400 mit Fehlermeldung |

---

## Konfigurationsänderungen

| Eintrag | Typ | Standardwert | Zweck |
|---------|-----|--------------|-------|
| `SecurityTxt.Enabled` | `bool` (als `"True"`/`"False"` in `AppSetting`) | `false` | Schaltet die security.txt-Funktion ein oder aus |
| `SecurityTxt.Contact` | `string?` | — | RFC-9116-Direktive `Contact` (ein URI oder E-Mail pro Zeile) |
| `SecurityTxt.Expires` | `string?` (ISO-8601) | — | RFC-9116-Direktive `Expires` |
| `SecurityTxt.Encryption` | `string?` | — | RFC-9116-Direktive `Encryption` |
| `SecurityTxt.Acknowledgments` | `string?` | — | RFC-9116-Direktive `Acknowledgments` |
| `SecurityTxt.PreferredLanguages` | `string?` | — | RFC-9116-Direktive `Preferred-Languages` |
| `SecurityTxt.Canonical` | `string?` | — | RFC-9116-Direktive `Canonical` |
| `SecurityTxt.Policy` | `string?` | — | RFC-9116-Direktive `Policy` |
| `SecurityTxt.Hiring` | `string?` | — | RFC-9116-Direktive `Hiring` |

---

## Seiteneffekte und Risiken

- **`RedirectToRegisterMiddleware`:** Ohne die Anpassung von `IsExcluded` werden alle Anfragen an `/security.txt` und `/.well-known/*` für nicht-authentifizierte Nutzer auf `/login` umgeleitet. Das ist der kritischste Schritt für die öffentliche Erreichbarkeit.
- **`/.well-known/`-Routing:** ASP.NET Core's `UseStaticFiles` kann Anfragen an `/.well-known/` abfangen, bevor `MapControllers` greift. Die Reihenfolge in `Program.cs` muss sichergestellt werden (`MapControllers` muss vor oder unabhängig von `UseStaticFiles` für diese Pfade aktiv sein). Alternativ kann das `StaticFileOptions`-Serving für `/.well-known/` explizit eingeschränkt werden.
- **`SettingsController`-Basisautorisierung:** Der Controller hat `[Authorize]` auf Controllerebene. Die neuen Admin-Endpunkte erben diese Autorisierung korrekt; kein Seiteneffekt erwartet.

---

## Umsetzungsreihenfolge

1. **`SecurityTxtSettings`-Record anlegen**
   - Voraussetzungen: Keine
   - Beschreibung: Neues Record `SecurityTxtSettings` mit allen Pflicht- und optionalen Feldern gemäß RFC 9116 im passenden Namespace anlegen (z. B. `Rezepte.Web/Models` oder neben den bestehenden DTOs)

2. **`ISettingsService` erweitern**
   - Voraussetzungen: `SecurityTxtSettings`-Record vorhanden
   - Beschreibung: Interface um `GetSecurityTxtSettingsAsync` und `SetSecurityTxtSettingsAsync` ergänzen

3. **`SettingsService` erweitern**
   - Voraussetzungen: `SecurityTxtSettings`-Record und erweitertes `ISettingsService` vorhanden
   - Beschreibung: Schlüsselkonstanten und Implementierungen für die beiden neuen Methoden hinzufügen; Mehrfachwerte werden als newline-separierter String gespeichert

4. **`RedirectToRegisterMiddleware.IsExcluded` anpassen**
   - Voraussetzungen: Keine
   - Beschreibung: Die vier Pfade `/security.txt`, `/.well-known/security.txt`, `/.well-known/security.md`, `/.well-known/security.html` als Ausnahmen eintragen

5. **Routing in `Program.cs` prüfen und ggf. anpassen**
   - Voraussetzungen: Keine
   - Beschreibung: Sicherstellen, dass `MapControllers` Anfragen an `/security.txt` und `/.well-known/*` vor `UseStaticFiles` verarbeiten kann; ggf. Reihenfolge oder `StaticFileOptions` anpassen

6. **`ISecurityTxtRenderer` anlegen**
   - Voraussetzungen: `SecurityTxtSettings`-Record vorhanden
   - Beschreibung: Interface mit den drei Methoden `RenderPlainText`, `RenderMarkdown`, `RenderHtml` anlegen

7. **`SecurityTxtRenderer` implementieren**
   - Voraussetzungen: `ISecurityTxtRenderer` und `SecurityTxtSettings` vorhanden
   - Beschreibung: Plain-Text-Rendering nach RFC 9116 (Mehrfachwerte als separate `Key:`-Zeilen); Markdown mit `## Key`-Abschnitten; HTML mit `<h2>`/`<p>`; `ISecurityTxtRenderer` in DI registrieren

8. **`SecurityTxtController` anlegen**
   - Voraussetzungen: `ISecurityTxtRenderer`, `ISettingsService` (erweitert), `SecurityTxtSettings` vorhanden; Routing und Middleware angepasst
   - Beschreibung: Vier Endpunkte implementieren; kein `[Authorize]`; bei `Enabled == false` → 404; korrekte `Content-Type`-Header setzen

9. **`SettingsController` erweitern**
   - Voraussetzungen: `SecurityTxtSettings`-Record und erweitertes `ISettingsService` vorhanden
   - Beschreibung: `GetGlobalSecurityTxt` und `SetGlobalSecurityTxt` hinzufügen; Pflichtfeldvalidierung im `PUT`-Endpunkt

10. **`SettingsViewModel` erweitern**
    - Voraussetzungen: `SecurityTxtSettings.razor` noch nicht vorhanden — Schritt kann parallel zu Schritt 11 erfolgen; `Item`-Eintrag zeigt auf `typeof(SecurityTxtSettings)` — Datei muss kompilierbar sein
    - Beschreibung: Neues `Item` mit Titel, Icon, `typeof(SecurityTxtSettings)` und `isAdmin`-Sichtbarkeit in den Konstruktor eintragen

11. **`SecurityTxtSettings.razor` anlegen**
    - Voraussetzungen: Admin-API-Endpunkte (`GET`/`PUT /api/settings/global/securitytxt`) vorhanden
    - Beschreibung: Blazor-Formularkomponente unter `Components/Settings/` anlegen; alle neun Felder als Eingabefelder; Laden beim Init, Speichern über PUT

12. **`SecurityTxtRendererTests` anlegen**
    - Voraussetzungen: `SecurityTxtRenderer` vorhanden
    - Beschreibung: Unit-Tests für alle drei Render-Methoden, `Enabled = false`-Verhalten, Pflichtfeld-Ausgabe, Mehrfachwert-Splitting

13. **`SecurityTxtControllerTests` anlegen**
    - Voraussetzungen: `SecurityTxtController` vollständig implementiert; Testinfrastruktur (bestehende `WebApplicationFactory` oder ähnliches Setup) vorhanden
    - Beschreibung: Integrationstests für alle vier Endpunkte: korrekter `Content-Type`, HTTP 200 bei aktivierter Konfiguration, HTTP 404 bei `Enabled = false`, kein Auth-Header erforderlich

---

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `RenderPlainText_ShouldReturnRfc9116Format_WhenAllFieldsSet` | `SecurityTxtRendererTests` | Alle Direktiven erscheinen korrekt als `Key: Value`-Zeilen |
| `RenderPlainText_ShouldRepeatDirective_ForMultilineContact` | `SecurityTxtRendererTests` | Mehrzeilige `Contact`-Werte erzeugen mehrere `Contact:`-Zeilen |
| `RenderMarkdown_ShouldReturnSectionHeaders` | `SecurityTxtRendererTests` | Jede Direktive erscheint als `## Key`-Abschnitt |
| `RenderHtml_ShouldReturnH2AndParagraph` | `SecurityTxtRendererTests` | Jede Direktive erscheint als `<h2>`/`<p>` |
| `RenderPlainText_ShouldOmitEmptyFields` | `SecurityTxtRendererTests` | Optionale null-Felder erscheinen nicht in der Ausgabe |
| `GetSecurityTxt_ReturnsOk_WhenEnabled` | `SecurityTxtControllerTests` | HTTP 200, `Content-Type: text/plain` für `/security.txt` |
| `GetSecurityTxt_ReturnsNotFound_WhenDisabled` | `SecurityTxtControllerTests` | HTTP 404 bei `Enabled = false` |
| `GetWellKnownSecurityTxt_ReturnsOk_WhenEnabled` | `SecurityTxtControllerTests` | HTTP 200 für `/.well-known/security.txt` |
| `GetSecurityMd_ReturnsOk_WithMarkdownContentType` | `SecurityTxtControllerTests` | HTTP 200, `Content-Type: text/markdown` für `/.well-known/security.md` |
| `GetSecurityHtml_ReturnsOk_WithHtmlContentType` | `SecurityTxtControllerTests` | HTTP 200, `Content-Type: text/html` für `/.well-known/security.html` |
| `GetSecurityTxt_RequiresNoAuthentication` | `SecurityTxtControllerTests` | Zugriff ohne Auth-Header liefert keine 401/302-Antwort |
| `GetSecurityTxtSettingsAsync_ShouldReturnDefaults_WhenNoSettingsExist` | `SettingsServiceTests` (Erweiterung) | Default-Rückgabe (`Enabled = false`, alle nullable Felder `null`) |
| `SetSecurityTxtSettingsAsync_ShouldPersistAllFields` | `SettingsServiceTests` (Erweiterung) | Alle neun Felder werden korrekt in `AppSetting`-Einträge geschrieben und zurückgelesen |

### Betroffene bestehende Tests

Keine. Die Änderungen an `ISettingsService` sind rein additiv (neue Methoden); bestehende Tests werden nicht berührt.

### E2E-Tests (Pflicht)

| Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium |
|----------|------------------------|-------------------------------|
| security.txt öffentlich abrufbar (aktiviert) | `Rezepte.Tests.Browser` / neues Szenario | `GET /security.txt` liefert HTTP 200 mit RFC-9116-Inhalt ohne Authentifizierung |
| security.txt liefert 404 (deaktiviert) | `Rezepte.Tests.Browser` / neues Szenario | `GET /security.txt` liefert HTTP 404, wenn `Enabled = false` |
| Admin konfiguriert security.txt im UI | `Rezepte.Tests.Browser` / neues Szenario | Admin öffnet Einstellungen, sieht security.txt-Menüpunkt, speichert Konfiguration, Inhalte erscheinen im öffentlichen Abruf |
| Normalbenutzerin sieht keinen security.txt-Menüpunkt | `Rezepte.Tests.Browser` / neues Szenario | Benutzer ohne Admin-Rolle sieht den Einstellungs-Menüpunkt für security.txt nicht |

Welche bestehenden E2E-Tests müssen angepasst werden? Keine.

---

## Offene Punkte

Keine. Alle offenen Fragen aus der Anforderung wurden durch die Bestandsaufnahme und die gewählten Designentscheidungen geklärt:

- **`Expires`-Pflichtfeld:** Serverseitige Validierung im `PUT`-Endpunkt (→ Designentscheidungen).
- **Canonical-URL:** Manuelle Eingabe durch den Administrator (→ Designentscheidungen).
- **Mehrfachwerte:** Ein Wert pro Zeile im Textfeld, Splitting beim Rendering (→ Designentscheidungen).
- **PGP-Signierung:** Nicht im Scope dieser Anforderung; kein Hinweis auf vorhandene PGP-Infrastruktur im Projekt.
- **`/.well-known/`-Routing:** Explizit als Seiteneffekt/Risiko in der Umsetzungsreihenfolge adressiert (Schritt 5).
