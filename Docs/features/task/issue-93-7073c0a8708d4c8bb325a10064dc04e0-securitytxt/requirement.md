# Anforderung: security.txt

## Fachliche Zusammenfassung

Die Anwendung soll eine `security.txt`-Datei gemäß RFC 9116 unter den Pfaden `/security.txt` und `/.well-known/security.txt` ausliefern. Die darin enthaltenen Direktiven (`Contact`, `Expires`, `Encryption`, `Acknowledgments`, `Preferred-Languages`, `Canonical`, `Policy`, `Hiring`) werden durch Administratoren über den bestehenden Einstellungsbereich konfiguriert. Normalbenutzern bleibt dieser Konfigurationsbereich verborgen. Zusätzlich werden dieselben Inhalte unter `/.well-known/security.md` (Markdown) und `/.well-known/security.html` (HTML) mit abschnittsweiser Überschriften-Formatierung angeboten. Alle drei Endpunkte sind ohne Authentifizierung erreichbar.

---

## Betroffene Klassen und Komponenten

### Datenmodell

| Artefakt | Art | Beschreibung |
|---|---|---|
| `AppSetting` | Bestehende Entity (erweitern via neue Keys) | Persistenz der security.txt-Felder als Key-Value-Paare (z. B. `SecurityTxt.Enabled`, `SecurityTxt.Contact`, `SecurityTxt.Expires`, `SecurityTxt.Encryption`, `SecurityTxt.Acknowledgments`, `SecurityTxt.PreferredLanguages`, `SecurityTxt.Canonical`, `SecurityTxt.Policy`, `SecurityTxt.Hiring`) |

### Services

| Artefakt | Art | Beschreibung |
|---|---|---|
| `ISettingsService` | Bestehendes Interface (erweitern) | Neue Methoden `GetSecurityTxtSettingsAsync` / `SetSecurityTxtSettingsAsync` für Lese-/Schreibzugriff auf alle security.txt-Direktiven |
| `SettingsService` | Bestehende Implementierung (erweitern) | Implementierung der neuen Interface-Methoden; Lese-/Schreibzugriff auf `AppSetting`-Tabelle |
| `SecurityTxtSettings` | Neues DTO/Record | Aggregiert alle Felder einer security.txt-Konfiguration: `bool Enabled`, `string? Contact`, `DateTimeOffset? Expires`, `string? Encryption`, `string? Acknowledgments`, `string? PreferredLanguages`, `string? Canonical`, `string? Policy`, `string? Hiring` |

### Controller / Endpunkte

| Artefakt | Art | Beschreibung |
|---|---|---|
| `SecurityTxtController` | Neue Klasse (`ControllerBase`, kein `[Authorize]`) | Bedient `GET /security.txt`, `GET /.well-known/security.txt`, `GET /.well-known/security.md`, `GET /.well-known/security.html` ohne Authentifizierung; delegiert Rendering an `ISecurityTxtRenderer` |
| `SettingsController` | Bestehende Klasse (erweitern) | Neue Admin-Endpunkte `GET /api/settings/global/securitytxt` und `PUT /api/settings/global/securitytxt` (Role `Admin`) |

### Rendering

| Artefakt | Art | Beschreibung |
|---|---|---|
| `ISecurityTxtRenderer` | Neues Interface | Definiert Methoden `RenderPlainText`, `RenderMarkdown`, `RenderHtml` jeweils mit `SecurityTxtSettings` als Parameter |
| `SecurityTxtRenderer` | Neue Klasse | Implementiert `ISecurityTxtRenderer`; gibt für `RenderPlainText` das RFC-9116-Format (`Key: Value`) zurück; für `RenderMarkdown` Überschriften als `## Key` gefolgt vom Wert; für `RenderHtml` `<h2>Key</h2><p>Value</p>` |

### UI

| Artefakt | Art | Beschreibung |
|---|---|---|
| `SecurityTxtSettings.razor` | Neue Blazor-Komponente unter `Components/Settings/` | Formular zur Bearbeitung aller security.txt-Direktiven; nur für Admins sichtbar |
| `SettingsViewModel` | Bestehende Klasse (erweitern) | Neues `Item` mit `isAdmin`-Sichtbarkeit und `typeof(SecurityTxtSettings)` als `ComponentType` |

### Tests

