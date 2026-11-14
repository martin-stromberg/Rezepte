# Umsetzungsplan: Versionsverzeichnisse für Plugin-Installationen (Issue #179, Schritt 1)

## Übersicht

Die Installation quelleninstallierter Import-Plugins wird von der bisherigen In-Place-Ersetzung auf versionsbezogene Verzeichnisse `plugins/<Name>@<ReleaseTag>/` umgestellt. Eine neue Verwaltungskomponente (`PluginDirectoryRegistry`) führt eine persistente Zustandsdatei `plugins/.install-state.json`, die verwaltete Installationen und als zu löschen markierte Verzeichnisse festhält; `PluginManager` schließt markierte, nicht aktuelle und überschattete Verzeichnisse aus der Erkennung aus und bereinigt Markierungen am Ende erfolgreicher Initialisierungsläufe. Das beobachtbare Fehlerverhalten (`InstallFailed`/`ReloadFailed` über `PluginPackageInstallException`, Rücksicherung des Vorbestands) bleibt erhalten, das Temp-Backup wird durch ein Registry-Snapshot-Rollback ersetzt.

## Scope-Klärung

Verbindliche fachliche Abgrenzung vor der Umsetzung — übernommen bzw. konkretisiert aus dem Abschnitt `Fachlicher Scope` der `requirement.md`:

| Feld | Festlegung |
|------|------------|
| Verarbeitungseinheit | Eine Release-Installation einer GitHub-Pluginquelle: das ZIP-Asset eines GitHub-Releases mit einem oder mehreren validierten Plugin-Verzeichnissen (`PluginPackageValidationResult.PluginDirectories`), verarbeitet über `PluginUpdateService.DownloadValidateInstallAsync` → `IPluginPackageInstaller.InstallWithReloadTrackingAsync`. |
| Auslösende Ereignisse | (a) Startprüfung aktivierter und vertrauensbestätigter `PluginSource`-Einträge beim Anwendungsstart über `PluginUpdateHostedService` → `IPluginUpdateService.CheckForUpdatesAsync`. (b) Interne Bereinigung: zu Beginn des Dateiaustauschs innerhalb des `CoordinateReloadAsync`-Callbacks sowie am Ende jeder erfolgreichen `InitializeCoreAsync`-Ausführung (deckt Anwendungsstart und koordinierten Austausch ab). |
| Erwartete Wirkung | Der wirksame Pluginbestand liegt in `plugins/<Name>@<ReleaseTag>/`-Verzeichnissen; die Erkennung lädt je Plugin-Basisnamen genau eine wirksame Version. Vorgänger-Verzeichnisse sind gelöscht oder in `.install-state.json` als `pendingDelete` markiert, aus der Erkennung ausgeschlossen und werden beim nächsten erfolgreichen Initialisierungslauf entfernt. Im neuen Release entfallene Plugins erhalten `PluginStatus.Missing` via `SynchronizeSettingsAsync`. Bei Fehlschlag bleibt der Vorbestand wirksam bzw. wird wiederhergestellt; `PluginSourceRelease.Status`/`ReloadStatus` verhalten sich wie bisher (`InstallFailed`/`ReloadFailed`). |
| Umgang mit fehlenden Objekten | Versionsverzeichnis der neuen Version: bei jeder Installation neu erzeugt (Ableitung `<Quellverzeichnisname>@<bereinigter ReleaseTag>`). Löschmarkierung: Eintrag in `pendingDelete` der Zustandsdatei `plugins/.install-state.json`, erzeugt sobald `Directory.Delete` auf ein Altverzeichnis mit `IOException`/`UnauthorizedAccessException` scheitert. Zustandsdatei: fehlend oder korrupt → als leerer Zustand gelesen (fail-open, Warn-Log) und beim nächsten Schreibzugriff erzeugt. Altverzeichnisse ohne Markierung, die nicht zum neuen Bestand gehören (`name@*` neben der wirksamen Version): werden beim Austausch verworfen (gelöscht bzw. bei Sperrung markiert). Flache Verzeichnisse ohne Versionsmuster: nie gelöscht; bei gleichnamigem `name@*`-Verzeichnis lediglich aus der Erkennung ausgeschlossen (überschattet, siehe Scope-Ausschluss 6). |

### Scope-Ausschlüsse

Dem Anwender vorgezeigt via Issue #179 (Kommentar 2026-11-14); Ausschlüsse 1-5 und 7 sind wörtliche Festlegungen der Anforderung/des Projektplans, Ausschluss 6 ist eine konservative Sicherheitsentscheidung (kein Datenverlustrisiko) — Bestätigung durch Projektleiter, Veto im PR-Review möglich.

