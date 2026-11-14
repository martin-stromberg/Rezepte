# Umsetzungsplan: Manuelle Update-Prüfung für eine einzelne Pluginquelle (Schritt 2, Issue #179)

## Übersicht

`IPluginUpdateService` erhält eine admin-geschützte Einzelquellen-Update-Prüfung `CheckSourceForUpdatesAsync`, die denselben Ablauf wie der Start-Lauf (`ProcessSourceAsync`) durchläuft und ihr Ergebnis als `PluginUpdateCheckResult` zurückgibt. Der Admin-Einstieg liegt in `IPluginSettingsService.CheckSourceForUpdatesAsync` (`EnsureAdmin` + Delegation). Start-Lauf und Einzelprüfungen werden über einen statischen `SemaphoreSlim`-Guard prozessweit serialisiert. Keine UI- und keine Datenbankänderungen.

## Scope-Klärung

| Feld | Festlegung |
|------|------------|
| Verarbeitungseinheit | Eine Update-Prüfung für genau eine per Id adressierte `PluginSource`, inkl. Fortschreibung von `PluginSourceRelease` und der Statusfelder `LastCheckedAt`, `LastSuccessfulReleaseTag`, `LastError`, `LastErrorAt` |
| Auslösende Ereignisse | Neu: `IPluginSettingsService.CheckSourceForUpdatesAsync(sourceId, ct)` durch einen Administrator (die UI-Aktion folgt in Schritt 4). Unverändert: `PluginUpdateHostedService` → `IPluginUpdateService.CheckForUpdatesAsync` beim Anwendungsstart |
| Erwartete Wirkung | `PluginUpdateCheckResult` mit unterscheidbarem Outcome (`NoUpdate` / `Installed` + Release-Tag / `Failed` + Fehlermeldung, plus `AlreadyInstalled`-Flag); gefundene Releases werden über den bestehenden `InstallWithReloadTrackingAsync` → `IPluginManager.CoordinateReloadAsync`-Pfad ohne Neustart wirksam; Quellen-Statusfelder und `PluginSourceRelease` werden wie beim Start-Lauf fortgeschrieben; zu keinem Zeitpunkt laufen zwei Prüfungen parallel |
| Umgang mit fehlenden Objekten | `PluginSource` zur Id nicht vorhanden → `Failed`-Ergebnis (kein Check, keine Exception). `Enabled`/`TrustConfirmed` nicht erfüllt → `Failed`-Ergebnis (kein Check). Privates Secret fehlt → bestehender Fehlerpfad von `ProcessSourceAsync` als `Failed`-Ergebnis (wiederverwenden). `PluginSourceRelease` zum neuesten Release fehlt → wie bisher neu anlegen (erzeugen). `PluginSourceRelease` mit Status `Installed` → überspringen, `NoUpdate` + `AlreadyInstalled` + Tag (wiederverwenden). `PluginSourceRelease` mit Fehlerstatus → wie bisher auf `Pending` zurücksetzen und erneut versuchen (aktivieren) |

### Scope-Ausschlüsse

