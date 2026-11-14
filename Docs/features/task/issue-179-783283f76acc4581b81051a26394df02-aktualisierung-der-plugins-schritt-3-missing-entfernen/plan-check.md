# Plan-Gegenprüfung

## Ergebnis

**Status:** Plan vollständig

## Abgleich Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| Neue Service-Methode `RemovePluginAsync(pluginId, ct)` auf `IPluginSettingsService` + `PluginSettingsService` | Umsetzungsschritte 1-2; Signatur `Task RemovePluginAsync(string pluginId, CancellationToken ct = default)` entspricht dem Vorschlag der requirement.md | Indirekt über alle `RemovePluginAsync_*`-Tests | Abgedeckt |
| Administratorenvorbehalt (`EnsureAdmin`) | Programmablauf Schritt 2: `EnsureAdmin()` vor `FindAsync`; verifiziert gegen `EnsureAdmin` (PluginSettingsService.cs:251-263, wirft `UnauthorizedAccessException`) | `RemovePluginAsync_ShouldRejectNonAdminUsers`, `RemovePluginAsync_ShouldRejectMissingHttpContext` | Abgedeckt |
| Nur `Missing`-Einträge entfernbar; andere Status → `InvalidOperationException` | Guard `plugin.Status != PluginStatus.Missing` vor `Remove` (Programmablauf Schritt 4 vor 5); `PluginStatus` ist statische Klasse mit `const string` (PluginStatus.cs:6-27), `PluginSetting.Status` ist `string` (PluginSetting.cs:41) — Stringvergleich korrekt, kein Enum | `RemovePluginAsync_ShouldRejectNonMissingStatus` (`[Theory]` über `Loaded`/`Incompatible`/`LoadFailed`/`RuntimeFailed`) | Abgedeckt |
| Unbekannte `pluginId` → `InvalidOperationException` „not found" (Muster `SetEnabledAsync`) | Programmablauf Schritt 3; englische Meldung `$"Plugin '{pluginId}' was not found."` verifiziert (PluginSettingsService.cs:209) | `RemovePluginAsync_ShouldRejectUnknownPluginId` | Abgedeckt |
| Keine Dateisystemoperationen | Scope-Ausschluss 2 (bestätigt); Programmablauf enthält nur DB-Operationen | Implizit durch Testumfang (InMemory-DB, kein FS-Zugriff) | Abgedeckt |
| Eintrag verschwindet aus `GetPluginsAsync` | Erwartete Wirkung in Scope-Klärung und Programmablauf Schritt 6 | `RemovePluginAsync_ShouldRemoveMissingPlugin` (prüft DB + `GetPluginsAsync`) | Abgedeckt |
| Wiederentdeckung: frischer `DiscoveredAt`, `OrderIndex` am Ende, `Enabled` gemäß Neu-Plugin-Logik — ohne Codeänderung | Verifiziert: `SynchronizeSettingsAsync` `else`-Zweig (PluginManager.cs:449-465) setzt `DiscoveredAt = now`, `OrderIndex = nextOrder++` (Init max+1, Z. 426), `Enabled = plugin.Status == PluginStatus.Loaded` (Z. 458); `InitializeAsync` ist wiederholt aufrufbar (`ReloadAsync` ohne Einmal-Guard) | `InitializeAsync_ShouldRecreatePluginSettingAfterRemoval` in `PluginManagerTests`; Infrastruktur (`PluginWorkspace`, `CreateServices` mit geteilter InMemory-DB, `CreateSetting(pluginId, orderIndex, status)`) vorhanden | Abgedeckt |
| `Missing` + `Enabled == true` wird ohne Rückfrage gelöscht (offene Frage 1, entschieden) | Designentscheidung dokumentiert | `RemovePluginAsync_ShouldRemoveMissingEnabledPlugin` | Abgedeckt |
| Keine Schemaänderung | `Datenbankmigrationen: Keine` — korrekt, `PluginSetting` unverändert | — | Abgedeckt |