| # | Einschränkung / Ausschluss | Begründung | Status |
|---|----------------------------|------------|--------|
| 1 | Mitgelieferte Plugins aus dem Anwendungs-Publish (`plugins/<Projektname>/`, z. B. `Rezepte.Import.Plugins.Backup`, `.AIFoto`, `.AIUrl`) sind von Versionsverzeichnis-, Lösch- und Markierungsbehandlung ausgenommen. | Expliziter Scope-Ausschluss der Anforderung. | bestätigt (2026-11-14) |
| 2 | Kein manueller Update-Trigger und kein UI-Ablauf; Auslöser bleibt ausschließlich die Startprüfung. | Expliziter Scope-Ausschluss der Anforderung (folgt in Schritt 2/4/5 des Gesamtprojekts). | bestätigt (2026-11-14) |
| 3 | Kein Mehrversionen-Betrieb: paralleles Laden mehrerer Versionen desselben Plugins ist nicht vorgesehen; markierte Altverzeichnisse sind unwirksam. | Expliziter Scope-Ausschluss der Anforderung. | bestätigt (2026-11-14) |
| 4 | Keine Änderung am Validierungsverhalten des ZIP-Pakets (`PluginPackageValidator`) über das hinaus, was das Verzeichnismodell erfordert (keine Änderung nötig — siehe Designentscheidungen). | Expliziter Scope-Ausschluss der Anforderung. | bestätigt (2026-11-14) |
| 5 | Keine Konfigurierbarkeit des Namensschemas oder des Löschmarkierungs-Mechanismus. | Expliziter Scope-Ausschluss der Anforderung; Namensschema und Zustandsdatei-Format sind fest im Code verankert. | bestätigt (2026-11-14) |
| 6 | Flache Verzeichnisse ohne Versionsmuster (Altbestände aus der In-Place-Ära sowie manuell/publish-seitig abgelegte Ordner) werden niemals gelöscht — bei gleichnamigem `name@*`-Verzeichnis werden sie aus der Erkennung ausgeschlossen (überschattet), bleiben aber auf dem Datenträger. | Herkunft eines flachen Verzeichnisses (frühere Quelleninstallation vs. Publish-Plugin vs. manuell abgelegt) ist zur Laufzeit nicht zuverlässig unterscheidbar; Löschen riskiert Datenverlust bei namentlicher Kollision (Anforderung verlangt Kollisionsabsicherung). | bestätigt (2026-11-14) |
| 7 | Direkt unter `plugins/` liegende `*.dll`-Dateien bleiben unverwaltet: sie werden wie bisher erkannt und weder versioniert noch gelöscht oder markiert. | Der Installationspfad erzeugt ausschließlich Verzeichnisse; Top-Level-DLLs stammen aus Publish-/manuellen Ablagen ohne Versionierungskontext. | bestätigt (2026-11-14) |

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Namensschema der Versionsverzeichnisse | Flach: `plugins/<Quellverzeichnisname>@<bereinigter ReleaseTag>/` (ein `@`-Trenner, erster `@` als Parser-Anker) | `DiscoverExternalPlugins`/`GetPluginAssemblyCandidates` betrachten exakt eine Unterverzeichnisebene — das flache Schema erfordert keine Tiefensuche und bleibt mit dem vorhandenen Fallback (alle `*.dll` im Verzeichnis, wenn `<Verzeichnisname>.dll` fehlt) kompatibel. `@` dient zugleich als syntaktischer Marker verwalteter Verzeichnisse. |
| Persistenz der Löschmarkierung und Identifikation verwalteter Verzeichnisse | Zentrale Zustandsdatei `plugins/.install-state.json`, verwaltet vom neuen Singleton `PluginDirectoryRegistry` (atomares Schreiben über Temp-Datei + `File.Move`) | Marker-Datei im Altverzeichnis oder Umbenennung können im Handle-Szenario ebenfalls scheitern (Anforderung); die Zustandsdatei erfordert nur Schreibzugriff auf `plugins/` — ohnehin Voraussetzung jeder Installation. Sie löst zugleich die geforderte Unterscheidung quelleninstallierter vs. Publish-Verzeichnisse: verwaltet ist, was der Installer registriert hat bzw. dem `name@tag`-Muster folgt. |
| Rollback-Modell | Registry-Snapshot + verzögertes physisches Löschen: Altverzeichnisse werden im Austausch-Callback nur als `pendingDelete` markiert und erst nach erfolgreicher Reinitialisierung (Ende von `InitializeCoreAsync`) gelöscht. Das `%TEMP%`-Backup (`backupRoot`) entfällt. | Da Vorgängerverzeichnisse beim Reload-Fehler noch unverändert auf dem Datenträger liegen, genügt zum Wiederherstellen das Zurückschreiben des Zustands-Snapshots plus Entfernen/Markieren der neuen Verzeichnisse — keine Backup-Kopie und keine Rückkopie nötig. Beobachtbares Verhalten (Vorbestand bleibt wirksam, `InstallFailed`/`ReloadFailed`) bleibt identisch. |
| Auswahl der wirksamen Version bei Mehrfachverzeichnissen | Registry-Eintrag (`current` je Basisname) ist autoritativ; ohne Eintrag wird das Versions-Maximum gewählt (`System.Version`-Parse nach Entfernen eines führenden `v`/`V`, sonst ordinal-absteigender Verzeichnisname) | Deterministisch und crash-sicher: nur im Registry-Zustand verbuchte Versionen wurden einmal aktiviert; verwaiste `name@*`-Verzeichnisse ohne Registereintrag werden über den Fallback bewertet und nicht-gewählte als Altverzeichnisse verworfen. |
| Signaturerweiterung `IPluginPackageInstaller` | Neuer Record `PluginPackageInstallOptions(string ReleaseTag, string? SourceKey)` als zusätzlicher Parameter beider Methoden | Kapselt die für Versionsverzeichnis-Ableitung (ReleaseTag) und quellenbezogene Entfall-Bereinigung (SourceKey = `PluginSource.Id`, für `Missing`-Markierung entfallener Plugins) nötigen Angaben erweiterbar in einem Objekt, statt zwei Einzelparameter. |
| Anwendungsbereich der Ausschlusslogik | Versions-/Schatten-/Markierungslogik gilt nur unterhalb des Install-Roots `{ContentRootPath}/plugins` (`IsManagedRoot`); `DiscoverFromDirectory` auf freie Verzeichnisse (Paketvalidierung im Temp-Pfad) und das `BaseDirectory`-Root bleiben im bisherigen Verhalten | Der Validator entpackt nach `%TEMP%` und erwartet unveränderte Discovery-Semantik; Build-/Publish-Outputs im `bin`-Root werden ohnehin bei jedem Build ersetzt. In Produktion fallen `ContentRootPath` und `BaseDirectory` zusammen — die Unterscheidung ist dann gegenstandslos. |
| Entfallener Plugin (Release ohne bisheriges Plugin-Verzeichnis) | Installer markiert Registry-Einträge mit `SourceKey` der aktuellen Quelle, deren Basisname nicht in der eingehenden Menge liegt, als `pendingDelete`; `SynchronizeSettingsAsync` vergibt `Missing` wie bisher | Der Installer kennt Plugin-Ids nicht; die Quellzugehörigkeit (`PluginSource.Id`) ist die einzige verlässliche Gruppierung dafür, welche verwalteten Verzeichnisse ein Release ersetzt bzw. entfallen lässt. |

## Programmabläufe

### Release-Installation in Versionsverzeichnisse (Happy Path)