| # | Einschränkung / Ausschluss | Begründung | Status |
|---|----------------------------|------------|--------|
| 1 | Keine UI-Änderungen — Auslöseaktion, Ergebnisanzeige und Sperrung folgen in Schritt 4 | Dieser Schritt stellt nur die Service-Funktionalität samt Ergebnis-Rückgabe bereit | bestaetigt |
| 2 | Keine Mehrquellen- oder Gesamtprüfung per manuellem Aufruf | Anforderung begrenzt den manuellen Check auf genau eine Quelle | bestaetigt |
| 3 | Keine automatische periodische Hintergrundprüfung | Projekt-Scope-Ausschluss | bestaetigt |
| 4 | Kein Abbruch/Sofort-„Busy"-Ergebnis eines zweiten Aufrufs — parallele Aufrufe warten am Guard | Anforderung verlangt Serialisierung; geklärt (siehe Designentscheidungen) | bestaetigt |
| 5 | Kein Downgrade-/Rollback-Angebot auf ältere Releases | Es wird wie bisher nur das neueste Release betrachtet | bestaetigt |
| 6 | Keine Änderungen an Entities/Datenbankschema | Vorhandene Statusfelder genügen | bestaetigt |
| 7 | Keine Änderung an `PluginPackageValidator`, `IGitHubReleaseClient`, `ISystemSecretStore`, `IPluginPackageInstaller`/`IPluginDirectoryRegistry` und am Ablauf der Start-Prüfung selbst | Bestehende Mechanik wird unverändert wiederverwendet | bestaetigt |

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Ergebnis-Typ | `sealed record PluginUpdateCheckResult(string Outcome, string? ReleaseTag, string? Error, bool AlreadyInstalled)` mit abgeleitetem `IsSuccess` und statischen Factory-Methoden (`NoUpdate`, `Installed`, `Failed`) | Folgt dem Muster `ApplicationUpdateCommandResult` (`ApplicationUpdateSettingsService.cs:316`); das `AlreadyInstalled`-Flag (geklärte Empfehlung) erlaubt der UI in Schritt 4, „bereits installiert" von „kein neueres Release" zu unterscheiden, ohne das `PluginSourceReleaseStatus`-Mapping zu duplizieren |
| Outcome-Abbildung | Neue statische Klasse `PluginUpdateCheckOutcome` mit `const string`-Werten `NoUpdate`, `Installed`, `Failed` | Konsistent zur Projekt-Konvention `PluginSourceReleaseStatus` (String-Konstanten statt Enum) |
| Vorbedingungsverletzungen | Werden als `Failed`-Ergebnis mit englischer Fehlermeldung zurückgegeben, nicht als `InvalidOperationException` | Geklärte Empfehlung: Ergebnis-Record statt Exception, damit die UI (Schritt 4) den Grund rückmelden kann; gilt für unbekannte Id, `Enabled == false` und `TrustConfirmed == false`. Fehlermeldungen bleiben englisch — konsistent zu den bestehenden `LastError`-Texten (`"Private source has no stored PAT."`); die UI lokalisiert die Darstellung |
| Serialisierung | `private static readonly SemaphoreSlim CheckGate = new(1, 1)` in `PluginUpdateService`; beide Einstiege (`CheckForUpdatesAsync`, `CheckSourceForUpdatesAsync`) laufen komplett unter `WaitAsync(ct)`/`Release` in `try/finally` | `PluginUpdateService` ist `Scoped` (`ServiceCollectionExtensions.cs:218`); der Hosted Service nutzt einen eigenen Scope ⇒ nur ein statischer Guard wirkt instanzübergreifend. Der Guard deckt die komplette Prüfung inkl. `ProcessSourceAsync` ab (geklärt); `InstallLock` in `PluginPackageInstaller` bleibt als innere Sperre der Installationsphase bestehen. Wartend, kein Busy-Ergebnis (geklärt) |
| Ergebnis-Rückgabe aus dem Ablauf | `ProcessSourceAsync` und `DownloadValidateInstallAsync` liefern `Task<PluginUpdateCheckResult>` statt `Task`; `CheckForUpdatesAsync` behält `Task` und verwirft die Teilergebnisse | Der komplette Ablauf liegt bereits in `ProcessSourceAsync`; nur die bisher geschluckten Ausgänge müssen als Rückgabewert weitergereicht werden. Signaturänderung ist intern (private Methoden) und risikoarm |
| `IPluginUpdateService`-Abhängigkeit in `PluginSettingsService` | Als erforderlicher Konstruktor-Parameter (vor den optionalen Parametern `IHttpContextAccessor?`/`ISystemSecretStore?`) | Scoped→Scoped ist unproblematisch (beide `AddScoped`, `ServiceCollectionExtensions.cs:218–219`; gemeinsamer `RezepteDbContext` je Scope, rein sequenzielle Await-Kette, keine Zirkularität — `PluginUpdateService` hängt nicht von `IPluginSettingsService` ab). Erforderlich statt optional, weil die Check-Methode ohne die Abhängigkeit nicht funktionsfähig wäre; die 9 Test-Konstruktoraufrufe werden angepasst |

## Programmabläufe

### Einzelquellen-Check (admin-geschützt)

