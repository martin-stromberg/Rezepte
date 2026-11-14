# Anforderungsübersetzung: Manuelle Update-Prüfung für eine einzelne Pluginquelle (Schritt 2, Issue #179)

## Fachliche Zusammenfassung

`IPluginUpdateService` wird um eine gezielte Update-Prüfung für genau eine GitHub-Pluginquelle (`PluginSource`) erweitert, die denselben Ablauf und dieselben Vorbedingungen wie die bestehende Start-Prüfung (`CheckForUpdatesAsync` via `PluginUpdateHostedService`) durchläuft und ihr Ergebnis als Rückgabewert bereitstellt. Der Aufruf ist Administratoren vorbehalten und wird über eine neue Methode in `IPluginSettingsService` (Admin-Prüfung via `EnsureAdmin`, Delegation an `IPluginUpdateService`) ausgelöst. Parallel laufende Prüfungen — Start-Lauf und manuelle Einzelprüfungen — werden serialisiert; ein installiertes Release wird über den bestehenden koordinierten Austausch (`IPluginManager.CoordinateReloadAsync`, versionsbezogene Verzeichnisse aus Schritt 1) ohne Anwendungsneustart wirksam.

## Fachlicher Scope

- **Verarbeitungseinheit:** Eine einzelne Update-Prüfung für genau eine erfasste GitHub-Pluginquelle (`PluginSource`), inklusive der Fortschreibung ihrer Release-Historie (`PluginSourceRelease`) und ihrer Statusfelder (`LastCheckedAt`, `LastSuccessfulReleaseTag`, `LastError`, `LastErrorAt`).
- **Auslösende Ereignisse:**
  - Neu: explizite Service-Auslösung der Einzelprüfung für eine per Id identifizierte `PluginSource` durch einen Administrator (fachliche Nutzeraktion; die auslösende UI-Aktion folgt in Schritt 4).
  - Bestehend, unverändert: automatische Prüfung aller Quellen beim Anwendungsstart über `PluginUpdateHostedService` → `IPluginUpdateService.CheckForUpdatesAsync`.
- **Erwartete Wirkung:**
  - Die Einzelprüfung liefert ein unterscheidbares Ergebnis zurück: kein Update gefunden / Release installiert (mit Release-Tag) / Fehler (mit Fehlermeldung) — damit die Oberfläche in Schritt 4 das Ergebnis unmittelbar rückmelden kann.
  - Ein gefundenes Release wird heruntergeladen, geprüft, installiert und ohne Neustart wirksam; nachfolgende Importe verwenden die neue Pluginversion.
  - Die Statusfelder der Quelle und der `PluginSourceRelease`-Datensatz werden wie beim Start-Lauf fortgeschrieben und sind über `GetSourcesAsync` sichtbar.
  - Es gibt zu keinem Zeitpunkt gleichzeitig laufende Prüfungen/Installationen: Manuelle Einzelprüfungen und der Start-Lauf warten sich gegenseitig ab.
- **Umgang mit fehlenden Objekten:**
  - `PluginSource` zur übergebenen Id nicht vorhanden: Der Aufruf wird abgelehnt (Fehler — konsistent zum bisherigen Muster `InvalidOperationException` in `PluginSettingsService`; Annahme, siehe Offene Fragen).
  - Vorbedingungen nicht erfüllt (`Enabled == false` oder `TrustConfirmed == false`): Der Aufruf wird abgelehnt, ohne einen Check zu starten (Annahme, siehe Offene Fragen).
  - Private Quelle ohne hinterlegtes PAT (`IsPrivate` und kein Token via `ISystemSecretStore`): bestehender Fehlerpfad von `ProcessSourceAsync` — Quellen-Fehlerstatus wird gesetzt und als Fehler-Ergebnis zurückgegeben (wiederverwenden).
  - Kein `PluginSourceRelease`-Datensatz zum aktuellen Release: wird wie bisher neu angelegt (erzeugen).
  - `PluginSourceRelease` mit Status `Installed` zum neuesten Release bereits vorhanden: wird übersprungen und als „kein Update gefunden" gemeldet (wiederverwenden).
  - `PluginSourceRelease` mit Fehlerstatus (`ValidationFailed`, `DownloadFailed`, `InstallFailed`, `ReloadFailed`, `RateLimited`): wird wie bisher erneut versucht, indem der Datensatz auf `Pending` zurückgesetzt wird (aktivieren).
