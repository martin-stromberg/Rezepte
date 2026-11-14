# Tasks: Vermisste Plugins aus der Verwaltung entfernen (Schritt 3, Issue #179)

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Logik | `RemovePluginAsync(string pluginId, CancellationToken ct = default)` in `IPluginSettingsService` deklarieren | Offen | — |
| 2 | Logik | `RemovePluginAsync` in `PluginSettingsService` implementieren (`EnsureAdmin` → `FindAsync` → Guard `Status == PluginStatus.Missing` → `Remove` + `SaveChangesAsync`, englische Meldungen) | Offen | — |
| 3 | Tests | Testklasse `PluginSettingsServiceTests_RemovePlugin` anlegen (erbt `PluginSettingsServiceTestsBase`) | Offen | — |
| 4 | Tests | Test: Admin entfernt `Missing`-Eintrag (`RemovePluginAsync_ShouldRemoveMissingPlugin`) | Offen | — |
| 5 | Tests | Test: Nicht-Admin abgelehnt (`RemovePluginAsync_ShouldRejectNonAdminUsers`) | Offen | — |
| 6 | Tests | Test: fehlender HttpContext abgelehnt (`RemovePluginAsync_ShouldRejectMissingHttpContext`) | Offen | — |
| 7 | Tests | Test: Nicht-`Missing`-Status abgelehnt (`RemovePluginAsync_ShouldRejectNonMissingStatus`, `[Theory]` über `Loaded`/`Incompatible`/`LoadFailed`/`RuntimeFailed`) | Offen | — |
| 8 | Tests | Test: unbekannte `pluginId` abgelehnt (`RemovePluginAsync_ShouldRejectUnknownPluginId`) | Offen | — |
| 9 | Tests | Test: `Missing` + `Enabled == true` wird ohne Rückfrage gelöscht (`RemovePluginAsync_ShouldRemoveMissingEnabledPlugin`) | Offen | — |
| 10 | Tests | Wiederentdeckungs-Test in `PluginManagerTests` ergänzen (`InitializeAsync_ShouldRecreatePluginSettingAfterRemoval`: frischer `DiscoveredAt`, `OrderIndex` am Ende) | Offen | — |
| 11 | Tests | `dotnet test Rezepte.Tests` ausführen und Ergebnis dokumentieren (Ausgangszähler 677/677) | Offen | — |