1. `PluginSettingsService.CheckSourceForUpdatesAsync(sourceId, ct)` ruft `EnsureAdmin()` auf; bei nicht authentifiziertem/nicht admin User `UnauthorizedAccessException` (bestehendes Muster).
2. Delegation an `IPluginUpdateService.CheckSourceForUpdatesAsync(sourceId, ct)`; Rückgabewert wird unverändert durchgereicht.
3. `PluginUpdateService.CheckSourceForUpdatesAsync` wartet via `CheckGate.WaitAsync(ct)` auf die prozessweite Sperre.
4. Innerhalb der Sperre: `db.PluginSources.FindAsync([sourceId], ct)`. Nicht gefunden → `PluginUpdateCheckResult.Failed("Plugin source was not found.")`.
5. `!source.Enabled` → `Failed("Plugin source is disabled.")`; `!source.TrustConfirmed` → `Failed("Plugin source is not confirmed as trusted.")`.
6. Vorbedingungen erfüllt → `return await ProcessSourceAsync(source, ct)`; Sperre wird im `finally` freigegeben.
7. `ProcessSourceAsync` setzt `LastCheckedAt`, löscht `LastError`/`LastErrorAt`, speichert (bestehend).
8. PAT-Auflösung via `ISystemSecretStore.GetAsync` bei `IsPrivate`; kein Token → `MarkSourceFailureAsync` + `Failed("Private source has no stored PAT.")` (bestehender Pfad, jetzt mit Rückgabe).
9. `IGitHubReleaseClient.GetLatestReleaseAsync`; kein Release → `MarkSourceFailureAsync` + `Failed("No published GitHub release was found.")`.
10. `FindZipAsset`; kein ZIP → `UpsertReleaseFailureAsync` (`ValidationFailed`) + `Failed("Latest release has no ZIP asset.")`.
11. `PluginSourceRelease`-Lookup; `Status == Installed` → `NoUpdate(release.TagName, alreadyInstalled: true)` (bestehender Skip).
12. Sonst Upsert (neu `Pending` oder Reset von `ValidationFailed`/`DownloadFailed`/`InstallFailed`/`ReloadFailed`/`RateLimited` auf `Pending`) → `return await DownloadValidateInstallAsync(...)`.
13. `DownloadValidateInstallAsync`: Status-Fortschreibung `Downloading` → `Validating` → `Installing` → `Reloading` → `Installed`; bei Erfolg `Installed(record.ReleaseTag)`; Validierungsfehler → `Failed(validation.Error)`; Exceptions → bestehendes Status-Mapping + `Failed(ex.Message)`.
14. Nicht behandelte `Exception` in `ProcessSourceAsync` → `MarkSourceFailureAsync` + `Failed(ex.Message)`; `OperationCanceledException` wird weiterhin durchgereicht.

Beteiligte Klassen/Komponenten: `PluginSettingsService`, `IPluginUpdateService`/`PluginUpdateService`, `PluginUpdateCheckResult`, `PluginUpdateCheckOutcome`, `ISystemSecretStore`, `IGitHubReleaseClient`, `IPluginPackageValidator`, `IPluginPackageInstaller`, `RezepteDbContext`.

### Start-Lauf (serialisiert, fachlich unverändert)

1. `PluginUpdateHostedService.StartAsync` erzeugt Scope, ruft `IPluginUpdateService.CheckForUpdatesAsync` (bestehend).
2. `CheckForUpdatesAsync` wartet via `CheckGate.WaitAsync(ct)`, lädt alle `Enabled && TrustConfirmed`-Quellen und ruft sequenziell `ProcessSourceAsync` auf; die zurückgegebenen `PluginUpdateCheckResult`-Werte werden verworfen; Sperre im `finally` freigegeben.

Beteiligte Klassen/Komponenten: `PluginUpdateHostedService`, `PluginUpdateService`, `CheckGate`.

### Serialisierung zweiter Aufrufe

