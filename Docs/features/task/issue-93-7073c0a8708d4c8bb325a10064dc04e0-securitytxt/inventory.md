# Bestandsaufnahme: security.txt

Analysiert wurde der Bereich Einstellungs-Persistenz, Settings-Service, Settings-Controller, UI-Integration und Middleware, bezogen auf die Anforderung zur Auslieferung und Administration einer `security.txt` gemäß RFC 9116.

---

## Zusammenfassung

- **`AppSetting`** existiert als einfache Key-Value-Entity; das bestehende Muster (`Key`/`Value` als Strings) ist direkt für `SecurityTxt.*`-Schlüssel nutzbar — keine Schemaänderung erforderlich.
- **`ISettingsService`** und **`SettingsService`** folgen einem klar erkennbaren Muster (je ein `Get`/`Set`-Methodenpaar pro Einstellungsschlüssel); beide müssen um `GetSecurityTxtSettingsAsync` / `SetSecurityTxtSettingsAsync` erweitert werden.
- **`SettingsController`** besitzt bereits Admin-geschützte `GET`/`PUT`-Endpunkte (z. B. `global/ai`); die neuen Endpunkte `global/securitytxt` fügen sich nahtlos in das vorhandene Muster ein.
- **`SettingsViewModel`** listet `Item`-Einträge mit `isAdmin`-Flag; ein neues `Item` für `SecurityTxtSettings.razor` kann analog zu `PluginSettings` eingetragen werden.
- **`RedirectToRegisterMiddleware`** enthält eine `IsExcluded`-Methode; `/security.txt` und `/.well-known/*` sind dort **noch nicht** ausgenommen — muss ergänzt werden.
- **`SecurityTxtController`**, **`ISecurityTxtRenderer`**, **`SecurityTxtRenderer`**, **`SecurityTxtSettings` (DTO)** und **`SecurityTxtSettings.razor`** existieren noch **nicht**.
- Es gibt **keine bestehenden Tests** für security.txt-Rendering oder -Controller.

---

## Details

- [Datenmodell](inventory/models.md)
- [Logik](inventory/logic.md)
- [Interfaces](inventory/interfaces.md)
- [Tests](inventory/tests.md)