1. `PluginUpdateHostedService.StartAsync` erzeugt einen DI-Scope und ruft `IPluginUpdateService.CheckForUpdatesAsync` auf.
2. `PluginUpdateService.ProcessSourceAsync` ermittelt das neueste Release, legt den `PluginSourceRelease`-Datensatz an bzw. setzt fehlgeschlagene Stati zurück und ruft `DownloadValidateInstallAsync`.
3. `DownloadValidateInstallAsync` lädt das ZIP herunter, validiert es über `IPluginPackageValidator.ValidateAsync` (unverändert) und ruft `packageInstaller.InstallWithReloadTrackingAsync(validation.PluginDirectories, new PluginPackageInstallOptions(record.ReleaseTag, source.Id), beforeReload, ct)`.
4. `PluginPackageInstaller.InstallWithReloadTrackingAsync` validiert die Eingaben (nicht-leere Liste, verwertbarer `ReleaseTag`), nimmt `InstallLock`, legt `plugins/` an, lädt den `PluginInstallState` über `IPluginDirectoryRegistry.LoadState()` und merkt sich einen Snapshot.
5. Innerhalb von `IPluginManager.CoordinateReloadAsync` (unter `_lifecycleLock`, `_loadedPlugins` geleert) führt der Austausch-Callback aus:
   - `IPluginDirectoryRegistry.CleanupMarkedDirectories(state)` — best-effort Löschversuch bisheriger `pendingDelete`-Einträge (räumt ggf. das Zielverzeichnis eines früheren Fehlversuchs frei).
   - Je Quellverzeichnis: Zielname `<Name>@<Tag>`; existiert das Ziel, wird es zuerst gelöscht (Löschfehler → `pendingDelete`-Eintrag + Ausnahme → `InstallFailed`); anschließend `CopyDirectory` des Quellverzeichnisses.
   - Vorgänger-Ermittlung: alle verwalteten Verzeichnisse desselben Basisnamens (Registry-Eintrag plus `name@*`-Verzeichnisse auf dem Datenträger) außer dem neuen Ziel sowie alle Registry-Einträge desselben `SourceKey`, deren Basisname nicht eingeht, werden in `pendingDelete` aufgenommen und ihre Registry-Einträge entfernt/ersetzt.
   - `IPluginDirectoryRegistry.SaveState(state)` (atomar); `replacementCompleted = true`; `beforeReload`-Callback (setzt `Status`/`ReloadStatus` auf `Reloading`).
6. `CoordinateReloadAsync` ruft `InitializeCoreAsync`: Discovery erkennt nur das neue Versionsverzeichnis (markierte und nicht aktuelle `name@*`-Verzeichnisse sowie überschattete Flachverzeichnisse sind ausgeschlossen), `_loadedPlugins` wird aufgebaut, `SynchronizeSettingsAsync` synchronisiert `PluginSetting` (entfallene Plugins → `Missing`).
7. Am Ende von `InitializeCoreAsync` löscht `IPluginDirectoryRegistry.CleanupMarkedDirectories` die markierten Altverzeichnisse; nicht löschbare bleiben in `pendingDelete` und ausgeschlossen.
8. `PluginUpdateService` persistiert `Status = Installed`, `InstalledAt`, `ReloadStatus = Installed`, `LastSuccessfulReleaseTag`.

Beteiligte Klassen/Komponenten: `PluginUpdateHostedService`, `PluginUpdateService`, `IPluginPackageValidator`, `PluginPackageInstaller`, `IPluginDirectoryRegistry`/`PluginDirectoryRegistry`, `IPluginManager`/`PluginManager`, `PluginInstallState`, `PluginPackageInstallOptions`, `RezepteDbContext`.

### Ausschluss markierter, nicht aktueller und überschatteter Verzeichnisse aus der Erkennung

1. `DiscoverPlugins` ruft `DiscoverExternalPlugins(GetPluginRoots(), …)`.
2. `DiscoverExternalPlugins` lädt einmalig den `PluginInstallState` (nur relevant, wenn ein Root `IsManagedRoot` ist) und filtert die direkten Unterverzeichnisse je Root:
   - Verzeichnisname ∈ `pendingDelete` (nur unter dem Install-Root) → ausgeschlossen.
   - Verzeichnisname matcht `<Basis>@<Tag>` → gruppiert nach Basisname; wirksam ist der Registry-`current`-Eintrag (falls vorhanden und auf Datenträger existent), sonst das Versions-Maximum; alle übrigen → ausgeschlossen.
   - Flaches Verzeichnis ohne `@`, zu dem mindestens ein `<Basis>@*`-Verzeichnis existiert → überschattet → ausgeschlossen.
   - Alle übrigen Verzeichnisse (Publish-Plugins, flache Altbestände ohne Versionsverzeichnis, beliebige Ordner unter nicht verwalteten Roots) → wie bisher über `GetPluginAssemblyCandidates` einbezogen.
3. Top-Level-`*.dll` der Roots werden wie bisher einbezogen (unverwaltet).
4. `DiscoverFromDirectory` mit beliebigem Root (Paketvalidierung): Root ist nicht `IsManagedRoot` → unveränderte Einbeziehung aller direkten Unterverzeichnisse.

Beteiligte Klassen/Komponenten: `PluginManager` (`DiscoverExternalPlugins`, `GetPluginAssemblyCandidates`, `GetPluginRoots`), `IPluginDirectoryRegistry` (`IsManagedRoot`, `LoadState`, `GetExcludedDirectoryNames`/`TryParseVersionedDirectoryName`/Auswahlhilfen), `PluginInstallState`.

### Start- und Austausch-Bereinigung markierter Verzeichnisse