- **Scope-Ausschlüsse:**
  - Keine UI-Änderungen — Auslöseaktion, Ergebnisanzeige und Sperrung während eines Laufs folgen in Schritt 4; dieser Schritt stellt nur die Service-Funktionalität samt Ergebnis-Rückgabe bereit.
  - Keine Prüfung mehrerer Quellen in einem manuellen Aufruf und keine manuelle Gesamtprüfung aller Quellen — ausschließlich Einzelquellen-Check.
  - Keine automatische periodische Hintergrundprüfung (Projekt-Scope-Ausschluss).
  - Keine Änderung am Ablauf der Start-Prüfung, an `PluginPackageValidator`, `IGitHubReleaseClient`, `ISystemSecretStore` oder am Installationsmechanismus (`IPluginPackageInstaller`/`IPluginDirectoryRegistry` aus Schritt 1).
  - Kein Abbruch einer laufenden Prüfung — parallele Aufrufe werden serialisiert (warten), nicht verworfen.
  - Kein Downgrade-/Rollback-Angebot auf ältere Releases über den manuellen Check (es wird wie bisher nur das neueste Release betrachtet).
  - Keine Änderung an Entities/Datenbankschema — vorhandene Statusfelder genügen.

## Betroffene Klassen und Komponenten

- **Logikklassen / Services (`Rezepte.Web/Services/Import/Plugins/`):**
  - `IPluginUpdateService` / `PluginUpdateService` — neue Methode für die Einzelquellen-Prüfung (Vorschlag: `CheckSourceForUpdatesAsync(string sourceId, CancellationToken)`), die die Quelle lädt, die Vorbedingungen prüft und die bestehende `ProcessSourceAsync`-Logik wiederverwendet. `ProcessSourceAsync` bzw. `DownloadValidateInstallAsync` müssen so angepasst werden, dass der Ausgang der Prüfung (kein Update / installiert / fehlgeschlagen) an den Aufrufer zurückgemeldet werden kann — bisher werden Fehler intern in `MarkSourceFailureAsync` abgelegt und geschluckt. Ergänzend ein gemeinsamer Guard, der `CheckForUpdatesAsync` und die Einzelprüfung gegenseitig serialisiert.
  - `IPluginSettingsService` / `PluginSettingsService` — neue Methode für den administrativen Einstieg (Vorschlag: `CheckSourceForUpdatesAsync(string sourceId, CancellationToken)`), die `EnsureAdmin` aufruft und an `IPluginUpdateService` delegiert; dafür ist `IPluginUpdateService` als Abhängigkeit zu injizieren.
- **Datenmodellklassen:**
  - Neu: Ergebnis-Datentransferobjekt für den Prüfausgang (Vorschlag: Record `PluginUpdateCheckResult` mit Outcome-Angabe, optionalem `ReleaseTag` und optionalem `Error`; Outcome als String-Konstanten im Stil von `PluginSourceReleaseStatus` oder als Enum — Umsetzungsentscheidung). Muster: `ApplicationUpdateCommandResult` (`Outcome`, `State`, `Message`, `Error`) in `ApplicationUpdateSettingsService`.
  - `PluginSource`, `PluginSourceRelease`, `PluginSourceSettingsItem`, `PluginSourceReleaseStatus` — unverändert.
- **Interfaces:** Erweiterung von `IPluginUpdateService` und `IPluginSettingsService` (s. o.); keine neuen eigenständigen Interfaces erforderlich.
- **Enums:** Keine zwingend neuen Status — die Ausprägungen kein Update / installiert / Fehler können als String-Konstanten im Stil von `PluginSourceReleaseStatus` abgebildet werden (Umsetzungsentscheidung).
- **UI-Komponenten / Controller:** Keine in diesem Schritt (Schritt 4).
- **Tests:**
  - `Rezepte.Tests/Services/Import/PluginUpdateServiceTests.cs` — Einzelquellen-Check: erfolgreiche Installation, Überspringen installierter Releases, Wiederholung fehlgeschlagener Releases, Fehlerfälle (kein Release, kein ZIP-Asset, fehlendes PAT, nicht gefundene/deaktivierte/nicht bestätigte Quelle), Fortschreibung der Quellen-Statusfelder, Serialisierung gegenüber `CheckForUpdatesAsync`.
  - `Rezepte.Tests/Services/Import/PluginSettingsServiceTests.cs` — Admin-Prüfung der neuen Methode (Ablehnung ohne Admin, Delegation an den Update-Service), analog den bestehenden `EnsureAdmin`-Tests.

