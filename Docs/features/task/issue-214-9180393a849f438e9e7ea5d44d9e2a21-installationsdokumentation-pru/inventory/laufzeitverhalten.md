# Verifiziertes Laufzeitverhalten (Program.cs, Konfiguration, Pfade)

Alle Angaben gegen den Code im Branch-Stand geprüft.

## Middleware-Reihenfolge (`Rezepte.Web/Program.cs`)

| Zeilen | Aufruf | Bedeutung für die Doku |
|---|---|---|
| 18 | `builder.ConfigureSerilog()` | Log-Verzeichnis wird **vor** App-Start angelegt (s. Pfade) |
| 20–55 | `builder.UseAutoUpdate(...)` | `msTools.Updater`, Sektion `ApplicationUpdates`; `UpdateUnitName` relevant für Auto-Updates auf Linux |
| 61 | `AddRezepteServices(...)` | Registriert u. a. DbContext (SQLite), Auth, Rate Limiting; **konstruiert `JwtSigningKeyProvider` eagerly** → `Jwt:Key` muss in Production gesetzt sein |
| 68 | `ApplyDatabaseMigrationsAsync()` | DB-Datei + Migrationen beim Start → Schreibrecht auf DB-Pfad nötig |
| 76–80 | `app.Configuration["PathBase"]` → `UsePathBase(pathBase)` | Aktiv nur bei gesetztem Schlüssel; Default = No-op. **Steht vor allen Routing-/Static-File-Schritten**, damit der Rest der Pipeline den gestrippten Pfad sieht |
| 87–93 | `UseForwardedHeaders` | `XForwardedProto \| XForwardedFor`; `KnownProxies` = `IPAddress.Loopback` + `IPv6Loopback`; **keine** `KnownNetworks`, **kein** `ForwardLimit` (Default 1), `X-Forwarded-Host` nicht ausgewertet |
| 96–103 | Dev-Page / `UseExceptionHandler` + `UseHsts()` | `UseHsts()` nur außerhalb Development; wirkt faktisch nur auf HTTPS-Responses |
| 107 | `UseRequestLogging()` | |
| 110 | `UseStaticFiles()` | wwwroot |
| 113–114 | `UseAuthentication/UseAuthorization` | Cookie-Auth: `SecurePolicy=SameAsRequest`, `SameSite=Lax`, `HttpOnly`, 8 h, Sliding (ServiceCollectionExtensions.cs:93–104) |
| 117 | `UseRateLimiter()` | Login/Register: 10/60 s pro Client-IP — **nutzt `RemoteIpAddress`** → ohne funktionierendes `X-Forwarded-For` rate-limitet der Proxy-Loopback alle Clients gemeinsam |
| 120–123 | `UseWhen(!"/api") → UseAntiforgery()` | |
| 126–136 | `MapStaticAssets`, `MapRazorComponents<App>().AddInteractiveServerRenderMode()`, `MapControllers`, `UseRedirectToRegisterWhenNoUsers` | Blazor Server (interaktiv, SignalR-WebSocket) |

### PathBase-/Proxy-Auswirkungen im Detail

- `<base href>` ist PathBase-aware (Components/App.razor:9); Redirects nutzen
  `Request.PathBase` (SessionController, AuthController,
  RedirectToRegisterMiddleware, SecurityTxtController).
- **Same-Host-Pflicht:** `KnownProxies` enthält nur Loopback → ein Reverse-Proxy
  auf einem anderen Host wird nicht vertraut; dessen `X-Forwarded-*` werden
  verworfen → falsches `Request.Scheme`, `RemoteIp` = Proxy-IP, Rate Limiting
  pro Loopback-IP. Zusätzlich wären die Header dann von jedem direkt
  erreichbaren Client fälschbar (Begründung steht im Code-Kommentar Z. 82–86).
- **`X-Forwarded-Host` wird nicht ausgewertet** → die Proxy-Konfiguration muss
  den Original-`Host`-Header durchreichen, sonst sind absolute URLs/Redirects
  falsch (security.txt-Canonical nutzt `Request.Host`, SecurityTxtController:66).
- **Cookies:** `SameAsRequest` → hinter TLS-terminierendem Proxy mit wirksamem
  `X-Forwarded-Proto` wird `Secure` gesetzt; im Standalone-HTTP-Betrieb nicht
  (funktioniert, aber ohne Secure-Flag).
- **Blazor Server = SignalR:** WebSocket-fähiges Proxying nötig (Long-Polling-
  Fallback existiert, aber WebSocket-Support sollte in der Proxy-Beispielkonfig
  dokumentiert werden).

## Konfiguration (`Rezepte.Web/appsettings*.json`)