1. `PluginStartupService.StartAsync` → `IPluginManager.InitializeAsync` → `ReloadAsync` → unter `_lifecycleLock` `InitializeCoreAsync`.
2. `InitializeCoreAsync` führt Discovery + `_loadedPlugins`-Aufbau + `SynchronizeSettingsAsync` aus; markierte Verzeichnisse waren dabei bereits ausgeschlossen.
3. Nur bei Erfolg (kein Fehler in Discovery/Settings-Sync) ruft `InitializeCoreAsync` abschließend `IPluginDirectoryRegistry.CleanupMarkedDirectories` best-effort auf: je `pendingDelete`-Eintrag `Directory.Delete`; Erfolg → Eintrag entfernt, `IOException`/`UnauthorizedAccessException` → Eintrag bleibt (Error-Log); nicht mehr existente Einträge werden verworfen. Anschließend `SaveState`. Fehler der Bereinigung werden geloggt und brechen die Initialisierung nicht ab.
4. Derselbe Schritt läuft am Ende jedes erfolgreichen `InitializeCoreAsync` innerhalb von `CoordinateReloadAsync` — also unmittelbar nach jedem verifizierten Austausch (physische Entfernung der Vorgängerversionen) — sowie beim nächsten Anwendungsstart (Nachbereinigung verbliebener Markierungen).

Beteiligte Klassen/Komponenten: `PluginStartupService`, `PluginManager` (`ReloadAsync`, `CoordinateReloadAsync`, `InitializeCoreAsync`), `IPluginDirectoryRegistry` (`CleanupMarkedDirectories`).

### Fehler- und Rollback-Ablauf (InstallFailed / ReloadFailed)

1. Ausnahme irgendwo in `InstallWithReloadTrackingAsync` (Kopierfehler, `SaveState`-Fehler, `beforeReload`-Fehler oder Fehler in `InitializeCoreAsync` des äußeren `CoordinateReloadAsync`) → `catch` im Installer.
2. `RollBackAsync` ruft erneut `pluginManager.CoordinateReloadAsync` mit einem Callback, der: (a) den Zustands-Snapshot wiederherstellt, (b) alle in diesem Lauf neu angelegten Zielverzeichnisse zusätzlich in `pendingDelete` aufnimmt und den Zustand speichert, (c) diese neuen Verzeichnisse best-effort per `TryDeleteDirectory` entfernt (nicht löschbare bleiben markiert und ausgeschlossen).
3. Die anschließende `InitializeCoreAsync` des Rollback-Laufs erkennt den Vorbestand (Altverzeichnisse waren nie gelöscht worden) und bereinigt die markierten Neuverzeichnisse.
4. Der Installer wirft `PluginPackageInstallException` mit `Status = InstallFailed` (Fehler vor `replacementCompleted`) bzw. `ReloadFailed` (danach); `PluginUpdateService` mappt auf `record.Status`/`ReloadStatus`/`ReloadError` wie bisher.
5. Fehler innerhalb des Rollback-Laufs werden nur geloggt (unverändertes Verhalten).

Beteiligte Klassen/Komponenten: `PluginPackageInstaller` (`RollBackAsync`, `TryDeleteDirectory`), `IPluginDirectoryRegistry` (Snapshot/`SaveState`/`pendingDelete`), `PluginManager` (`CoordinateReloadAsync`, `InitializeCoreAsync`), `PluginPackageInstallException`, `PluginUpdateService` (Fehlermapping).

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `PluginPackageInstallOptions` | Datenmodellklasse (sealed record) | Übergabeobjekt an den Installer: `ReleaseTag` (Pflicht, Quelle der Versionsverzeichnis-Ableitung), `SourceKey` (`string?`, `PluginSource.Id` — steuert die Entfall-Bereinigung für `Missing`-Markierung). Datei: `Rezepte.Web/Services/Import/Plugins/PluginPackageInstallOptions.cs`. |
| `PluginInstallState` | Datenmodellklasse | Persistenter Zustand (`plugins/.install-state.json`): `Version` (Schema-Version, Startwert 1), `Installs` (Liste von `PluginInstallEntry`), `PendingDeletes` (Liste markierter Verzeichnisnamen). |
| `PluginInstallEntry` | Datenmodellklasse | Eintrag je verwaltetem Plugin-Basisnamen: `BaseName`, `DirectoryName` (aktuelles `name@tag`-Verzeichnis), `ReleaseTag`, `SourceKey` (`string?`). Kann als eigenständige Datei oder im selben File wie `PluginInstallState` liegen. |
| `IPluginDirectoryRegistry` | Interface | Verwaltungskomponente für Verzeichnisnamens-Ableitung, Zustandsladen/-speichern, Löschmarkierung, Erkennungs-Ausschluss und Bereinigung. Datei: `Rezepte.Web/Services/Import/Plugins/IPluginDirectoryRegistry.cs`. |
| `PluginDirectoryRegistry` | Klasse (Singleton) | Implementierung von `IPluginDirectoryRegistry` unter `plugins/`: `IsManagedRoot`, `LoadState` (fail-open), `SaveState` (atomar), `CreateVersionedDirectoryName` (Bereinigung ungültiger Zeichen + `@`), `TryParseVersionedDirectoryName`, `GetExcludedDirectoryNames`/`SelectEffectiveDirectory`, `MarkPending`, `CleanupMarkedDirectories`. Injiziert `IHostEnvironment` + `ILogger`. |

## Änderungen an bestehenden Klassen

### `IPluginPackageInstaller` (Interface)

- **Geänderte Methoden:** `InstallAsync` — neuer Parameter `PluginPackageInstallOptions options` zwischen `pluginDirectories` und `ct`.
- **Geänderte Methoden:** `InstallWithReloadTrackingAsync` — neuer Parameter `PluginPackageInstallOptions options`; Default-Implementierung reicht `options` an `InstallAsync` weiter.

### `PluginPackageInstaller` (Klasse)