## Implementierungsansatz

- **Einstiegspunkt und Admin-Prüfung:** Die öffentliche, admin-geschützte Aufrufstelle liegt in `PluginSettingsService` (vorhandenes `EnsureAdmin`-Muster über `IHttpContextAccessor`; `IsInRole("Admin")` oder Claim `IsAdmin=true`). `PluginUpdateService` selbst bleibt kontextfrei, damit der Start-Lauf im `PluginUpdateHostedService` ohne `HttpContext` weiterhin funktioniert.
- **Einzelquellen-Check:** Die neue Methode in `PluginUpdateService` lädt die `PluginSource` per Id aus `db.PluginSources`, lehnt ab, wenn sie nicht existiert oder `Enabled`/`TrustConfirmed` nicht erfüllt sind, und führt anschließend denselben Ablauf wie `ProcessSourceAsync` aus (Token via `ISystemSecretStore`, `IGitHubReleaseClient.GetLatestReleaseAsync`, `FindZipAsset`, `PluginSourceRelease`-Upsert, `DownloadValidateInstallAsync`). Damit das Ergebnis rückmeldbar ist, wird `ProcessSourceAsync` dahingehend refaktoriert, dass es seinen Ausgang zurückliefert, statt Fehler nur intern zu protokollieren.
- **Serialisierung:** `PluginUpdateService` ist `Scoped` registriert (`ServiceCollectionExtensions.cs:218`); der Start-Lauf (eigener Scope im `PluginUpdateHostedService`) und Request-Scopes erzeugen daher separate Instanzen. Der Guard muss instanzübergreifend wirken — naheliegendes Muster ist ein statischer `SemaphoreSlim` analog `InstallLock` in `PluginPackageInstaller` (`PluginPackageInstaller.cs:13`) bzw. `_manualCheckGate` in `ApplicationUpdateSettingsService` (`ApplicationUpdateSettingsService.cs:64`). Beide Einstiege (`CheckForUpdatesAsync` und die Einzelprüfung) werden unter denselben Guard gestellt; `InstallLock` in der Installationsphase bleibt zusätzlich bestehen.
- **Neustartfreie Wirksamkeit:** Bereits vorhanden — `InstallWithReloadTrackingAsync` → `IPluginManager.CoordinateReloadAsync` mit den versionsbezogenen Verzeichnissen und `IPluginDirectoryRegistry` aus Schritt 1. Die Einzelprüfung nutzt diesen Pfad unverändert weiter.
- **Abhängigkeiten:** `PluginSettingsService` erhält `IPluginUpdateService` als neue Abhängigkeit; beide Services sind `Scoped` und teilen denselben `RezepteDbContext` je Scope — unproblematisch, da die Settings-Methode nur delegiert.

## Konfiguration

Keine neue Konfiguration erforderlich. Die bestehende Sektion `PluginUpdates` (`PluginUpdateOptions`: `GitHubApiBaseUrl`, `TimeoutSeconds`, `UserAgent`) gilt für die Einzelprüfung genauso wie für den Start-Lauf.

## Offene Fragen

Alle ursprünglich offenen Fragen wurden in der Planung entschieden (siehe `plan.md`, Designentscheidungen):

1. **Nicht erfüllte Vorbedingungen** (Quelle unbekannt, deaktiviert oder nicht vertrauensbestätigt): Ablehnung als `Failed`-Ergebnis statt Ausnahme — damit die Oberfläche den Grund in Schritt 4 unmittelbar rückmelden kann.
2. **„Bereits installiertes Release"**: wird als `NoUpdate`-Ergebnis mit `AlreadyInstalled`-Flag und Release-Tag zurückgegeben.
3. **Parallel eintreffende Aufrufe**: warten am Guard (strenge Serialisierung, kein „Busy"-Ergebnis).
4. **Serialisierungsgranularität**: der statische Guard umschließt die komplette Prüfung (inkl. GitHub-Release-Abruf) für beide Einstiege.
