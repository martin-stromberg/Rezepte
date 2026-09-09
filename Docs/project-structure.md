# Projektstruktur

```text
Rezepte.sln
Rezepte.Web/        Webanwendung, API, Services, Datenmodell und Migrationen
Rezepte.Import.Abstractions/
                    Gemeinsame Verträge und DTOs für Import-Plugins
Rezepte.Import.PluginSdk/
                    Isoliert baubare Parser- und URL-Hilfen für externe Import-Plugins
Rezepte.Import.Plugins.Backup/
                    Backup-Import-Plugin im Hauptrepository
Rezepte.Import.Plugins.AIFoto/
                    KI-Foto-Import-Plugin im Hauptrepository
Rezepte.Import.Plugins.AIUrl/
                    KI-Webseiten-Import-Plugin im Hauptrepository
Rezepte.Tests/      Unit-Tests für zentrale Services
Rezepte.Tests.Browser/
                    Browser-/E2E-Tests mit Playwright
Rezepte.Tests.PluginFixture/
                    Testfixture für Plugin-bezogene Tests
.githooks/          Git-Hooks (pre-commit, pre-push) und Installationsskripte
docs/               Dokumentation und Installationshinweise
```

## Wichtige Bereiche in `Rezepte.Web`

- `Components/Pages`: Blazor-Seiten wie Startseite, Login, Kochbücher, Rezepte, Kalender und Einstellungen.
- `Components/Shared`: wiederverwendbare UI-Komponenten und Dialoge.
- `Controllers`: API-Endpunkte für Auth, Benutzer, Kochbücher, Rezepte, Kalender, Jobs, Einstellungen und Exporte.
- `Services`: Fachlogik und Infrastruktur.
- `Services/Import`: Import-Orchestrierung, PluginManager, hostseitige Persistenz neutraler Importdaten und KI-Hostadapter.
- `Data`, `Entities`, `Migrations`: EF-Core-Datenzugriff und Schemaentwicklung.
- `Resources`: Shared-Resource-Dateien für die UI-Lokalisierung (`IStringLocalizer<UiStrings>`, `UiStrings.resx`).
- `wwwroot`: statische Assets, CSS, JavaScript, Icons und Manifest.