- **Neue Abhängigkeit:** `IPluginDirectoryRegistry` (Konstruktorparameter).
- **Geänderte Methoden:** `InstallAsync` — delegiert mit `options` an `InstallWithReloadTrackingAsync`.
- **Geänderte Methoden:** `InstallWithReloadTrackingAsync` — Umstellung von In-Place-Ersetzung + Temp-Backup auf das Versionsverzeichnis-Modell (siehe Programmabläufe): Zielnamenableitung über Registry, `pendingDelete`-Markierung der Vorgänger statt Sofort-Löschung, `SaveState` statt Backup-Kopie, `replacementCompleted`-Semantik unverändert (gesetzt nach Abschluss aller Datei-/Zustandsoperationen, vor `beforeReload`). Validierung: leere Liste und unverwertbarer `ReleaseTag` weiterhin bzw. neu als frühe Ausnahmen.
- **Geänderte Methoden:** `RollBackAsync` — statt Backup-Rückkopie: Snapshot des `PluginInstallState` zurückschreiben, neue Zielverzeichnisse markieren + best-effort löschen.
- **Entfernte Elemente:** `backupRoot`-Anlage/-Aufräumung im `finally` sowie das Backup-Kopieren im Austausch-Callback.
- **Unverändert:** `InstallLock`, `CopyDirectory`, `TryDeleteDirectory`, `PluginPackageInstallException`-Statusmapping.

### `PluginUpdateService` (Klasse)

- **Geänderte Methoden:** `DownloadValidateInstallAsync` — Übergabe von `new PluginPackageInstallOptions(record.ReleaseTag, source.Id)` beim Aufruf von `InstallWithReloadTrackingAsync` (Zeile ~150). Fehlermapping und Statuspersistenz unverändert.

### `PluginManager` (Klasse)

- **Neue Abhängigkeit:** `IPluginDirectoryRegistry` (Konstruktorparameter).
- **Geänderte Methoden:** `DiscoverExternalPlugins` — je Root bei `IsManagedRoot` Ausschluss markierter (`pendingDelete`), nicht wirksamer `name@*` und überschatteter flacher Verzeichnisse vor `GetPluginAssemblyCandidates`; nicht verwaltete Roots unverändert.
- **Geänderte Methoden:** `InitializeCoreAsync` — nach erfolgreichem `SynchronizeSettingsAsync` best-effort `IPluginDirectoryRegistry.CleanupMarkedDirectories` (Fehler nur Error-Log).
- **Unverändert:** `CoordinateReloadAsync`-Signatur und Lock-Semantik (Bereinigung ergibt sich über `InitializeCoreAsync`), `SelectPreferredDescriptor`, `SynchronizeSettingsAsync`-Logik, `DiscoverFromDirectory`-Verhalten auf freien Roots.

### `ServiceCollectionExtensions` (Erweiterungsklasse)

- **Neue Registrierung:** `services.AddSingleton<IPluginDirectoryRegistry, PluginDirectoryRegistry>();` im Plugin-Block (nahe Zeile 204–218).

## Datenbankmigrationen

Keine. Der Verwaltungszustand liegt ausschließlich im Dateisystem (`plugins/.install-state.json`); `PluginStatus`/`PluginSourceReleaseStatus` und die Entity-Modelle bleiben unverändert.

## Validierungsregeln

| Feld / Objekt | Regel | Fehlerfall |
|---------------|-------|------------|
| `pluginDirectories` | Nicht leer (bestehend) | `InvalidOperationException` → `InstallFailed` |
| `PluginPackageInstallOptions.ReleaseTag` | Nicht null/leer; nach Bereinigung (ungültige Dateizeichen und `@` → `-`) nicht leer | `InvalidOperationException`/`ArgumentException` → `InstallFailed` |
| Quellverzeichnis-Basisname (`Path.GetFileName`) | Darf kein `@` enthalten und nicht leer sein | `InvalidOperationException` → `InstallFailed` (verhindert mehrdeutiges Parsen) |
| `.install-state.json` | Fehlend/korrupt → als leerer Zustand lesen, Warn-Log; niemals werfen | Kein Fehlerfall (fail-open) |
| Zielverzeichnis `name@tag` bereits vorhanden | Zuerst löschen; Löschfehler → `pendingDelete`-Eintrag, dann Ausnahme propagieren | `IOException`/`UnauthorizedAccessException` → `InstallFailed` + Markierung |
| `pendingDelete`-Einträge | Löschen best-effort; `IOException`/`UnauthorizedAccessException` → Eintrag bleibt markiert | Kein Fehlerfall (Nachbereinigung späterer Läufe) |

## Konfigurationsänderungen

Keine. Namensschema (`<Name>@<ReleaseTag>`), Zustandsdatei (`.install-state.json`) und Bereinigungsverhalten sind fest im Code verankert (Scope-Ausschluss 5).

## Seiteneffekte und Risiken

- **`PluginManager`-Konstruktor:** neuer Parameter `IPluginDirectoryRegistry` — betrifft DI (Singleton, unproblematisch) und `PluginManagerTests.CreateServices` (einzige direkte Instanziierung, `PluginManagerTests.cs:483`).
- **`IPluginPackageInstaller`-Signatur:** bricht Test-Fakes `FakePackageInstaller`/`FailingPackageInstaller` (`PluginUpdateServiceTests.cs:165-177`) — Anpassung nötig.
- **Rollback ohne Datei-Backup:** Voraussetzung ist, dass Altverzeichnisse bis zum verifizierten Reload nicht physisch gelöscht werden. Das ist durch die `pendingDelete`-Verschiebung gegeben; eine Abweichung (z. B. vorzeitiger physischer Löschversuch im Callback) würde das Rollback unterminieren — in Umsetzung und Tests abzusichern.
- **Zwei Quellen liefern denselben Plugin-Verzeichnisnamen:** Last-Writer-Wins — die zuletzt installierte Quelle ersetzt das `name@tag`-Verzeichnis bzw. den Registry-Eintrag; Entfall-Bereinigung greift nur auf Einträge mit eigenem `SourceKey`. Erkennungs-Deduplizierung bei identischer Plugin-`Id` läuft weiter über `SelectPreferredDescriptor` (unverändertes Verhalten, keine Verschlechterung).
- **Verwaiste `name@*`-Verzeichnisse ohne Registry-Eintrag** (Crash zwischen Kopieren und `SaveState`, manuelle Ablage): werden über den Versions-Maximum-Fallback bewertet; ein ungewähltes Verzeichnis wird bereinigt, ein gewähltes ggf. wirksam ohne Registry-Eintrag — begrenztes Restrisiko, in Kauf genommen (fail-open statt Datenverlust).
- **Tag-Bereinigung kann kollidieren** (`v1/2` und `v1\2` → `v1-2`): gleiche Quelle + gleicher bereinigter Name → identisches Zielverzeichnis, deterministisch überschrieben.
- **Dev-Loop:** `CopyImportPluginsToOutput` entfernt `$(OutputPath)plugins` bei jedem Build — betrifft nur das `BaseDirectory`-Root (nicht `IsManagedRoot`), Verhalten identisch zu heute.
- **Einzelinstanz-Annahme:** parallele App-Instanzen auf demselben `plugins/` würden die Zustandsdatei racen — wie bisher bei SQLite/Datei-Layout implizit angenommen.
- **`SynchronizeSettingsAsync`/`Missing`:** funktioniert unverändert, da die Plugin-`Id` aus der Assembly (nicht aus dem Verzeichnisnamen) stammt — Versionswechsel behält `Enabled`/`OrderIndex`.
- **Bestehender Quirk bleibt:** `OperationCanceledException` im Austausch-Callback wird wie bisher zu `InstallFailed` verpackt (unverändertes beobachtbares Verhalten).

