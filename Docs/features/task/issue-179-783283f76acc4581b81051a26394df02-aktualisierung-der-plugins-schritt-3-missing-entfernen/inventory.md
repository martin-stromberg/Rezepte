# Bestandsaufnahme: Vermisste Plugins aus der Verwaltung entfernen (Schritt 3, Issue #179)

Bestandsaufnahme zu [requirement.md](requirement.md). Projektweite Bestandsaufnahme des Gesamtfeatures:
[docs/projects/task/issue-179-…-aktualisierung-der-plugins/inventory.md](../../../projects/task/issue-179-783283f76acc4581b81051a26394df02-aktualisierung-der-plugins/inventory.md) (Detaildokumente: [Logik](../../../projects/task/issue-179-783283f76acc4581b81051a26394df02-aktualisierung-der-plugins/inventory/logic.md), [Modelle](../../../projects/task/issue-179-783283f76acc4581b81051a26394df02-aktualisierung-der-plugins/inventory/models.md), [Tests](../../../projects/task/issue-179-783283f76acc4581b81051a26394df02-aktualisierung-der-plugins/inventory/tests.md)). Nachfolgend nur der für diesen Schritt relevante Ausschnitt, verifiziert am aktuellen Code.

## Löschmuster `DeleteSourceAsync`

`Rezepte.Web/Services/Import/Plugins/PluginSettingsService.cs:172-184` — Vorlage für die neue Methode:

1. `EnsureAdmin()` (Zeile 174)
2. `db.PluginSources.FindAsync([sourceId], ct)` → `null` ⇒ `InvalidOperationException` (Zeilen 175-176)
3. Secret-Bereinigung über `ISystemSecretStore` (für `PluginSetting` nicht nötig — kein Secret-Bezug)
4. `db.PluginSources.Remove(source)` + `SaveChangesAsync` (Zeilen 182-183)

`EnsureAdmin()` (Zeilen 251-263) wirft `UnauthorizedAccessException`, wenn kein authentifizierter Benutzer vorliegt oder weder Rolle `"Admin"` noch Claim `IsAdmin=true` gesetzt ist. `SetEnabledAsync` (Zeilen 204-214) und `MoveAsync` (Zeilen 222-249) rufen `EnsureAdmin` **nicht** auf; ihre Not-found-Meldung ist englisch: `$"Plugin '{pluginId}' was not found."` — das neue Entfernen folgt diesem englischen Meldungsmuster, aber mit `EnsureAdmin` (Konsistenz zu den Quellen-Methoden, laut Anforderung).

## `PluginSetting` + `PluginStatus.Missing`

- `Rezepte.Web/Entities/PluginSetting.cs`: Primärschlüssel `PluginId` (string), `Status` (string, Default `PluginStatus.Loaded`, Zeile 41), `Enabled` (bool, Default `true`), `OrderIndex` (int), `DiscoveredAt`, `LastSeenAt` (beide `DateTime`), `Error` (string?). Keine Schemaänderung nötig.
- `Rezepte.Web/Services/Import/Plugins/PluginStatus.cs`: **keine** Enum, sondern statische Klasse mit String-Konstanten: `Loaded` (Z. 11), `Missing` (Z. 15), `Incompatible` (Z. 19), `LoadFailed` (Z. 23), `RuntimeFailed` (Z. 27). Statusvergleiche erfolgen per `==` auf die Konstanten.

## `PluginManager.SynchronizeSettingsAsync` — Wiederentdeckung

`Rezepte.Web/Services/Import/Plugins/PluginManager.cs:422-476` (private; einziger Aufruf aus `InitializeAsync`, Zeile 105):

- Bekannte `pluginId` → Update bestehender Felder inkl. `Status`/`Error`/`LastSeenAt` (Zeilen 439-448).
- Unbekannte `pluginId` → `else`-Zweig (Zeilen 449-465): neuer `PluginSetting` mit `DiscoveredAt = now`, `OrderIndex = nextOrder++` (Initialwert = max. vorhandener `OrderIndex` + 1, Zeile 426), `Enabled = plugin.Status == PluginStatus.Loaded`.
- Nicht entdeckte bestehende Einträge → `Status = PluginStatus.Missing`, `Error = "Plugin was not found during startup discovery."` (Zeilen 468-473).

**Bestätigt:** Nach dem Löschen eines `Missing`-Eintrags ist die `pluginId` der Synchronisation unbekannt; bei Wiederentdeckung greift ohne jede Codeänderung der `else`-Zweig — frischer `DiscoveredAt`, `OrderIndex` hinter dem aktuellen Maximum, `Enabled` gemäß Neu-Plugin-Logik.

## Tests (Bestand)

- `Rezepte.Tests/Services/Import/PluginSettingsServiceTestsBase.cs` (aus Schritt 2): `CreateDb()` (EF Core InMemory, GUID-Name), `CreateSetting(pluginId, orderIndex, enabled)` (setzt `Status = PluginStatus.Loaded` fest — für `Missing`-Fixtures nachträglich überschreiben oder Hilfsmethode erweitern), `CreateHttpContextAccessor(isAdmin)`, `CreateServiceProvider()`, `FakeSecretStore`, `FakePluginManager`, `FakePluginUpdateService`.
- `PluginSettingsServiceTests.cs` (9 `[Fact]`) und `PluginSettingsServiceTests_CheckSource.cs` (3 `[Fact]`) — Muster für Konstruktion des Service mit/ohne Admin-Kontext; Nicht-Admin → `UnauthorizedAccessException`, fehlender HttpContext → `UnauthorizedAccessException`.
- `Rezepte.Tests/Services/Import/PluginManagerTests.cs` (17 `[Fact]`): `InitializeAsync_ShouldMarkPreviouslyConfiguredPluginAsMissing` (Z. 335), `InitializeAsync_ShouldKeepExistingOrderAndAppendNewPlugins` (Z. 303) decken Missing-Markierung bzw. Anhängen ans Ende ab. **Kein** dedizierter Test für „Eintrag gelöscht → Wiederentdeckung erzeugt frischen Datensatz" — ggf. ergänzen.
- `IPluginSettingsService` hat genau eine Implementierung (`PluginSettingsService`); keine Test-Fakes des Interfaces — Interface-Erweiterung bricht keine Test-Doubles.

**Testzähler (letzter Stand, Zitat aus `acceptance-schritt-2.md`):** `Rezepte.Tests` **677/677** bestanden; `Rezepte.Tests.Browser` 19/20 im Gesamtlauf, einziger Fehlschlag `RegisterTests.Register_WithDemoData_CreatesDemoData` (Timeout, Einzelwiederholung bestanden — dokumentierte Unschärfe, keine Regression). Kein neuer Testlauf in diesem Schritt.
