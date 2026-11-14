# Plan-Gegenprüfung

## Ergebnis

**Status:** Plan vollständig

## Abgleich Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| Einzelquellen-Check `CheckSourceForUpdatesAsync(sourceId, ct)` auf `IPluginUpdateService` mit Ergebnis-Rückgabe | Schritte 1–3: `PluginUpdateCheckResult`/`PluginUpdateCheckOutcome`, `ProcessSourceAsync`/`DownloadValidateInstallAsync` → `Task<PluginUpdateCheckResult>`, neue Methode unter `CheckGate` | `PluginUpdateServiceTests_CheckSource` (Install, AlreadyInstalled, Retry, alle Fehlerpfade) | Abgedeckt |
| Admin-geschützter Einstieg `IPluginSettingsService.CheckSourceForUpdatesAsync` via `EnsureAdmin` + Delegation | Schritt 4: erforderliche `IPluginUpdateService`-Konstruktorabhängigkeit (vor optionalen Parametern), `EnsureAdmin` + Delegation, Interface-Erweiterung | `PluginSettingsServiceTests_CheckSource`: `ShouldRejectNonAdminUsers`, `ShouldRejectMissingHttpContext`, `ShouldDelegateToUpdateService` | Abgedeckt |
| Gleicher Ablauf/dieselben Vorbedingungen wie Start-Prüfung (`ProcessSourceAsync` wiederverwenden: PAT, Release, ZIP-Asset, Upsert, Download/Validate/Install) | Programmablauf „Einzelquellen-Check" bildet jeden vorhandenen Pfad inkl. Fehler auf `Failed`/`NoUpdate` ab | Fehlerfall-Tests: `NoReleaseFound`, `NoZipAsset`, `PrivateSourceHasNoPat`, `UnexpectedException` | Abgedeckt |
| Unterscheidbares Ergebnis: kein Update / installiert+Tag / Fehler (UI-tauglich für Schritt 4) | `PluginUpdateCheckResult` (`Outcome`, `ReleaseTag`, `Error`, `AlreadyInstalled`, `IsSuccess`, Factories) — Muster `ApplicationUpdateCommandResult` | `ShouldInstallNewReleaseAndReturnInstalled` (Outcome+Tag), `ShouldReturnAlreadyInstalledForInstalledRelease` (`NoUpdate`+Flag+Tag), `Failed`-Tests | Abgedeckt |
| Installiertes Release ohne Neustart wirksam (`CoordinateReloadAsync`, versionierte Verzeichnisse aus Schritt 1) | Unveränderte Wiederverwendung `InstallWithReloadTrackingAsync`-Pfad (Scope-Ausschluss 7) | `ShouldInstallNewReleaseAndReturnInstalled` + bestehende Reload-Tracking-Tests (`ShouldPersistReloadFailureSeparately`) | Abgedeckt |
| Statusfelder (`LastCheckedAt`, `LastSuccessfulReleaseTag`, `LastError`, `LastErrorAt`) + `PluginSourceRelease` fortgeschrieben, über `GetSourcesAsync` sichtbar | Bestehende Fortschreibung in `ProcessSourceAsync`/`MarkSourceFailureAsync` unverändert; Sichtbarkeit via `PluginSourceSettingsItem` bereits gegeben | Install-Test (Statusfelder), alle `Failed`-Tests (`LastError`/`LastErrorAt`), `GetSourcesAsync`-Sichtbarkeit | Abgedeckt |
| Serialisierung: Start-Lauf und Einzelprüfungen warten sich gegenseitig ab, kein paralleler Lauf | Statischer `SemaphoreSlim CheckGate` um `CheckForUpdatesAsync` und `CheckSourceForUpdatesAsync`; wartend, kein Busy/Abbruch | `ShouldWaitForRunningStartupCheck` + `CheckForUpdatesAsync_ShouldWaitForRunningSourceCheck` (beide Richtungen, instanzübergreifend via `BlockingGitHubReleaseClient`) | Abgedeckt |
| Fehlende Objekte: unbekannte Id → Ablehnung; `Enabled`/`TrustConfirmed` nicht erfüllt → Ablehnung ohne Check; privates Secret fehlt → Fehlerpfad; fehlender `PluginSourceRelease` → anlegen; `Installed` → überspringen; Fehlerstatus → `Pending`-Reset + Retry | Scope-Klärung „Umgang mit fehlenden Objekten" + Validierungsregeln: alle Fälle als `Failed`-Ergebnis bzw. bestehender Upsert/Skip/Retry-Pfad | `Preconditions`-Klasse (3 Tests), `ShouldReturnFailedWhenPrivateSourceHasNoPat`, `ShouldReturnAlreadyInstalledForInstalledRelease`, `ShouldRetryPreviouslyFailedRelease` | Abgedeckt |
| Scope-Ausschlüsse (keine UI, keine Mehrquellen-/Gesamtprüfung, keine periodische Prüfung, kein Abbruch, kein Downgrade, kein Schema, keine Änderung an Validator/Client/Store/Installer/Start-Ablauf) | 7 Ausschlüsse mit Begründung und Status dokumentiert; Umsetzung beschränkt sich auf Service-Ebene | — (Ausschluss-Nachweis durch Planstruktur) | Abgedeckt |