## Umsetzungsreihenfolge

1. **Datenmodellklassen anlegen:** `PluginPackageInstallOptions`, `PluginInstallState`, `PluginInstallEntry`
   - Voraussetzungen: Keine.
   - Beschreibung: Records/POCOs für Installer-Optionen und das Zustandsdatei-Schema (inkl. `Version = 1`).

2. **`IPluginDirectoryRegistry` + `PluginDirectoryRegistry` implementieren**
   - Voraussetzungen: Schritt 1.
   - Beschreibung: Namensableitung/-bereinigung, Parser (`TryParseVersionedDirectoryName`), `LoadState`/`SaveState` (atomar, fail-open), `IsManagedRoot`, Exklusions-/Auswahllogik (`pendingDelete`, `name@*`-Gruppen, Schattenregel), `CleanupMarkedDirectories`; interne Serialisierung der Lade-Ändere-Speichere-Sequenz.

3. **DI-Registrierung**
   - Voraussetzungen: Schritt 2.
   - Beschreibung: `AddSingleton<IPluginDirectoryRegistry, PluginDirectoryRegistry>()` in `ServiceCollectionExtensions`.

4. **`IPluginPackageInstaller`-Signatur + `PluginPackageInstaller`-Umstellung**
   - Voraussetzungen: Schritte 1–3.
   - Beschreibung: `options`-Parameter in Interface inkl. Default-Implementierung und Klasse; Austausch-Callback auf Versionsverzeichnis-Modell (Zielname, Vorgänger-`pendingDelete`, `SaveState`, `beforeReload`), Rollback auf Snapshot-Basis, Backup-Pfad entfernen.

5. **`PluginUpdateService.DownloadValidateInstallAsync` anpassen**
   - Voraussetzungen: Schritt 4.
   - Beschreibung: `PluginPackageInstallOptions(record.ReleaseTag, source.Id)` übergeben.

6. **`PluginManager` anpassen**
   - Voraussetzungen: Schritte 2–3.
   - Beschreibung: Konstruktorparameter, Erkennungs-Ausschluss unter `IsManagedRoot`, `CleanupMarkedDirectories` am Ende erfolgreicher `InitializeCoreAsync`-Läufe.

7. **Bestehende Tests anpassen**
   - Voraussetzungen: Schritte 4–6.
   - Beschreibung: `PluginPackageInstallerTests` (Signatur + versionsbezogene Assertions), `PluginUpdateServiceTests`-Fakes (Signatur), `PluginManagerTests.CreateServices` (Konstruktorargument).

8. **Neue Tests schreiben**
   - Voraussetzungen: Schritte 2–7.
   - Beschreibung: `PluginDirectoryRegistryTests` sowie neue Fälle in `PluginPackageInstallerTests`, `PluginManagerTests`, `PluginUpdateServiceTests` gemäß Testabschnitt.

9. **Vollständiger Testlauf**
   - Voraussetzungen: Schritte 1–8.
   - Beschreibung: `dotnet test Rezepte.sln` — Plugin-Bereich muss grün sein; bekannter preexisting Browser-Testfehler (`RegisterTests.Register_WithDemoData_CreatesDemoData`) ist kein Regressionskriterium.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `CreateVersionedDirectoryName_ShouldSanitizeInvalidCharacters` | `PluginDirectoryRegistryTests` (neu, `Rezepte.Tests/Services/Import/`) | Tag `v1/2` o. ä. → `Name@v1-2`; `@` im Tag → `-` |
