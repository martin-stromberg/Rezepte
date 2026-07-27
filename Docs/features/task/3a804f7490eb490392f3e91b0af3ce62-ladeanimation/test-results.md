# Test-Ergebnisse

## Ergebnis

**Status:** Keine Fehler

## Zusammenfassung

- Gesamt: 271
- Bestanden: 263
- Fehlgeschlagen: 0
- Übersprungen: 8

## Testabdeckung

**Abdeckung:** 59.2 %

| Datei | Abdeckung |
|-------|-----------|
| Rezepte.Web/Services/Import/BaseImportHandler.cs | 6.2% |
| Rezepte.Tests.PluginFixture/TestImportPlugin.cs | 14.3% |
| Rezepte.Web/Services/Import/Plugins/PluginSettingsItem.cs | 23.1% |
| Rezepte.Web/Services/Import/BaseAIImportHandler.cs | 23.6% |
| Rezepte.Import.Plugins.AIFoto/AIFotoImportHandler.cs | 25.0% |
| Rezepte.Web/Services/RecipeService.cs | 27.3% |
| Rezepte.Web/Services/BackgroundJobs/ExportJobFileStore.cs | 28.6% |
| Rezepte.Web/Services/Import/Plugins/IPluginManager.cs | 33.3% |
| Rezepte.Web/Services/ExportService.cs | 36.4% |
| Rezepte.Web/Services/ExportService.cs | 38.5% |
| Rezepte.Import.Abstractions/ImportCollectionModels.cs | 40.0% |
| Rezepte.Web/Services/BackgroundJobs/BackgroundJob.cs | 45.5% |
| Rezepte.Web/Services/Import/Plugins/GitHubReleaseClient.cs | 46.1% |

## Fehlende Tests

Quelle: `Coverage-Daten`

**Dateien mit 0% Abdeckung (Top 20 von 548):**

- `Rezepte.Import.Plugins.AIFoto/AIFotoImportHandler.cs` — 0 % Abdeckung
- `Rezepte.Import.Plugins.AIUrl/AIUrlImportHandler.cs` — 0 % Abdeckung
- `Rezepte.Import.PluginSdk/ImportParserBase.cs` — 0 % Abdeckung
- `Rezepte.Import.PluginSdk/ParsedIngredient.cs` — 0 % Abdeckung
- `Rezepte.Import.PluginSdk/UrlHelpers.cs` — 0 % Abdeckung
- `Rezepte.Web/ApiAuthHandler.cs` — 0 % Abdeckung
- `Rezepte.Web/Components/LoadingBar.razor.cs` — 0 % Abdeckung
- `Rezepte.Web/Migrations/Migration*.cs` — 0 % Abdeckung
- `Rezepte.Web/Program.cs` — 0 % Abdeckung
- `Rezepte.Web/Services/AiUsageService.cs` — 0 % Abdeckung
- `Rezepte.Web/Services/Authentication/CredentialCryptography.cs` — 0 % Abdeckung
- `Rezepte.Web/Services/Authentication/PasswordHashing.cs` — 0 % Abdeckung
- `Rezepte.Web/Services/CookbookService.cs` — 0 % Abdeckung
- `Rezepte.Web/Services/GoogleCredentialsProvider.cs` — 0 % Abdeckung
- `Rezepte.Web/Services/Import/GeminiClient.cs` — 0 % Abdeckung
- `Rezepte.Web/Services/RecipeService.cs` (innerhalb) — 0 % Abdeckung
- `Rezepte.Web/Services/ShoppingListService.cs` — 0 % Abdeckung
- `Rezepte.Web/Services/SettingsService.cs` — 0 % Abdeckung
- `Rezepte.Web/Services/UserService.cs` (innerhalb) — 0 % Abdeckung
- `Rezepte.Web/Controllers/AuthController.cs` — 0 % Abdeckung

## Hinweise

- Die Browser-Tests (Rezepte.Tests.Browser) wurden übersprungen, da `Rezepte.Web` nicht veröffentlicht wurde. Führe `dotnet publish Rezepte.Web -c Release` aus, um diese Tests zu aktivieren.
- **Hauptgründe für niedrige Abdeckung:**
  - Unterstützungsschichten (Plugin-Infrastruktur, Migration, Kryptografie) werden durch Unit-Tests nicht abgedeckt
  - Einige Service-Methoden haben eher Integrations- als Unit-Tests
  - Generated files (Migrations) und Einstiegspunkt (Program.cs) sind nicht testbar
  - 548 Klassen haben 0% Abdeckung, meist in der Plugin-SDK und Hilfsbibliotheken

**Empfehlungen:**
- Erhöhe Coverage durch gezielte Unit-Tests für `BaseImportHandler` und verwandte Klassen
- Review ob alle Service-Methoden mit Unit-Tests abgedeckt sein sollten
- Publish-Tests können nach Release der Web-App durchgeführt werden