Ein eintreffender Aufruf (`CheckForUpdatesAsync` oder `CheckSourceForUpdatesAsync`) wartet am `CheckGate`, bis der laufende Lauf die Sperre freigibt, und führt danach seinen kompletten Ablauf aus. Es gibt kein Busy-Ergebnis und keinen Abbruch.

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `PluginUpdateCheckResult` | Datenmodellklasse (sealed record) | Ergebnis der Einzelquellen-Prüfung: `Outcome`, `ReleaseTag`, `Error`, `AlreadyInstalled`, abgeleitetes `IsSuccess`, statische Factories `NoUpdate`/`Installed`/`Failed`. Datei: `Rezepte.Web/Services/Import/Plugins/PluginUpdateCheckResult.cs` |
| `PluginUpdateCheckOutcome` | statische Konstantenklasse | Outcome-Werte `NoUpdate`, `Installed`, `Failed` (Muster `PluginSourceReleaseStatus`). Datei: `Rezepte.Web/Services/Import/Plugins/PluginUpdateCheckOutcome.cs` |
| `PluginUpdateServiceTestsBase` | abstrakte Test-Basisklasse | Gemeinsame Infrastruktur für die thematisch aufgeteilten `PluginUpdateService`-Tests: `CreateDb`, `CreateSut`, Quellen-Fixture-Helfer, Fakes. Datei: `Rezepte.Tests/Services/Import/PluginUpdateServiceTestsBase.cs` (Konvention: `PluginDirectoryRegistryTestsBase`) |

## Änderungen an bestehenden Klassen

### `IPluginUpdateService` (Interface)

- **Neue Methoden:** `CheckSourceForUpdatesAsync(string sourceId, CancellationToken ct = default) → Task<PluginUpdateCheckResult>` — Einzelquellen-Check mit Ergebnis-Rückgabe.
- Datei: `Rezepte.Web/Services/Import/Plugins/IPluginUpdateService.cs`

### `PluginUpdateService` (Klasse)

- **Neue Felder:** `private static readonly SemaphoreSlim CheckGate = new(1, 1)` — statisch, weil `Scoped`-Lebensdauer Instanz-Felder unwirksam macht (Hosted Service nutzt eigenen Scope).
- **Neue Methoden:** `CheckSourceForUpdatesAsync(sourceId, ct)` — `CheckGate.WaitAsync(ct)`; Quelle laden, Vorbedingungen als `Failed`-Ergebnis ablehnen, sonst `ProcessSourceAsync` aufrufen; `Release` im `finally`.
- **Geänderte Methoden:**
  - `CheckForUpdatesAsync` — kompletter Rumpf unter `CheckGate`; Teilergebnisse von `ProcessSourceAsync` werden verworfen (Signatur `Task` bleibt).
  - `ProcessSourceAsync` — Rückgabetyp `Task` → `Task<PluginUpdateCheckResult>`; alle bisherigen Early-Returns liefern das zugehörige Ergebnis (`Failed` für PAT/Release/ZIP-Fehler, `NoUpdate` + `AlreadyInstalled` für den Installed-Skip); `catch (Exception)` liefert zusätzlich zu `MarkSourceFailureAsync` `Failed(ex.Message)`; `OperationCanceledException` unverändert durchgereicht.
  - `DownloadValidateInstallAsync` — Rückgabetyp `Task` → `Task<PluginUpdateCheckResult>`; Erfolg → `Installed(record.ReleaseTag)`; Validierungsfehler und Exception-Mapping → `Failed(...)`; `finally`-Bereinigung unverändert.
- Datei: `Rezepte.Web/Services/Import/Plugins/PluginUpdateService.cs`

### `IPluginSettingsService` (Interface)

- **Neue Methoden:** `CheckSourceForUpdatesAsync(string sourceId, CancellationToken ct = default) → Task<PluginUpdateCheckResult>` — Admin-Einstieg.
- Datei: `Rezepte.Web/Services/Import/Plugins/IPluginSettingsService.cs`

### `PluginSettingsService` (Klasse)

- **Neue Konstruktor-Abhängigkeit:** `IPluginUpdateService pluginUpdateService` — erforderlich, vor den optionalen Parametern positioniert.
- **Neue Methoden:** `CheckSourceForUpdatesAsync(sourceId, ct)` — `EnsureAdmin()`, dann `return await pluginUpdateService.CheckSourceForUpdatesAsync(sourceId, ct)`.
- Datei: `Rezepte.Web/Services/Import/Plugins/PluginSettingsService.cs`

### `PluginSettingsServiceTests` (Testklasse)