## Fehlende oder unvollständige Testanforderungen

Keine — Status `Plan vollständig`.

## E2E-Abdeckung

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| Einzelquellen-Check per Admin-Aktion | Keiner geplant | Nicht erforderlich mit Begründung: kein UI-Einstieg in diesem Schritt (Scope-Ausschluss 1); der fachliche Auslöser ist ein Service-Aufruf, die UI-Aktion folgt in Schritt 4 — dort ist der E2E-Nachweis (Button → Ergebnisanzeige) zu planen. Kein anwendererreichbarer Pfad in diesem Schritt. |

## Fehlende oder unvollständige Planbestandteile

Keine — Status `Plan vollständig`.

## Hinweise

- **Scope-Ausschluss-Status:** Alle 7 Ausschlüsse tragen Status `offen`, obwohl die zugehörigen Designentscheidungen als geklärt dokumentiert sind und „Offene Punkte: Keine" gilt — inkonsistent; bei Nachplanung ggf. auf `bestätigt` setzen.
- **Bewusste Abweichung von der Anforderungs-Annahme:** Vorbedingungsverletzungen (unbekannte Id, `Enabled=false`, `TrustConfirmed=false`) werden als `Failed`-Ergebnis statt der in `requirement.md` Offene Frage 1 angenommenen `InvalidOperationException` umgesetzt. Die Entscheidung ist im Plan dokumentiert und begründet (UI-Rückmeldung in Schritt 4); `requirement.md` ggf. nachziehen, damit Anforderung und Plan konsistent bleiben.
- **PAT-Variante:** `IsPrivate` ohne `SecretName` läuft über denselben Fehlerpfad (`token == null` → „Private source has no stored PAT."); der geplante Test deckt die Variante „SecretName gesetzt, Store liefert `null`" ab — die zweite Variante ist implizit mitabgedeckt, ein expliziter Test ist optional.
- **Statischer Guard + Testparallelität:** Risiko korrekt benannt — Serialisierungstests müssen die `CheckGate`-Freigabe garantieren (TCS in `finally`, ggf. Timeout), da der Guard prozessweit auch über xUnit-Testklassen hinweg wirkt.
- **Schritt-4-Verpflichtung:** Der E2E-Nachweis des vollständigen Benutzerflusses (Auslösebutton, Ergebnisanzeige, Sperrung während des Laufs) ist explizit Schritt 4 vorbehalten und muss dort geplant werden.
- **Verifizierte Bestandsfakten:** `AddScoped`-Registrierungen (`ServiceCollectionExtensions.cs:218–219`), `EnsureAdmin`-Muster (`PluginSettingsService.cs:237–249`), statisches `InstallLock` (`PluginPackageInstaller.cs:13`), `_manualCheckGate` als Instanz-Feld (`ApplicationUpdateSettingsService.cs:64`), 9 `new PluginSettingsService(...)`-Aufrufe in `PluginSettingsServiceTests.cs`, 4 bestehende `PluginUpdateServiceTests`, `PluginDirectoryRegistryTestsBase` als Basisklassen-Konvention — alle Plan-Referenzen zutreffend.