| Artefakt | Art | Beschreibung |
|---|---|---|
| `SecurityTxtRendererTests` | Neue Testklasse | Unit-Tests für `ISecurityTxtRenderer`: korrekte Ausgabe aller drei Formate, Verhalten bei deaktivierter Funktion (`Enabled = false`), Pflichtfelder (`Contact`, `Expires`) |
| `SecurityTxtControllerTests` | Neue Testklasse | Integrationstests für alle vier Endpunkte: korrekter Content-Type, HTTP 200 bei aktiver Konfiguration, HTTP 404 bei `Enabled = false`, kein Auth-Header erforderlich |

---

## Implementierungsansatz

1. **Konfigurationspersistenz:** Die security.txt-Felder werden als einzelne `AppSetting`-Einträge gespeichert (Schlüsselschema `SecurityTxt.<Direktive>`). Kein neues Datenbankschema nötig — das bestehende Key-Value-Modell von `AppSetting` wird wiederverwendet. Eine EF-Migration ist **nicht erforderlich**, da keine neue Tabelle/Spalte entsteht.

2. **Service-Erweiterung:** `ISettingsService` und `SettingsService` erhalten neue Methoden, die das neue DTO `SecurityTxtSettings` lesen und schreiben. Das Muster orientiert sich an den bestehenden `GetGlobalAiEnabledAsync`/`SetGlobalAiEnabledAsync`-Methoden.

3. **Rendering:** `ISecurityTxtRenderer` kapselt die Formatlogik vollständig von der HTTP-Schicht. Das Interface ermöglicht einfaches Testen und späteres Austauschen der Rendering-Logik.

4. **Controller ohne Authentifizierung:** `SecurityTxtController` hat kein `[Authorize]`-Attribut. Die Middleware `RedirectToRegisterMiddleware` muss die neuen Pfade (`/security.txt`, `/.well-known/security.txt`, `/.well-known/security.md`, `/.well-known/security.html`) in die Liste der ausgenommenen Pfade aufnehmen, damit der Zugriff ohne Login möglich ist.

5. **Admin-API:** Neue Endpunkte in `SettingsController` (`GET`/`PUT /api/settings/global/securitytxt`) übertragen das DTO `SecurityTxtSettings` als JSON. Der `PUT`-Endpunkt ist mit `[Authorize(Roles = "Admin")]` geschützt.

6. **UI-Integration:** Die neue Komponente `SecurityTxtSettings.razor` wird als neues `Item` mit `isAdmin`-Sichtbarkeit in `SettingsViewModel` registriert — analog zu `UserAdmin` und `PluginSettings`.

7. **Deaktivierungslogik:** Ist `SecurityTxt.Enabled` auf `false` gesetzt, antworten alle vier Endpunkte mit HTTP 404. So kann die Funktion ohne Code-Änderung ab- und eingeschaltet werden.

---

## Konfiguration

- **Ebene:** Anwendungsweit (global), Admin-only
- **Persistenz:** `AppSetting`-Tabelle (bestehend), Key-Präfix `SecurityTxt.*`
- **Sichtbarkeit im UI:** Nur für Benutzer mit der Rolle `Admin` (`isAdmin`-Flag in `SettingsViewModel`)

---

## Offene Fragen

1. **`Expires`-Pflichtfeld:** RFC 9116 schreibt `Contact` und `Expires` als Pflichtfelder vor. Soll das Backend eine Validierung erzwingen (HTTP 400 beim Speichern ohne diese Felder) oder soll die Validierung nur im UI erfolgen?

2. **Canonical-URL:** Der Wert von `Canonical` soll laut RFC auf die eigene Instanz zeigen. Soll er automatisch aus der konfigurierten Basis-URL der Anwendung befüllt werden, oder wird er manuell eingetragen?

3. **Mehrfachwerte:** RFC 9116 erlaubt mehrere `Contact`- und `Acknowledgments`-Einträge. Soll die UI mehrere Einträge je Direktive unterstützen (z. B. als mehrzeiliges Textfeld, ein Wert pro Zeile)?

4. **PGP-Signierung:** RFC 9116 empfiehlt eine PGP-Signatur für die Datei. Ist eine Signierung in diesem Projektkontext gewünscht oder ausdrücklich nicht?

5. **`/.well-known/`-Routing:** ASP.NET Core behandelt Pfade unter `/.well-known/` ggf. als statische Dateien. Muss das Routing in `Program.cs` explizit konfiguriert werden (z. B. Reihenfolge `MapControllers` vor `UseStaticFiles`)?