- Alle 9 `new PluginSettingsService(...)`-Aufrufe erhalten die neue `IPluginUpdateService`-Abhängigkeit (Stub `FakePluginUpdateService`).
- Datei: `Rezepte.Tests/Services/Import/PluginSettingsServiceTests.cs`

## Datenbankmigrationen

Keine.

## Validierungsregeln

| Feld / Objekt | Regel | Fehlerfall |
|---------------|-------|------------|
| `sourceId` | `PluginSource` muss existieren | `Failed("Plugin source was not found.")` — kein Check, keine Statusänderung |
| `PluginSource.Enabled` | Muss `true` sein | `Failed("Plugin source is disabled.")` — kein Check, keine Statusänderung |
| `PluginSource.TrustConfirmed` | Muss `true` sein | `Failed("Plugin source is not confirmed as trusted.")` — kein Check, keine Statusänderung |
| Admin-Berechtigung | `EnsureAdmin` (authentifiziert + `IsInRole("Admin")` oder Claim `IsAdmin=true`) | `UnauthorizedAccessException` (bestehendes Muster) |

Die bestehenden Laufzeit-Validierungen von `ProcessSourceAsync` (fehlendes PAT, kein Release, kein ZIP-Asset) bleiben unverändert und werden als `Failed`-Ergebnis weitergereicht.

## Konfigurationsänderungen

Keine.

## Seiteneffekte und Risiken

- **`CheckForUpdatesAsync` unter Guard:** Der Start-Lauf kann jetzt hinter einem manuellen Check warten (und umgekehrt); `PluginUpdateHostedService.StartAsync` kann entsprechend länger blockieren — fachlich gewünscht (Serialisierung), aber sichtbare Verhaltensänderung.
- **Statischer Guard + Testparallelität:** `CheckGate` wirkt prozessweit — auch über parallel laufende xUnit-Testklassen hinweg. Serialisierungstests mit blockierendem Fake müssen die Freigabe garantieren (`TaskCompletionSource` in `finally` setzen, ggf. `WaitAsync` mit Timeout), sonst blockieren sie fremde Tests.
- **`PluginSettingsService`-Konstruktor:** Neue erforderliche Abhängigkeit bricht die 9 direkten Konstruktoraufrufe in `PluginSettingsServiceTests` — werden im selben Schritt angepasst. DI-Auflösung (`ServiceCollectionExtensions.cs:219`) ist unverändert lauffähig; weitere Implementierungen der Interfaces existieren nicht (nur die beiden konkreten Services).
- **Gemeinsamer `RezepteDbContext` je Scope:** `PluginSettingsService` und `PluginUpdateService` teilen sich im selben Request-Scope denselben DbContext; die Await-Kette ist rein sequenziell — kein Parallelitätsproblem, aber keine unabhängigen Kontexte (konsistent zum bisherigen Scoped-Muster).
- **Lock-Verschachtelung:** `CheckGate` (komplette Prüfung) umschließt `InstallLock` (Installationsphase in `PluginPackageInstaller`). Da `CheckGate` alle Check-Läufe serialisiert und `InstallLock` nur innerhalb der Installation betreten wird, ist keine Deadlock-Konstellation gegeben.

## Umsetzungsreihenfolge

1. **Ergebnis-Typen anlegen**
   - Voraussetzungen: Keine
   - Beschreibung: `PluginUpdateCheckOutcome` (Konstanten `NoUpdate`/`Installed`/`Failed`) und `PluginUpdateCheckResult` (Record + Factories + `IsSuccess`) in `Rezepte.Web/Services/Import/Plugins/` anlegen.

2. **`PluginUpdateService` umbauen: statischer Guard + Ergebnis-Rückgabe**
   - Voraussetzungen: Schritt 1
   - Beschreibung: `CheckGate` einführen; `ProcessSourceAsync` und `DownloadValidateInstallAsync` auf `Task<PluginUpdateCheckResult>` umstellen (alle Ausgänge mappen); `CheckForUpdatesAsync` komplett unter den Guard stellen, Ergebnisse verwerfen.