| `TryParseVersionedDirectoryName_ShouldSplitOnFirstAt` | `PluginDirectoryRegistryTests` | `Name@v1.0` → Basis `Name`, Tag `v1.0`; Namen ohne `@` → `false` |
| `LoadState_ShouldReturnEmptyState_WhenFileMissingOrCorrupt` | `PluginDirectoryRegistryTests` | Fehlende/korrupte `.install-state.json` → leerer Zustand, kein Wurf |
| `SaveState_ShouldRoundtrip` | `PluginDirectoryRegistryTests` | Gespeicherter Zustand über neue Registry-Instanz wieder lesbar (Persistenz über „Neustart") |
| `CleanupMarkedDirectories_ShouldDeletePendingAndKeepLocked` | `PluginDirectoryRegistryTests` | Markiertes Verzeichnis wird entfernt; gesperrtes Verzeichnis bleibt in `pendingDelete` |
| `GetExcludedDirectoryNames_ShouldExcludePendingNonCurrentAndShadowed` | `PluginDirectoryRegistryTests` | `pendingDelete`-Eintrag, nicht aktuelles `name@*`, überschattetes Flachverzeichnis → ausgeschlossen; Publish-/Flachverzeichnis ohne `@`-Geschwister → einbezogen |
| `SelectEffectiveDirectory_ShouldPreferRegistryThenVersionMax` | `PluginDirectoryRegistryTests` | Registry-`current` schlägt höheres Tag-Verzeichnis; ohne Eintrag Versions-Maximum |
| `InstallAsync_ShouldInstallIntoVersionedDirectory` | `PluginPackageInstallerTests` | Nach `InstallAsync(dirs, options("v1.0","src"))` existiert `plugins/PluginA@v1.0` mit Inhalt; `.install-state.json` enthält Eintrag |
| `InstallAsync_ShouldMarkPredecessorPendingAndRemoveAfterSuccess` | `PluginPackageInstallerTests` | `PluginA@v0.9` vorhanden → nach erfolgreichem Install `PluginA@v1.0` wirksam, `v0.9` physisch entfernt (echte Registry-Bereinigung) bzw. `pendingDelete` im Stub-Pfad |
| `InstallAsync_ShouldMarkDroppedPluginOfSameSourcePending` | `PluginPackageInstallerTests` | Eintrag mit `SourceKey = S`, Basisname nicht im neuen Set → `pendingDelete` + Registry-Eintrag entfernt; Eintrag fremder `SourceKey` bleibt |
| `InstallAsync_ShouldKeepPreviousVersionEffective_WhenReloadFails` | `PluginPackageInstallerTests` | `FailingPluginManager` → `ReloadFailed`; `PluginA@v0.9` liegt weiterhin unverändert auf Datenträger und ist im Zustand wieder wirksam; neues `PluginA@v1.0` entfernt/markiert |
| `InstallAsync_ShouldRestoreState_WhenInstallFailsBeforeReplacement` | `PluginPackageInstallerTests` | Copy-Fehler (gelockte Quelldatei) → `InstallFailed`; kein wirksamer Registry-Eintrag; Teil-Verzeichnis entfernt bzw. markiert |
| `InitializeAsync_ShouldExcludePendingDeleteDirectories` | `PluginManagerTests` | `pendingDelete`-Eintrag → Plugin wird nicht erkannt, kein `Loaded`-Eintrag |
| `InitializeAsync_ShouldSelectCurrentVersionedDirectory` | `PluginManagerTests` | Zwei `Name@*`-Verzeichnisse → nur das wirksame wird erkannt (Fixture-Plugin in beide legen, Assertion auf `PluginSetting`-Status/Assembly) |
| `InitializeAsync_ShouldShadowFlatDirectory_WhenVersionedExists` | `PluginManagerTests` | `plugins/PluginX/` + `plugins/PluginX@v1/` → nur Version erkannt |
| `InitializeAsync_ShouldCleanPendingDirectoriesOnSuccessfulInit` | `PluginManagerTests` | Markiertes Verzeichnis ist nach `InitializeAsync` physisch entfernt, Eintrag aus Zustandsdatei verschwunden |
| `InstallAsync_ShouldKeepFlatDirectory_WhenVersionedInstallShadowsIt` | `PluginPackageInstallerTests` o. `PluginManagerTests` | Flaches `plugins/PluginX/` bleibt nach Installation von `PluginX@v1` physisch bestehen (Scope-Ausschluss 6) |
| `CheckForUpdatesAsync_ShouldPassReleaseTagAndSourceKey` | `PluginUpdateServiceTests` | `FakePackageInstaller` zeichnet `options` auf → `ReleaseTag == release.TagName`, `SourceKey == source.Id` |
| `TempWorkspace`/`PluginWorkspace`-Erweiterung: `WriteStateFile`, `CreateVersionedPluginDir`, `LockFile` | jeweilige Testklasse | Hilfsmethoden für Zustandsdatei-Fixtures, Versionsverzeichnisse und Dateisperrung |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `PluginPackageInstallerTests.InstallAsync_ShouldRestoreBackupAndReinitializeAfterReloadFailure` | Neues Rollback-Modell (kein Temp-Backup); Signaturänderung (`options`); Assertions auf `PluginA@v*` statt `plugins/PluginA` — wird zu `...KeepPreviousVersionEffective_WhenReloadFails` überführt |
| `PluginPackageInstallerTests.InstallAsync_ShouldRemovePartiallyCopiedNewTargetAfterCopyFailure` | Zielname versioniert (`PluginB@tag`); Signaturänderung |
| `PluginUpdateServiceTests` — `FakePackageInstaller`, `FailingPackageInstaller` | `InstallAsync`-Signatur um `PluginPackageInstallOptions` erweitert |
| `PluginManagerTests` — `CreateServices` | `PluginManager`-Konstruktor erhält `IPluginDirectoryRegistry` (echte Instanz über `TestHostEnvironment`) |

### E2E-Tests (primärer Funktionsnachweis)

Keine E2E-Tests erforderlich: Die Anforderung berührt keinen über UI oder Nutzeraktion erreichbaren Ablauf. Der einzige Auslöser ist die automatische Startprüfung (`PluginUpdateHostedService`), ein manueller Update-Trigger ist expliziter Scope-Ausschluss. Der wirksame Ablauf (Dateiaustausch, Markierung, Erkennungs-Ausschluss, Bereinigung, Statuspersistenz) ist vollständig serverseitig und dateisystembasiert; er wird durch Integrationstests mit echten Temp-Verzeichnissen (`PluginManager` + `PluginPackageInstaller` + `PluginUpdateService` über `RezepteDbContext` In-Memory) nachgewiesen — ein Browser-E2E könnte denselben Pfad nicht zusätzlich auslösen oder sichtbar machen.

Keine bestehenden E2E-Tests betroffen (kein UI-Bestandteil ändert sich).

## Akzeptanzszenarien

| # | Szenario | Auslöser | Ausgangszustand | Schritte | Sichtbare Wirkung | Nachweis |
|---|----------|----------|------------------|----------|-------------------|----------|
| 1 | Happy Path: Release-Installation in Versionsverzeichnis | Anwendungsstart → `PluginUpdateHostedService` → `CheckForUpdatesAsync` | Quelle `Enabled && TrustConfirmed`, neues Release verfügbar; `plugins/` enthält Vorgänger `PluginA@v0.9` + Registry-Eintrag; `.install-state.json` existiert (oder fehlt bei Erstinstallation → wird erzeugt) | Download → Validierung → `InstallWithReloadTrackingAsync` mit `options("v1.0", source.Id)` → Austausch-Callback (Vorgänger → `pendingDelete`, Zustand gespeichert) → `InitializeCoreAsync` → Bereinigung | `plugins/PluginA@v1.0` vorhanden und einzig wirksames Verzeichnis von `PluginA`; `PluginA@v0.9` entfernt; `PluginSetting` bleibt `Loaded` mit gleicher `PluginId`; `PluginSourceRelease.Status = Installed`, `LastSuccessfulReleaseTag` gesetzt | Integrationstest `InstallAsync_ShouldMarkPredecessorPendingAndRemoveAfterSuccess` + `CheckForUpdatesAsync_ShouldPassReleaseTagAndSourceKey` |
| 2 | Nicht löschbares Altverzeichnis | Koordinierter Austausch, `Directory.Delete` auf `PluginA@v0.9` scheitert (`IOException`) | Wie #1, zusätzlich Datei-Handle auf `PluginA@v0.9` | Austausch-Callback markiert `PluginA@v0.9` in `pendingDelete`; Erkennung schließt es aus; Bereinigung am Ende von `InitializeCoreAsync` scheitert erneut → Markierung bleibt | `PluginA@v1.0` wirksam, `PluginA@v0.9` physisch vorhanden aber unwirksam (nicht `Loaded`, kein Handler); `.install-state.json` enthält `pendingDelete`-Eintrag | `CleanupMarkedDirectories_ShouldDeletePendingAndKeepLocked`, `InitializeAsync_ShouldExcludePendingDeleteDirectories` |
| 3 | Nachbereinigung beim nächsten Start | Anwendungsstart (`PluginStartupService` → `InitializeAsync`) | `pendingDelete` enthält `PluginA@v0.9` aus Lauf #2; Verzeichnis nun entsperrt | `InitializeCoreAsync`: Erkennung (ohne markiertes Verzeichnis) → Settings-Sync → `CleanupMarkedDirectories` | `PluginA@v0.9` physisch entfernt; `pendingDelete` leer; wirksamer Bestand unverändert | `InitializeAsync_ShouldCleanPendingDirectoriesOnSuccessfulInit` |
| 4 | Reload-Fehler: Vorbestand bleibt wirksam | Fehler in `InitializeCoreAsync` nach erfolgreichem Austausch (z. B. Discovery-Ausnahme) | Wie #1; `beforeReload` wurde ausgeführt (`replacementCompleted = true`) | Installer-`catch` → `RollBackAsync` → zweiter `CoordinateReloadAsync`: Snapshot zurückschreiben + neues Verzeichnis markieren/löschen → `InitializeCoreAsync` erkennt `PluginA@v0.9` erneut | `PluginPackageInstallException.Status = ReloadFailed`; `PluginSourceRelease.Status/ReloadStatus = ReloadFailed`; `PluginA@v0.9` weiterhin wirksam, `PluginA@v1.0` entfernt/markiert | `InstallAsync_ShouldKeepPreviousVersionEffective_WhenReloadFails`, `CheckForUpdatesAsync_ShouldPersistReloadFailureSeparately` (angepasst) |
| 5 | Plugin entfällt im neuen Release | Startprüfung installiert Release `v2.0` der Quelle S, das `PluginB` nicht mehr enthält | Registry enthält `PluginB@v1.0` mit `SourceKey = S`; `PluginSetting` für `PluginB` existiert (`Loaded`) | Austausch-Callback: `PluginB@v1.0` → `pendingDelete`, Registry-Eintrag entfernt → Erkennung ohne `PluginB` → `SynchronizeSettingsAsync` | `PluginSetting.PluginB.Status = Missing` mit `"Plugin was not found during startup discovery."`; `PluginB@v1.0` bereinigt | `InstallAsync_ShouldMarkDroppedPluginOfSameSourcePending` + `InitializeAsync_ShouldMarkPreviouslyConfiguredPluginAsMissing` (bestehend) |
| 6 | Publish-Plugin unberührt | Startprüfung installiert beliebiges Release | `plugins/Rezepte.Import.Plugins.Backup/` vorhanden, kein `@`-Namensgeschwister | Erkennung filtert nur markierte/`@`-/überschattete Verzeichnisse | `Rezepte.Import.Plugins.Backup` bleibt erkannt (`Loaded`) und physisch unverändert | `GetExcludedDirectoryNames_ShouldExcludePendingNonCurrentAndShadowed`, `InitializeAsync_ShouldDiscoverProductiveExternalImportPlugins` (bestehend) |
| 7 | Installationsfehler vor Austausch-Abschluss | Copy-Fehler (gelockte Quelldatei) im Austausch-Callback | Wie #1, `replacementCompleted = false` | Ausnahme → Rollback: Zustand unverändert/Snapshot, Teil-Verzeichnis entfernt bzw. markiert → `InitializeCoreAsync` | `PluginPackageInstallException.Status = InstallFailed`; Vorbestand wirksam; kein wirksamer Registry-Eintrag für die neue Version | `InstallAsync_ShouldRestoreState_WhenInstallFailsBeforeReplacement` |

Wartesituationen/asynchrone Übergänge: Der Update-Check läuft einmalig beim Start im `PluginUpdateHostedService`; während `CoordinateReloadAsync` sind neue Imports über `_lifecycleLock` gesperrt und werden nach Abschluss mit dem neuen Bestand fortgesetzt (bestehender Mechanismus). Während des Laufs sind die Zwischenstati (`Downloading`/`Validating`/`Installing`/`Reloading`) auf dem `PluginSourceRelease` persistiert; ein abgebrochener Lauf hinterlässt einen konsistenten Zustand (Versionsverzeichnis erst nach `SaveState` wirksam, Markierungen werden nächsten Lauf nachbereinigt).

## Offene Punkte

Keine — die in der Anforderung gestellten Umsetzungsfragen (Namensschema, Markierungsform, Identifikation verwalteter Verzeichnisse, Mehrquellen-Verhalten, Rollback-Detail, Bereinigungszeitpunkt) sind als Designentscheidungen oben festgelegt; nutzerrelevante Einschränkungen sind als Scope-Ausschlüsse mit Status `bestätigt` abgeschlossen.
