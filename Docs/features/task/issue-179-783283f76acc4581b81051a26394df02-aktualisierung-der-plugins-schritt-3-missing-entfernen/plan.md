# Umsetzungsplan: Vermisste Plugins aus der Verwaltung entfernen (Schritt 3, Issue #179)

## Übersicht

`IPluginSettingsService` und `PluginSettingsService` erhalten eine neue Methode `RemovePluginAsync(string pluginId, CancellationToken)`, die einen `PluginSetting`-Verwaltungseintrag ausschließlich im Status `PluginStatus.Missing` löscht — administratorenvorbehalten (`EnsureAdmin`), ohne Dateisystemoperationen. Die Wiederentdeckung eines entfernten Plugins ist bereits durch den Neu-Erfassungspfad in `PluginManager.SynchronizeSettingsAsync` abgedeckt und benötigt keine Produktivänderung.

## Scope-Klärung

| Feld | Festlegung |
|------|------------|
| Verarbeitungseinheit | Ein einzelner `PluginSetting`-Verwaltungseintrag mit `Status == PluginStatus.Missing` |
| Auslösende Ereignisse | Aufruf von `RemovePluginAsync(pluginId)` — die auslösende UI-Aktion ist Gegenstand von Schritt 5; dieser Schritt liefert den Service-Vertrag |
| Erwartete Wirkung | Eintrag verschwindet aus `GetPluginsAsync` und damit aus der Pluginverwaltung; bei späterer Wiederentdeckung durch `PluginManager.SynchronizeSettingsAsync` entsteht ein neuer Datensatz mit frischem `DiscoveredAt`, `OrderIndex` am Ende und `Enabled` gemäß Neu-Plugin-Logik |
| Umgang mit fehlenden Objekten | `PluginSetting` zur `pluginId` nicht vorhanden → Fehlerfall `InvalidOperationException` („not found", Muster `SetEnabledAsync`); `PluginSetting` vorhanden, aber `Status != Missing` → verwerfen, `InvalidOperationException`; Plugindateien im Dateisystem → nichts zu tun (per Definition nicht mehr im Discovery-Pfad) |

### Scope-Ausschlüsse

| # | Einschränkung / Ausschluss | Begründung | Status |
|---|----------------------------|------------|--------|
| 1 | Keine UI-Anbindung (Button, Bestätigungsdialog, Anzeige) | Folgt in Schritt 5 | bestätigt (requirement.md) |
| 2 | Keine Dateisystem-Löschung | Pluginverzeichnisse/Löschmarkierungen sind Schritt-1-Thema; `Missing`-Plugins sind ohnehin nicht mehr auffindbar | bestätigt (requirement.md) |
| 3 | Keine Entfernen-Funktion für andere Status (`Loaded`, `Incompatible`, `LoadFailed`, `RuntimeFailed`) | Nicht angefordert; Guard lehnt sie ab | bestätigt (requirement.md) |
| 4 | Kein Entfernen von `PluginSource`-Einträgen | Dafür existiert `DeleteSourceAsync` | bestätigt (requirement.md) |
| 5 | Kein Audit-Log/Protokolleintrag | Nicht angefordert | bestätigt (requirement.md) |
| 6 | Keine Änderung am Systemexport | `PluginSettings` bleiben Teil des Exports; ein Restore kann entfernte Einträge zurückbringen — bewusst unverändert | bestätigt (requirement.md) |

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| `RemovePluginAsync` — Admin-Gate | `EnsureAdmin()` aufrufen, obwohl `SetEnabledAsync`/`MoveAsync` es nicht tun | Anforderung: administratorenvorbehalten; Konsistenz mit den Quellen-Methoden (`DeleteSourceAsync`, `SaveSourceAsync` u. a.) |
| Entfernen bei `Missing` + `Enabled == true` | Löschen ohne Rückfrage (klärt offene Frage 1 der requirement.md) | `Missing` bedeutet: Plugin nicht mehr auffindbar und faktisch inaktiv; `Enabled` hat dann keine Wirkung |
| Fehlermeldungen | Englisch, Muster `SetEnabledAsync` (`$"Plugin '{pluginId}' was not found."`) | Englisch für nicht lokalisierte Exception-Meldungen; die deutschen Meldungen der Quellen-Methoden sind deren Eigenheit und werden nicht als Muster übernommen |
| Status-Guard-Meldung | Englisch, z. B. `$"Plugin '{pluginId}' cannot be removed because its status is '{plugin.Status}'."` | Eindeutige, englische Ablehnung konsistent zum Service-Muster |

## Programmabläufe

### Entfernen eines vermissten Plugins

1. `RemovePluginAsync(pluginId, ct)` wird aufgerufen.
2. `EnsureAdmin()` prüft authentifizierten Admin → sonst `UnauthorizedAccessException`.
3. `db.PluginSettings.FindAsync([pluginId], ct)` lädt den Datensatz → `null` ⇒ `InvalidOperationException` („not found").
4. Guard: `plugin.Status != PluginStatus.Missing` ⇒ `InvalidOperationException` (Ablehnung).
5. `db.PluginSettings.Remove(plugin)` + `db.SaveChangesAsync(ct)`.
6. Eintrag ist aus `PluginSettings` entfernt und erscheint nicht mehr in `GetPluginsAsync`.

Beteiligte Klassen/Komponenten: `IPluginSettingsService`, `PluginSettingsService`, `RezepteDbContext`, `PluginSetting`, `PluginStatus`

### Wiederentdeckung nach Entfernen (unverändert, kein Code)

1. `PluginManager.InitializeAsync` → `SynchronizeSettingsAsync` (PluginManager.cs:422-476).
2. Die `pluginId` ist nach dem Löschen in `db.PluginSettings` unbekannt → `else`-Zweig (Zeilen 449-465).
3. Neuer `PluginSetting` mit `DiscoveredAt = now`, `OrderIndex` hinter dem aktuellen Maximum, `Enabled = Status == PluginStatus.Loaded`.

Beteiligte Klassen/Komponenten: `PluginManager`, `PluginSetting`, `PluginStatus`

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `PluginSettingsServiceTests_RemovePlugin` | Testklasse (`Rezepte.Tests/Services/Import/`) | Tests für `RemovePluginAsync`, erbt `PluginSettingsServiceTestsBase` (Muster: `PluginSettingsServiceTests_CheckSource`) |

## Änderungen an bestehenden Klassen

### `IPluginSettingsService` (Interface)

`Rezepte.Web/Services/Import/Plugins/IPluginSettingsService.cs`

- **Neue Methoden:** `Task RemovePluginAsync(string pluginId, CancellationToken ct = default)` — Entfernt einen `PluginSetting`-Eintrag im Status `Missing`; Dokumentationskommentar im Stil der übrigen Member.

### `PluginSettingsService` (Klasse)

`Rezepte.Web/Services/Import/Plugins/PluginSettingsService.cs`

- **Neue Methoden:** `RemovePluginAsync(string pluginId, CancellationToken ct)` — Ablauf `EnsureAdmin()` → `FindAsync` → `null` ⇒ `InvalidOperationException` → `Status != PluginStatus.Missing` ⇒ `InvalidOperationException` → `Remove` + `SaveChangesAsync` (Muster `DeleteSourceAsync`, Zeilen 172-184; englische Meldungen im Muster von `SetEnabledAsync`, Zeilen 204-214).

## Datenbankmigrationen

Keine.

## Validierungsregeln

| Feld / Objekt | Regel | Fehlerfall |
|---------------|-------|------------|
| Aufrufer-Kontext | Authentifizierter Admin (`IsInRole("Admin")` oder Claim `IsAdmin=true`) via `EnsureAdmin()` | `UnauthorizedAccessException` |
| `pluginId` | `PluginSetting` muss existieren | `InvalidOperationException` „not found" |
| `PluginSetting.Status` | Muss `PluginStatus.Missing` sein | `InvalidOperationException` mit Statusnennung |

## Konfigurationsänderungen

Keine.

## Seiteneffekte und Risiken

- **Interface-Erweiterung:** `IPluginSettingsService` hat genau eine Implementierung (`PluginSettingsService`); keine Test-Fakes des Interfaces — kein Bruch. `PluginSettings.razor` ist noch nicht angebunden (Schritt 5) und bleibt funktional unverändert.
- **Systemexport:** `PluginSettings` bleiben Teil des Exports — ein wiederhergestelltes Backup kann entfernte Einträge zurückbringen (bewusster Scope-Ausschluss, dokumentiert).
- **`Enabled`-Restwirkung:** Ein `Missing`-Eintrag mit `Enabled == true` wird ohne Zusatzlogik gelöscht; bei Wiederentdeckung wird `Enabled` aus der Neu-Plugin-Logik (`Status == Loaded`) gesetzt, nicht aus dem alten Wert — gewollt.

## Umsetzungsreihenfolge

1. **Interface-Methode `RemovePluginAsync` in `IPluginSettingsService`**
   - Voraussetzungen: Keine.
   - Beschreibung: Member-Deklaration mit `pluginId` und `CancellationToken ct = default` plus XML-Kommentar ergänzen.
2. **Implementierung in `PluginSettingsService`**
   - Voraussetzungen: Schritt 1 (Interface-Member); `EnsureAdmin`, `db.PluginSettings`, `PluginStatus.Missing` sind vorhanden.
   - Beschreibung: Methode nach `DeleteSourceAsync`-Muster implementieren; Status-Guard auf `Missing`; englische Fehlermeldungen.
3. **Testklasse `PluginSettingsServiceTests_RemovePlugin`**
   - Voraussetzungen: Schritt 2; `PluginSettingsServiceTestsBase` (vorhanden).
   - Beschreibung: Neue Datei `Rezepte.Tests/Services/Import/PluginSettingsServiceTests_RemovePlugin.cs` mit den unten gelisteten Fällen.
4. **Wiederentdeckungs-Test in `PluginManagerTests` ergänzen**
   - Voraussetzungen: Keine (bestehende Infrastruktur `PluginWorkspace`, `CreateServices`, `CreateSetting`).
   - Beschreibung: Test, der einen `PluginSetting`-Eintrag entfernt und per `InitializeAsync` die Neuanlage mit frischem `DiscoveredAt` und `OrderIndex` am Ende nachweist.
5. **Testlauf `Rezepte.Tests`**
   - Voraussetzungen: Schritte 1-4.
   - Beschreibung: `dotnet test Rezepte.Tests` ausführen; Ergebnis dokumentieren (Ausgangszähler 677/677).

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `RemovePluginAsync_ShouldRemoveMissingPlugin` | `PluginSettingsServiceTests_RemovePlugin` | Admin entfernt `Missing`-Eintrag; `db.PluginSettings` enthält `pluginId` danach nicht mehr; `GetPluginsAsync` listet ihn nicht |
| `RemovePluginAsync_ShouldRejectNonAdminUsers` | `PluginSettingsServiceTests_RemovePlugin` | `CreateHttpContextAccessor(isAdmin: false)` → `UnauthorizedAccessException`; Eintrag bleibt erhalten |
| `RemovePluginAsync_ShouldRejectMissingHttpContext` | `PluginSettingsServiceTests_RemovePlugin` | `HttpContextAccessor` ohne User → `UnauthorizedAccessException` |
| `RemovePluginAsync_ShouldRejectNonMissingStatus` | `PluginSettingsServiceTests_RemovePlugin` | Einträge mit `PluginStatus.Loaded` (und stichprobenartig `Incompatible`/`LoadFailed`/`RuntimeFailed` — `[Theory]` mit `InlineData`) → `InvalidOperationException`; Eintrag bleibt erhalten |
| `RemovePluginAsync_ShouldRejectUnknownPluginId` | `PluginSettingsServiceTests_RemovePlugin` | Unbekannte `pluginId` → `InvalidOperationException` |
| `RemovePluginAsync_ShouldRemoveMissingEnabledPlugin` | `PluginSettingsServiceTests_RemovePlugin` | `Missing`-Eintrag mit `Enabled == true` wird ohne Rückfrage gelöscht (Entscheidung zu offener Frage 1) |
| `InitializeAsync_ShouldRecreatePluginSettingAfterRemoval` | `PluginManagerTests` | Fixture-Plugin discoveren (`InitializeAsync`), `PluginSetting` direkt aus der DB löschen (simuliert `RemovePluginAsync`), erneut `InitializeAsync` → neuer Datensatz mit frischem `DiscoveredAt` und `OrderIndex` hinter dem Maximum der übrigen Einträge |
| Hilfsmethode: `Status`-Parameter für `CreateSetting` | `PluginSettingsServiceTestsBase` | Optionaler `status`-Parameter (Default `PluginStatus.Loaded`) oder `Status` im Test nachträglich setzen — Umsetzungsentscheidung, keine Pflicht |

### Betroffene bestehende Tests

Keine.

### E2E-Tests (primärer Funktionsnachweis)

Keine neuen E2E-Tests erforderlich: Dieser Schritt liefert ausschließlich einen Service-Vertrag ohne UI-Anbindung; es gibt keinen über UI oder Nutzeraktion erreichbaren neuen Ablauf (die Entfernen-Aktion folgt erst in Schritt 5). Die Browser-Suite bleibt als Regressionslauf bestehen; `RegisterTests.Register_WithDemoData_CreatesDemoData` ist als dokumentierte, fachfremde Unschärfe bekannt.

## Akzeptanzszenarien

| # | Szenario | Auslöser | Ausgangszustand | Schritte | Sichtbare Wirkung | Nachweis |
|---|----------|----------|------------------|----------|-------------------|----------|
| 1 | Happy Path: Admin entfernt vermisstes Plugin | Aufruf `RemovePluginAsync(pluginId)` durch Admin-Kontext | `PluginSetting` mit `Status == Missing` existiert | EnsureAdmin → Find → Guard → Remove → SaveChanges | Eintrag ist aus `PluginSettings` gelöscht; `GetPluginsAsync` enthält ihn nicht mehr | `RemovePluginAsync_ShouldRemoveMissingPlugin` |
| 2 | Nicht-Admin wird abgelehnt | Aufruf ohne Admin-Rolle/Claim oder ohne HttpContext | `Missing`-Eintrag existiert | EnsureAdmin schlägt fehl | `UnauthorizedAccessException`; Eintrag bleibt erhalten | `RemovePluginAsync_ShouldRejectNonAdminUsers`, `..._ShouldRejectMissingHttpContext` |
| 3 | Nicht-`Missing`-Status wird abgelehnt | Aufruf mit `pluginId` eines `Loaded`/`Incompatible`/`LoadFailed`/`RuntimeFailed`-Eintrags | Eintrag mit Nicht-`Missing`-Status existiert | Guard `Status != Missing` | `InvalidOperationException`; Eintrag bleibt erhalten | `RemovePluginAsync_ShouldRejectNonMissingStatus` |
| 4 | Unbekannte `pluginId` | Aufruf mit nicht vorhandener Id | Kein passender `PluginSetting`-Datensatz | Find → `null` | `InvalidOperationException` „not found" | `RemovePluginAsync_ShouldRejectUnknownPluginId` |
| 5 | Wiederentdeckung nach Entfernen | `PluginManager.InitializeAsync` bei erneut auffindbarem Plugin | Verwaltungseintrag wurde entfernt; Plugin ist im Discovery-Pfad | Synchronisation → `else`-Zweig | Neuer `PluginSetting` mit frischem `DiscoveredAt`, `OrderIndex` am Ende, `Enabled` gemäß Neu-Plugin-Logik | `InitializeAsync_ShouldRecreatePluginSettingAfterRemoval` |

## Offene Punkte

Keine.

- Offene Frage 1 aus requirement.md (Löschen bei `Missing` + `Enabled == true`): entschieden — **ja, ohne Rückfrage** (siehe Designentscheidungen).
- Offene Frage 2 aus requirement.md (`EnsureAdmin` auch auf `SetEnabledAsync`/`MoveAsync`): Folgebeobachtung außerhalb dieses Schritts — nicht Teil des Plans.