3. **`IPluginUpdateService.CheckSourceForUpdatesAsync` + Implementierung**
   - Voraussetzungen: Schritt 2
   - Beschreibung: Interface-Methode deklarieren; in `PluginUpdateService` Quelle per Id laden, Vorbedingungen als `Failed`-Ergebnis ablehnen, ansonsten `ProcessSourceAsync` unter `CheckGate` aufrufen.

4. **Admin-Einstieg in `IPluginSettingsService`/`PluginSettingsService`**
   - Voraussetzungen: Schritt 3 (Interface-Methode muss für die Delegation existieren)
   - Beschreibung: `IPluginUpdateService` als erforderliche Konstruktor-Abhängigkeit ergänzen; `CheckSourceForUpdatesAsync` mit `EnsureAdmin` + Delegation implementieren; Interface erweitern; die 9 Konstruktoraufrufe in `PluginSettingsServiceTests` um eine `FakePluginUpdateService`-Stub-Übergabe ergänzen (Kompilierbarkeit sichern).

5. **Test-Basisklasse extrahieren und Fakes erweitern**
   - Voraussetzungen: Schritte 2–4 (Produktiv-APIs kompilierbar)
   - Beschreibung: `PluginUpdateServiceTestsBase` anlegen (Konvention `PluginDirectoryRegistryTestsBase`); `CreateDb`, `CreateSut`, Quellen-Fixture-Helfer und die Fakes `FakeGitHubReleaseClient`, `FakePackageValidator`, `FakePackageInstaller`, `FailingPackageInstaller` dorthin verschieben; `FakeSecretStore` konfigurierbar machen (Token-Rückgabe steuerbar, Standard `"token"` — für PAT-Fehlerfall `null`); neuen `BlockingGitHubReleaseClient` (blockiert in `GetLatestReleaseAsync` via `TaskCompletionSource`, mit Entered-Signal und Release) für Serialisierungstests; `FakePluginUpdateService` (fängt `sourceId`, liefert konfigurierbares Ergebnis) für Settings-Tests; `PluginUpdateServiceTests` auf die Basisklasse umstellen (bestehende 4 Tests unverändert).

6. **Neue Testklassen**
   - Voraussetzungen: Schritt 5
   - Beschreibung: `PluginUpdateServiceTests_CheckSource` (Ergebnis-Mapping + Statusfortschreibung), `PluginUpdateServiceTests_Preconditions` (Ablehnungen), `PluginUpdateServiceTests_Serialization` (Guard-Warteverhalten), `PluginSettingsServiceTests_CheckSource` (Admin-Auth + Delegation).

7. **Testlauf**
   - Voraussetzungen: Schritt 6
   - Beschreibung: `dotnet test Rezepte.Tests --filter "FullyQualifiedName~Plugin"` (erwartet: alle Plugin-Tests grün), danach Gesamtsuite `dotnet test Rezepte.Tests` gegen die Baseline 609/609.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `CheckSourceForUpdatesAsync_ShouldInstallNewReleaseAndReturnInstalled` | `PluginUpdateServiceTests_CheckSource` | Neue Quelle + Release → `Outcome == Installed`, `ReleaseTag` gesetzt; `PluginSourceRelease.Status == Installed`, `source.LastSuccessfulReleaseTag`, `LastCheckedAt` gesetzt, `LastError` null |