| Schlüssel | Wert (Production) | Deployment-Relevanz |
|---|---|---|
| `Urls` / `ASPNETCORE_URLS` | **nicht gesetzt** | Default-Bind `http://localhost:5000`; Standalone braucht expliziten Bind (z. B. `http://0.0.0.0:<port>`), Proxy-Pfad Loopback |
| `PathBase` | nicht gesetzt | Aktiviert `UsePathBase`; Env-Form in systemd: `Environment=PathBase=/rezepte` |
| `AllowedHosts` | `"*"` | Keine Host-Filterung |
| `Jwt:Key` | `""` | **Production-Pflicht:** ≥ 32 Zeichen, sonst `InvalidOperationException` beim Start (JwtSigningKeyProvider.cs:51–58, `MinimumKeyLength=32`, Placeholder `"JWT_KEY"`); Env: `Jwt__Key` |
| `Jwt:Issuer`/`Audience`/`LifetimeMinutes` | `rezepte`/`rezepte.api`/43200 | Defaults ok |
| `ConnectionStrings:Default` | nicht gesetzt → Fallback `Data Source=rezepte.db` | Relativer Pfad relativ zum Prozess-Arbeitsverzeichnis (= `WorkingDirectory` der Unit) |
| `UpdateBackups:Directory` | `update-backups` | Relativ → `ContentRootPath` (UpdateBackupService.cs:134–141), `RetentionCount=5` |
| `ApplicationUpdates:Enabled` | `false` | Auto-Update aus; `DownloadPath: "updates"`, `UpdateUnitName: "RezepteWebAutoUpdate"`, `StopHostAfterScriptStart: false` (für Auto-Install auf Linux müsste `true` — außerhalb Scope) |
| `Images:MaxSizeBytes` | 5 MiB | Bilder landen in der DB, nicht im Dateisystem |

## Persistente Pfade und Schreibrechte

**Wichtige Präzisierung:** zwei verschiedene Basen —

| Pfad | Basis | Fundstelle | Schreibrecht nötig für |
|---|---|---|---|
| `rezepte.db` (+ `-wal`/`-shm` bei WAL) | Prozess-Working-Directory (`WorkingDirectory` der Unit) | ServiceCollectionExtensions.cs:66 | DB-Anlage, Migrationen, Betrieb |
| `logs/app-*.log` | **`AppContext.BaseDirectory`** = Verzeichnis der `Rezepte.Web.dll` | LoggingExtensions.cs:25–46 | `Directory.CreateDirectory` beim Start; täglich rotierend, 7 Tage Retention |
| `update-backups/` | `ContentRootPath` | UpdateBackupService.cs:134–141 | Backup-Zips vor Programmupdates |
| `exports/` | `ContentRootPath` | ExportJobFileStore.cs:28–29 | Export-Zips der Benutzer |
| `plugins/` | `ContentRootPath` | PluginDirectoryRegistry.cs:20, PluginPackageInstaller.cs:52 | Plugin-Installation/Updates (`.install-state.json`, DLLs) |
| `updates/` | `ApplicationUpdates:DownloadPath` — Auflösung durch externe `msTools.Updater`-Komponente (Basis im Repo nicht verifizierbar) | appsettings.json:36 | nur bei aktiviertem Auto-Update |

- `ContentRootPath` in einem systemd-Betrieb = `WorkingDirectory` (hier
  `/var/www/rezepte`); `AppContext.BaseDirectory` = Publish-Verzeichnis.
  Bei identischem Layout fallen beide zusammen — die Doku sollte dennoch
  benennen, dass `logs/` am Assembly-Verzeichnis hängt.
- **Bilder/Uploads:** `RecipeImage.Data` ist `byte[]` in SQLite
  (RecipeImage.cs:33, RecipeService.cs:714–722) — kein Upload-Verzeichnis
  im Dateisystem; die Anforderungsformulierung „Upload-Verzeichnisse" ist
  in diesem Punkt faktisch unzutreffend.
- Konsequenz für Hardening: mit `ProtectSystem=strict` müsste das
  Deployment-Verzeichnis via `ReadWritePaths=` freigegeben werden — oder die
  Datenpfade werden auf ein separates Verzeichnis konfiguriert
  (`ConnectionStrings:Default` absolut, `UpdateBackups:Directory` absolut);
  `logs/` ist **nicht** konfigurierbar (hartcodiert auf BaseDirectory).

## Laufzeitrahmen

- `net10.0` (Rezepte.Web.csproj:4, Directory.Build.props:3);
  `Microsoft.NETCore.App` + `Microsoft.AspNetCore.App` 10.x beim
  framework-abhängigen Deployment (verifiziert durch
  `DeploymentDocumentationTests.FrameworkDependentLinuxPublish_…`).
- Kein eigenes TLS/HTTPS in der App — Standalone ist HTTP-only.
