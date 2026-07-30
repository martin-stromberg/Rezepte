# Program.cs, DI und Hosted Services

## Startstruktur

`Rezepte.Web/Program.cs` enthaelt nur den groben Host-Aufbau:

- `WebApplication.CreateBuilder(args)`
- `builder.ConfigureSerilog()`
- `builder.Services.AddRezepteServices(builder.Configuration, builder.Environment)`
- `app.ApplyDatabaseMigrationsAsync()`
- Middleware und Endpoint-Mapping
- `app.Run()`

Die eigentliche Service-Registrierung ist in `Rezepte.Web/Extensions/ServiceCollectionExtensions.cs` gekapselt. Das ist der zentrale Einstiegspunkt fuer eine Updater-Integration.

## Relevante DI-Registrierungen

In `AddRezepteServices` werden technische Options-Klassen gebunden:

- `ImageOptions` aus `Images`
- `AIOptions` aus `AI`
- `PluginUpdateOptions` aus `PluginUpdates`
- `GoogleCredentialsOptions` aus `GoogleCredentials`
- `LoadingBarOptions` aus `LoadingBar`

Danach folgen Framework-Services, EF Core, Auth, Infrastruktur, fachliche Services, Plugin-Services, ViewModels und `services.AddBackgroundJobQueue()`.

Fuer die Update-Anforderung relevant:

- `IExportService` ist scoped und wird mit `ExportService` registriert.
- `ExportJobFileStore` ist scoped.
- `IBackgroundJobHandler` fuer `ExportUserJobHandler` und `ExportAllJobHandler` ist scoped.
- `IPluginUpdateService` ist scoped.
- Hosted Services werden ueber `AddHostedService` registriert.
- Singleton-Services, die scoped Services brauchen, arbeiten bereits ueber Scope-Factories oder ueber explizit erzeugte Scopes.

## Hosted Services

Vorhandene Hosted Services:

- `BackgroundJobHostedService` in `AddBackgroundJobQueue`
- `PluginStartupService` in `AddRezepteServices`
- `PluginUpdateHostedService` in `AddRezepteServices`

`BackgroundJobHostedService` erbt von `BackgroundService`, liest Job-IDs aus einem Channel und erzeugt pro Job einen Scope. `PluginStartupService` und `PluginUpdateHostedService` implementieren `IHostedService` und fuehren Startup-Initialisierung bzw. Update-Checks aus.

## Konsequenzen fuer msTools.Updater

Ein Updater-Service kann technisch in `AddRezepteServices` registriert werden. Falls `msTools.Updater` selbst eine Extension wie `AddUpdater(...)` anbietet, sollte diese dort eingebunden werden. Falls es nur eine konkrete Klasse/API gibt, sollte ein kleiner Adapter-Service in `Rezepte.Web` die externe API kapseln.

Der Pre-Install-Hook darf `IExportService` nicht aus dem Root-ServiceProvider aufloesen, weil `ExportService` scoped ist. Korrektes Muster:

- `IServiceScopeFactory` injizieren
- im Callback Scope erzeugen
- `IExportService`, Options und Logger aus dem Scope holen
- Backup synchron/awaitbar abschliessen
- bei Fehler Exception/Fehlerstatus an den Updater zurueckgeben

## Beobachtungen

- `Program.cs` selbst muss wahrscheinlich nicht stark wachsen; die bestehende Projektkonvention bevorzugt Extension-Methoden.
- Es gibt keine bestehende Web-App-Update-Registrierung.
- Es gibt keine generische "ApplicationLifecycle"-, "Maintenance"- oder "StartupTasks"-Abstraktion. Eine neue kleine Service-Schicht waere akzeptabel, wenn sie die Updater-API kapselt.