| `CheckSourceForUpdatesAsync_ShouldReturnAlreadyInstalledForInstalledRelease` | dto. | `PluginSourceRelease` mit `Installed` zum neuesten Release → `Outcome == NoUpdate`, `AlreadyInstalled == true`, `ReleaseTag` gesetzt, keine Download-/Install-Aufrufe |
| `CheckSourceForUpdatesAsync_ShouldRetryPreviouslyFailedRelease` | dto. | `ValidationFailed`-Datensatz → Reset `Pending` → erneute Verarbeitung → `Installed`-Ergebnis |
| `CheckSourceForUpdatesAsync_ShouldReturnFailedWhenNoReleaseFound` | dto. | `GetLatestReleaseAsync` → `null` → `Failed` + `LastError`/`LastErrorAt` gesetzt |
| `CheckSourceForUpdatesAsync_ShouldReturnFailedWhenReleaseHasNoZipAsset` | dto. | Release ohne `.zip` → `Failed`; `PluginSourceRelease.Status == ValidationFailed`; `LastError` gesetzt |
| `CheckSourceForUpdatesAsync_ShouldReturnFailedWhenPrivateSourceHasNoPat` | dto. | `IsPrivate` + `SecretName`, `FakeSecretStore` liefert `null` → `Failed("Private source has no stored PAT.")`, `LastError` gesetzt, kein GitHub-Aufruf |
| `CheckSourceForUpdatesAsync_ShouldReturnFailedOnUnexpectedException` | dto. | Werfender Client/Installer → `Failed(ex.Message)`, `MarkSourceFailureAsync`-Wirkung (`LastError`) |
| `CheckSourceForUpdatesAsync_ShouldReturnFailedForUnknownSource` | `PluginUpdateServiceTests_Preconditions` | Unbekannte Id → `Failed`, `LatestReleaseCalls == 0` |
| `CheckSourceForUpdatesAsync_ShouldReturnFailedForDisabledSource` | dto. | `Enabled=false` → `Failed`, kein GitHub-Aufruf, `LastCheckedAt` unverändert |
| `CheckSourceForUpdatesAsync_ShouldReturnFailedForUnconfirmedSource` | dto. | `TrustConfirmed=false` → `Failed`, kein GitHub-Aufruf |
| `CheckSourceForUpdatesAsync_ShouldWaitForRunningStartupCheck` | `PluginUpdateServiceTests_Serialization` | `BlockingGitHubReleaseClient` in `CheckForUpdatesAsync` (eigene Service-Instanz/Scope-Simulation) blockiert; manueller Check auf zweiter Instanz wartet (`LatestReleaseCalls` des zweiten Clients bleibt 0, Task nicht abgeschlossen); nach Freigabe läuft der manuelle Check vollständig durch — Nachweis des statischen Guards über Instanzen hinweg |
| `CheckForUpdatesAsync_ShouldWaitForRunningSourceCheck` | dto. | Spiegelrichtung: laufende Einzelprüfung blockiert den Start-Lauf |
| `CheckSourceForUpdatesAsync_ShouldRejectNonAdminUsers` | `PluginSettingsServiceTests_CheckSource` | `isAdmin: false` → `UnauthorizedAccessException`; `FakePluginUpdateService` wurde nicht aufgerufen |
| `CheckSourceForUpdatesAsync_ShouldRejectMissingHttpContext` | dto. | Fehlender `HttpContext` → `UnauthorizedAccessException` |
| `CheckSourceForUpdatesAsync_ShouldDelegateToUpdateService` | dto. | Admin-Kontext → `FakePluginUpdateService` erhält `sourceId`, dessen konfiguriertes `PluginUpdateCheckResult` wird 1:1 zurückgegeben |
| `FakeSecretStore(string? token = "token")` | `PluginUpdateServiceTestsBase` | Konfigurierbarer Token-Rückgabewert für PAT-Fehlerfall |
| `BlockingGitHubReleaseClient` | `PluginUpdateServiceTestsBase` | `GetLatestReleaseAsync` blockiert auf `TaskCompletionSource` + `Entered`-Signal für Serialisierungstests |
| `FakePluginUpdateService` | `PluginSettingsServiceTests` (oder Basisklasse) | `IPluginUpdateService`-Stub: fängt `sourceId`, liefert konfigurierbares Ergebnis, `CheckForUpdatesAsync` als No-op |
| `AddSource`-Fixture-Helfer | `PluginUpdateServiceTestsBase` | Legt `PluginSource` mit Parametern (`Enabled`, `TrustConfirmed`, `IsPrivate`, `SecretName`) an |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `PluginUpdateServiceTests` (4 Tests) | Umzug der Hilfsmethoden/Fakes in `PluginUpdateServiceTestsBase`; Klasse erbt von der Base — Testlogik unverändert |
| `PluginSettingsServiceTests` (9 `new PluginSettingsService(...)`-Aufrufe) | Neue erforderliche `IPluginUpdateService`-Abhängigkeit → `FakePluginUpdateService`-Stub übergeben |

### E2E-Tests (primärer Funktionsnachweis)

