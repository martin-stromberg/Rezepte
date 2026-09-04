# Laufzeit- und Testauswirkungen

## SQLite-Laufzeit

`Rezepte.Web/Extensions/ServiceCollectionExtensions.cs:54-56` registriert den EF-Core-Kontext mit `UseSqlite`. Die Anwendung verwendet SQLite damit im normalen Datenzugriff. Die native SQLite-Bibliothek wird als transitive Runtime-Abhangigkeit verteilt; Build, Publish und Start der Anwendung muessen nach dem Upgrade geprueft werden.

## Relevante Tests

- `Rezepte.Tests/Services/ExportServiceExportTests.cs` prueft Export mit `SqliteConnection("DataSource=:memory:")`.
- `Rezepte.Tests/Services/ExportServiceRestoreTests.cs` prueft Restore, Validierung und Datenbankoperationen mit In-Memory-SQLite.
- `Rezepte.Tests/Services/ExportServiceSystemBackupTests.cs` prueft Systembackup sowie Restore in Quell- und Ziel-Datenbanken.
- `Rezepte.Tests.Browser/Infrastructure/RezepteAppFixture.cs` startet die publizierte Webanwendung mit einer temporaeren SQLite-Datei.

## AngleSharp

Es gibt keine produktiven AngleSharp-Verwendungen im Quellcode. Die Abhangigkeit stammt aus dem bUnit-Teststack und ist daher durch Komponenten-/Render-Tests zu validieren. Der Upgradepfad sollte auf Paketauflosung und erfolgreiche Tests achten, nicht auf eine fachliche API-Migration im Produktivcode.

## Testbedarf

Mindestens erforderlich sind Solution-Build, vollstaendige Solution-Tests, Browser-Testbuild/-lauf beziehungsweise der im CI vorgesehene Playwright-Pfad sowie der Vulnerability-Scan nach frischem Restore. Bei SQLite-Paketwechseln sind insbesondere Export, Restore, Systembackup und Browser-Startup risikoorientiert zu beobachten.