## E2E-Abdeckung

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| Entfernen-Funktion auf Service-Ebene | Keine neuen E2E-Tests | Nicht erforderlich mit Begründung: Dieser Schritt liefert ausschließlich den Service-Vertrag; die auslösende UI-Aktion (Button, Dialog) ist explizit Scope-Ausschluss und Gegenstand von Schritt 5 (requirement.md Zeile 10, 17). Es existiert kein über UI erreichbarer neuer Ablauf. Begründung im Plan-Abschnitt „E2E-Tests" nachvollziehbar. |
| Regressionslauf Browser-Suite | Bestehender Lauf bleibt; bekannte Unschärfe `RegisterTests.Register_WithDemoData_CreatesDemoData` benannt | Abgedeckt |

## Hinweise

- **Kern-Verifikation bestanden:**
  - `PluginStatus` ist eine statische Klasse mit `const string`-Konstanten (`PluginStatus.cs:6-27`), `PluginSetting.Status` ist `string` (`PluginSetting.cs:41`). Der Plan verwendet durchgehend `PluginStatus.Missing`-Konstanten im Stringvergleich — nirgends Enum-Semantik.
  - Reihenfolge verifiziert: `EnsureAdmin()` → `FindAsync` → `null`-Guard → `Status != Missing`-Guard → `Remove` + `SaveChangesAsync` — entspricht exakt dem `DeleteSourceAsync`-Muster (PluginSettingsService.cs:172-184) und dem Implementierungsansatz der requirement.md.
  - `EnsureAdmin` vor `FindAsync` verhindert zudem Existenzerkennung durch Nicht-Admins — korrekt platziert.
  - Wiederentdeckungs-Test ist umsetzbar: `InitializeAsync` → `ReloadAsync` → `InitializeCoreAsync` → `SynchronizeSettingsAsync` ist mehrfach aufrufbar (kein Initialisierungs-Guard); `CreateServices` teilt eine InMemory-DB über Scopes, sodass Löschen per Scope-DbContext und erneutes `InitializeAsync` denselben Datenbestand treffen.
  - Tasks-Mapping (`...-tasks.md`, 11 Aufgaben) deckt sich 1:1 mit den Umsetzungsschritten 1-5 und der Testtabelle des Plans — konsistent.
- **Kleinere Ungenauigkeiten in der inventory.md (nicht plan-relevant, kein Blocking):**
  - `SynchronizeSettingsAsync` wird aus `InitializeCoreAsync` (PluginManager.cs:105) aufgerufen, nicht direkt aus `InitializeAsync`.
  - `PluginManagerTests` enthält aktuell 21 `[Fact]` (inventory nennt 17).
- **Implementierungshinweis (keine Lücke):** Für den Wiederentdeckungs-Test sollte „OrderIndex am Ende" gegen mindestens einen weiteren bestehenden Eintrag geprüft werden (z. B. `CopyProductivePlugins` oder Seed-Settings), damit die Aussage „hinter dem Maximum" aussagekräftig ist — der Plantext („hinter dem Maximum der übrigen Einträge") impliziert das bereits. Ebenso ist für „frischer `DiscoveredAt`" ein Vergleich gegen den ursprünglichen Wert oder Mindest-Schwelle vorzusehen.
- **Hilfsmethode:** `PluginSettingsServiceTestsBase.CreateSetting` setzt `Status = PluginStatus.Loaded` fest (Zeile 47); der Plan benennt korrekt, dass `Status` für `Missing`-Fixtures nachträglich gesetzt oder die Signatur erweitert werden muss (Umsetzungsentscheidung, ausdrücklich keine Pflicht).
- Testkonstruktions-Muster (`new PluginSettingsService(db, FakePluginManager, CreateServiceProvider(), updateService, httpContextAccessor)`) ist durch `PluginSettingsServiceTests_CheckSource` belegt und für die neuen Tests direkt übernehmbar.