Keine E2E-Tests in diesem Schritt — Begründung: Die Anforderung betrifft ausschließlich Service-Interna ohne über UI oder Nutzeraktion erreichbaren Ablauf (Scope-Ausschluss „keine UI"). Der fachliche Auslöser (Admin-Aktion in der Pluginverwaltung) wird erst in Schritt 4 angebunden; dort ist der E2E-Nachweis des vollständigen Benutzerflusses (Button → Ergebnisanzeige) zu planen. In diesem Schritt ist kein Pfad für einen Anwender erreichbar — Service-Tests mit EF-InMemory decken den fachlich prüfbaren Ablauf ab.

## Akzeptanzszenarien

| # | Szenario | Auslöser | Ausgangszustand | Schritte | Sichtbare Wirkung | Nachweis |
|---|----------|----------|------------------|----------|-------------------|----------|
| 1 | Happy Path: Einzelquellen-Check installiert neues Release | `IPluginSettingsService.CheckSourceForUpdatesAsync` mit Admin-Kontext | `PluginSource` existiert, `Enabled && TrustConfirmed`, neues GitHub-Release verfügbar, kein `PluginSourceRelease`-Datensatz | `EnsureAdmin` → Delegation → Guard → Quelle laden → `ProcessSourceAsync` → Download/Validierung/Installation via `CoordinateReloadAsync` | `PluginUpdateCheckResult` `Installed` + `ReleaseTag`; `PluginSourceRelease.Status == Installed`; `LastSuccessfulReleaseTag`, `LastCheckedAt` fortgeschrieben; Plugin ohne Neustart wirksam | `CheckSourceForUpdatesAsync_ShouldInstallNewReleaseAndReturnInstalled` |
| 2 | Bereits installiertes Release | wie 1 | `PluginSourceRelease` mit Status `Installed` zum neuesten Release vorhanden | wie 1 bis Release-Vergleich → Skip | `NoUpdate` + `AlreadyInstalled` + `ReleaseTag`; kein Download | `...ShouldReturnAlreadyInstalledForInstalledRelease` |
| 3 | Vorbedingung verletzt | wie 1 | Quelle `Enabled=false` (Varianten: unbekannte Id, `TrustConfirmed=false`) | `EnsureAdmin` → Delegation → Guard → Vorbedingung prüft negativ | `Failed` mit Meldung; kein GitHub-Aufruf; Statusfelder unverändert | `...ShouldReturnFailedForDisabledSource` u. a. |
| 4 | Serialisierung: manueller Check während Start-Lauf | Einzelcheck wird ausgelöst, während `CheckForUpdatesAsync` läuft (z. B. anwendungsweit beim Start) | Start-Lauf hält `CheckGate` | Manueller Check wartet an `WaitAsync`; nach Freigabe läuft er vollständig durch | Kein paralleler Lauf; Ergebnis nach Ablauf korrekt; kein Busy-/Abbruch-Ergebnis | `CheckSourceForUpdatesAsync_ShouldWaitForRunningStartupCheck` + Spiegeltest |
| 5 | Fehlerfall: kein PAT für private Quelle | wie 1 | `IsPrivate=true`, `SecretName` gesetzt, Secret-Store ohne Eintrag | Guard → `ProcessSourceAsync` → PAT-Auflösung schlägt fehl | `Failed("Private source has no stored PAT.")`; `LastError`/`LastErrorAt` gesetzt, über `GetSourcesAsync` sichtbar | `...ShouldReturnFailedWhenPrivateSourceHasNoPat` |
| 6 | Nicht-Admin | `CheckSourceForUpdatesAsync` ohne Admin-Rolle | beliebig | `EnsureAdmin` wirft | `UnauthorizedAccessException`; Update-Service nie aufgerufen | `...ShouldRejectNonAdminUsers` |

## Offene Punkte

Keine. (Die vier Fragen der Anforderung wurden vorab geklärt und sind als Designentscheidungen eingearbeitet: Vorbedingungs-Fehler als `Failed`-Ergebnis statt Exception; „bereits installiert" als `AlreadyInstalled`-Flag am `NoUpdate`-Ergebnis mit Tag; Guard wartet via `SemaphoreSlim`; Guard ist statisch und umschließt die komplette Einzelquellen-Prüfung inkl. `ProcessSourceAsync`.)
