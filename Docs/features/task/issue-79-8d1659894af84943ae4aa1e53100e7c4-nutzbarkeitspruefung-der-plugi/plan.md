# Umsetzungsplan: Nutzbarkeitsprüfung der Plugins

## Übersicht

Import-Plugins erhalten eine Laufzeit-Prüfmethode, die meldet, ob ein Plugin einsatzbereit ist, und bei Nicht-Nutzbarkeit strukturierte Fehlerursachen samt Lösungshinweisen liefert (z. B. fehlende KI-Credentials, deaktivierte globale Schalter). Betroffen sind das Abstraktions-Interface `IImportPlugin`, der `PluginManager`, das DTO `PluginSettingsItem`, der `PluginSettingsService` sowie die Admin-Komponente `PluginSettings.razor`. Die KI-Plugins `AIFotoImportPlugin` und `AIUrlImportPlugin` implementieren die Prüfung; alle übrigen Plugins gelten über eine Default-Implementierung als nutzbar.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Prüfmethode auf `IImportPlugin` | **Default Interface Method** `CheckUsabilityAsync(IServiceProvider, CancellationToken)` mit Standardrückgabe „nutzbar" | Das Abstraktions-Assembly wird zur Laufzeit gemeinsam (shared) geladen (siehe `PluginLoadContext.Load`). Eine Default-Implementierung hält bereits kompilierte externe Plugins (Backup u. a.) binärkompatibel und erspart ihnen eine Anpassung; nur KI-Plugins überschreiben die Methode. |
| Ergebnis-Datenmodell | **Value Object** `PluginUsabilityResult` mit `IsUsable` + Liste `PluginUsabilityIssue(Message, Hint)` | Ein zusammengesetztes Issue-Objekt (Meldung + Lösungshinweis pro Ursache) ist klarer als zwei parallele Listen (`Errors`/`HelpText`) und hält Meldung und Hinweis paarweise zusammen. Unveränderliches Record ohne Identität → Value Object. |
| Ort der Prüflogik | Prüfung liegt **im Plugin** (`IImportPlugin`-Implementierung), resolved Dienste selbst aus dem übergebenen `IServiceProvider` | Folgt der Anforderung („Plugin prüft eigenverantwortlich"). Der `PluginManager` instanziiert die Plugin-Klasse (parameterlos, wie bei der Discovery) und ruft die Prüfmethode auf. Handler-Instanziierung (mit DI) ist dafür nicht nötig. |
| Prüf-Scope | **Nur globale/administrative Voraussetzungen** (Credentials vorhanden, globale KI-Schalter aktiv). Keine benutzerspezifischen Einstellungen. | Die Admin-UI ist benutzerübergreifend; benutzerbezogene Schalter (`GetUserGeminiEnabledAsync`) gehören nicht in die administrative Nutzbarkeitsanzeige. Die bestehende handler-seitige `IsActiveAsync` bleibt für die benutzerspezifische Laufzeitprüfung zuständig. |
| Prüffrequenz / Persistenz | **Live-Berechnung** bei jedem Laden der Plugin-Liste; **keine** Persistenz in `PluginSetting`, kein Hintergrund-Service | Prüfungen sind lokal und günstig (DB-Settings, Dateisystem, In-Memory-Credential-Check) und laufen nur beim seltenen Öffnen der Admin-Seite. Das erspart eine Migration und liefert stets aktuelle Ergebnisse. |
| Netzwerkverhalten der Prüfung | Prüfungen führen **keine** externen Netzwerkaufrufe aus (kein Live-Ping gegen Gemini/Vision) | Verhindert Blockieren der Admin-UI und Kosten/Latenz; geprüft wird nur die Konfiguration (Schlüssel/Datei vorhanden, Schalter aktiv). |
| Zusammenführung DTO ↔ Prüfergebnis | `PluginSettingsService.GetPluginsAsync` reichert die DB-Projektion nach Abruf über `IPluginManager.GetPluginsUsabilityAsync` an | Die UI erhält Nutzbarkeit im selben DTO; die bestehende Service-Grenze (Service liefert `PluginSettingsItem` an die Komponente) bleibt erhalten. |
| Sprache der Meldungen | Feste **englische** Meldungs- und Hinweistexte (nicht lokalisiert) | CLAUDE.md: nicht über einen Localizer geführte Fehler-/Statusmeldungen bleiben englisch. |
| Plugin-Abhängigkeiten | Abhängigkeiten zwischen Plugins werden **nicht** in der Nutzbarkeitsprüfung berücksichtigt | Entschieden: außerhalb des Scopes dieser Anforderung. Es existieren aktuell keine Plugin-Abhängigkeiten; jedes Plugin prüft ausschließlich seine eigenen globalen Voraussetzungen. |

## Programmabläufe

### Nutzbarkeit beim Laden der Plugin-Liste ermitteln

1. Die Admin-Komponente `PluginSettings.razor` ruft in `LoadAsync` `PluginSettingsService.GetPluginsAsync` auf.
2. `GetPluginsAsync` projiziert wie bisher die `PluginSetting`-Entities in `PluginSettingsItem`-DTOs.
3. Anschließend ruft `GetPluginsAsync` `IPluginManager.GetPluginsUsabilityAsync(serviceProvider, ct)` mit dem scoped `IServiceProvider` auf.
4. `PluginManager.GetPluginsUsabilityAsync` iteriert über die geladenen Descriptor (`_loadedPlugins`), instanziiert für jeden über den gespeicherten `PluginType` die `IImportPlugin`-Implementierung (parameterlos) und ruft `CheckUsabilityAsync(serviceProvider, ct)` auf.
5. Wirft eine Plugin-Prüfung eine Ausnahme, wird sie abgefangen, protokolliert und als nicht nutzbares Ergebnis mit generischer Fehlermeldung gewertet (kein Abbruch der Gesamtprüfung).
6. `GetPluginsUsabilityAsync` liefert ein `IReadOnlyDictionary<string, PluginUsabilityResult>` (Schlüssel = Plugin-Id) zurück.
7. `GetPluginsAsync` setzt für jedes DTO das passende `Usability`-Ergebnis; Plugins ohne Eintrag (nicht geladen/deaktiviert) erhalten `null`.
8. Die Komponente rendert unterhalb des Status-Badges einen Fehlerbereich, sofern `Usability` vorhanden und `IsUsable == false` ist, und listet je `PluginUsabilityIssue` Meldung und Hinweis auf.

Beteiligte Klassen/Komponenten: `PluginSettings.razor`, `PluginSettingsService`, `IPluginManager`, `PluginManager`, `IImportPlugin`, `PluginUsabilityResult`, `PluginUsabilityIssue`

### Nutzbarkeitsprüfung eines KI-Plugins

1. `PluginManager` ruft `AIFotoImportPlugin.CheckUsabilityAsync(serviceProvider, ct)` bzw. `AIUrlImportPlugin.CheckUsabilityAsync(serviceProvider, ct)` auf.
2. Das Plugin resolved die benötigten Dienste aus dem `serviceProvider` (`ISettingsService`, `IGeminiClient`, für AIFoto zusätzlich `IGoogleCredentialsProvider`).
3. Das Plugin prüft der Reihe nach die globalen Voraussetzungen und sammelt für jede fehlgeschlagene Bedingung ein `PluginUsabilityIssue`:
   - AIUrl: globales KI aktiv (`GetGlobalAiEnabledAsync`), Gemini-Authentifizierung vorhanden (`IGeminiClient.HasApiKey()`/`HasServiceAccount()`), globales Gemini aktiv (`GetGlobalGeminiEnabledAsync`).
   - AIFoto: zusätzlich Vision-Service-Account-Datei vorhanden (`IGoogleCredentialsProvider.GetDiagnostics().ServiceAccountFileExists`) und globales Google Vision aktiv (`GetGlobalGoogleVisionEnabledAsync`).
4. Sind alle Bedingungen erfüllt, gibt das Plugin `PluginUsabilityResult` mit `IsUsable == true` und leerer Issue-Liste zurück; andernfalls `IsUsable == false` mit den gesammelten Issues.

Beteiligte Klassen/Komponenten: `AIFotoImportPlugin`, `AIUrlImportPlugin`, `ISettingsService`, `IGeminiClient`, `IGoogleCredentialsProvider`, `PluginUsabilityResult`, `PluginUsabilityIssue`

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `PluginUsabilityResult` | Datenmodellklasse (sealed record, in `Rezepte.Import.Abstractions`) | Ergebnis einer Nutzbarkeitsprüfung: `IsUsable` (bool) und `Issues` (Liste `PluginUsabilityIssue`); statische Convenience-Instanz für „nutzbar". |
| `PluginUsabilityIssue` | Datenmodellklasse (sealed record, in `Rezepte.Import.Abstractions`) | Einzelne Fehlerursache: `Message` (string) und `Hint` (string?). |

## Änderungen an bestehenden Klassen

### `IImportPlugin` (Interface, `Rezepte.Import.Abstractions`)

- **Neue Methoden:** `CheckUsabilityAsync(IServiceProvider serviceProvider, CancellationToken ct = default)` — Default Interface Method mit Rückgabe `Task<PluginUsabilityResult>`; Standard-Implementierung liefert ein nutzbares Ergebnis (`IsUsable == true`, keine Issues). Plugins mit externen Voraussetzungen überschreiben sie.

### `ImportPluginDescriptor` (Record, `Rezepte.Web`)

- **Neue Eigenschaften:** `PluginType` (`Type?`) — Typ der `IImportPlugin`-Implementierung, um das Plugin für die Nutzbarkeitsprüfung erneut instanziieren zu können (analog zum bestehenden `HandlerType`; `null` bei collectible/entladenen Discoveries).

### `PluginManager` (Klasse, `Rezepte.Web`)

- **Geänderte Methoden:** `DiscoverFromAssembly` — setzt beim Erzeugen der Loaded-Descriptor den neuen `PluginType` (bzw. `null` bei `useCollectibleLoadContext`); die übrigen Descriptor-Konstruktionen (`FailedDescriptor`, Incompatible-Zweig) übergeben `null`.
- **Neue Methoden:** `GetPluginsUsabilityAsync(IServiceProvider serviceProvider, CancellationToken ct = default)` — liefert `Task<IReadOnlyDictionary<string, PluginUsabilityResult>>`; instanziiert je geladenem Plugin die `IImportPlugin`-Implementierung, ruft `CheckUsabilityAsync` auf, fängt Ausnahmen ab und aggregiert die Ergebnisse.

### `IPluginManager` (Interface, `Rezepte.Web`)

- **Neue Methoden:** `GetPluginsUsabilityAsync(IServiceProvider serviceProvider, CancellationToken ct = default)` mit `Task<IReadOnlyDictionary<string, PluginUsabilityResult>>`; Default-Implementierung liefert ein leeres Dictionary (für Fakes/alternative Manager).

### `PluginSettingsItem` (Record/DTO, `Rezepte.Web`)

- **Neue Eigenschaften:** `Usability` (`PluginUsabilityResult?`) — Nutzbarkeitsergebnis für das Plugin; `null`, wenn keine Prüfung vorliegt (Plugin nicht geladen/deaktiviert).

### `PluginSettingsService` (Klasse, `Rezepte.Web`)

- **Neue Konstruktorabhängigkeiten:** `IPluginManager` und `IServiceProvider` (scoped) — um die Nutzbarkeit live zu berechnen.
- **Geänderte Methoden:** `GetPluginsAsync` — reichert die projizierten DTOs nach dem DB-Abruf über `IPluginManager.GetPluginsUsabilityAsync` mit dem `Usability`-Ergebnis an.

### `AIFotoImportPlugin` (Klasse, `Rezepte.Import.Plugins.AIFoto`)

- **Neue Methoden:** Überschreibt `CheckUsabilityAsync` — prüft globales KI, Vision-Service-Account-Datei, Gemini-Authentifizierung, globales Google Vision und globales Gemini; erzeugt je fehlender Voraussetzung ein `PluginUsabilityIssue` mit englischem Meldungs- und Hinweistext.

### `AIUrlImportPlugin` (Klasse, `Rezepte.Import.Plugins.AIUrl`)

- **Neue Methoden:** Überschreibt `CheckUsabilityAsync` — prüft globales KI, Gemini-Authentifizierung und globales Gemini; erzeugt je fehlender Voraussetzung ein `PluginUsabilityIssue`.

### `PluginSettings.razor` (Komponente, `Rezepte.Web`)

- **Geänderte Darstellung:** Ergänzt in der Plugin-Tabelle unterhalb des Status-Badges einen Nutzbarkeitsbereich, der bei `plugin.Usability is { IsUsable: false }` je Issue Meldung (`text-danger`) und Hinweis (`text-muted`) rendert. Optional zusätzlich ein „nicht nutzbar"-Badge neben dem Status.

## Datenbankmigrationen

Keine. Die Nutzbarkeit wird live berechnet und nicht persistiert.

## Validierungsregeln

Keine. Es werden keine neuen Benutzereingaben entgegengenommen; die Prüfung liest ausschließlich vorhandene Konfiguration.

## Konfigurationsänderungen

Keine. Die Prüfung nutzt bestehende globale KI-/Vision-Einstellungen und Credential-Quellen.

## Seiteneffekte und Risiken

- **`IImportPlugin`-Erweiterung:** Neue Methode ist als Default Interface Method binärkompatibel; bereits veröffentlichte externe Plugins bleiben ohne Neukompilierung nutzbar (Standardergebnis „nutzbar").
- **`ImportPluginDescriptor`-Erweiterung:** Zusätzlicher `PluginType`-Parameter im Record-Konstruktor betrifft alle Konstruktionsstellen im `PluginManager` und ggf. Tests, die den Descriptor direkt erzeugen.
- **`PluginSettingsService`-Abhängigkeiten:** Neuer Konstruktor mit `IPluginManager`/`IServiceProvider` betrifft die DI-Registrierung und bestehende Service-Tests (Konstruktoraufrufe).
- **`GetActiveHandlersAsync`/Discovery:** Keine funktionale Änderung; die Nutzbarkeitsprüfung greift nicht in Discovery/Handler-Erstellung ein.
- **Plugin-Load-Context:** Die Prüfung resolved `Rezepte.Web`-Dienste aus dem Host-Provider; für geladene (nicht-collectible) Plugins ist dies wie bei der Handler-Instanziierung unproblematisch.

## Umsetzungsreihenfolge

1. **`PluginUsabilityIssue` anlegen**
   - Voraussetzungen: Keine.
   - Beschreibung: Record in `Rezepte.Import.Abstractions` mit `Message` und `Hint`.

2. **`PluginUsabilityResult` anlegen**
   - Voraussetzungen: `PluginUsabilityIssue`.
   - Beschreibung: Record in `Rezepte.Import.Abstractions` mit `IsUsable` und `Issues` sowie statischer „nutzbar"-Instanz.

3. **`IImportPlugin` um `CheckUsabilityAsync` erweitern**
   - Voraussetzungen: `PluginUsabilityResult`.
   - Beschreibung: Default Interface Method mit Standardrückgabe „nutzbar" hinzufügen.

4. **`ImportPluginDescriptor` um `PluginType` erweitern**
   - Voraussetzungen: Keine.
   - Beschreibung: Neue `Type?`-Eigenschaft ergänzen.

5. **`PluginManager`-Descriptor-Erzeugung anpassen**
   - Voraussetzungen: Schritt 4.
   - Beschreibung: In `DiscoverFromAssembly` (und übrigen Descriptor-Konstruktionen) den `PluginType` setzen bzw. `null` übergeben.

6. **`IPluginManager`/`PluginManager` um `GetPluginsUsabilityAsync` erweitern**
   - Voraussetzungen: Schritte 2, 3, 5.
   - Beschreibung: Methode zur Live-Berechnung der Nutzbarkeit über geladene Plugins (mit Ausnahmebehandlung) hinzufügen; Default-Implementierung im Interface liefert leeres Dictionary.

7. **`PluginSettingsItem` um `Usability` erweitern**
   - Voraussetzungen: Schritt 2.
   - Beschreibung: Optionales `PluginUsabilityResult`-Feld ergänzen.

8. **`PluginSettingsService.GetPluginsAsync` anreichern**
   - Voraussetzungen: Schritte 6, 7.
   - Beschreibung: `IPluginManager`/`IServiceProvider` injizieren und die DTOs mit dem Nutzbarkeitsergebnis anreichern.

9. **`AIUrlImportPlugin.CheckUsabilityAsync` implementieren**
   - Voraussetzungen: Schritte 2, 3.
   - Beschreibung: Globale KI-/Gemini-Voraussetzungen prüfen und Issues erzeugen.

10. **`AIFotoImportPlugin.CheckUsabilityAsync` implementieren**
    - Voraussetzungen: Schritte 2, 3.
    - Beschreibung: Globale KI-/Vision-/Gemini-Voraussetzungen prüfen und Issues erzeugen.

11. **`PluginSettings.razor` um Nutzbarkeitsanzeige erweitern**
    - Voraussetzungen: Schritte 7, 8.
    - Beschreibung: Fehlerbereich unterhalb des Status-Badges rendern.

12. **Tests ergänzen**
    - Voraussetzungen: Schritte 1–11.
    - Beschreibung: Unit- und Integrationstests laut Abschnitt „Tests" schreiben; betroffene bestehende Tests anpassen.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `CheckUsabilityAsync_ShouldReturnUsable_WhenAllGlobalPrerequisitesMet` | `AIUrlImportPluginTests` (neu) | AIUrl meldet nutzbar, wenn globales KI/Gemini aktiv und Gemini-Auth vorhanden. |
| `CheckUsabilityAsync_ShouldReportMissingGeminiAuthentication` | `AIUrlImportPluginTests` (neu) | Fehlende Gemini-Authentifizierung erzeugt Issue mit Hinweis. |
| `CheckUsabilityAsync_ShouldReportDisabledGlobalGemini` | `AIUrlImportPluginTests` (neu) | Deaktivierter globaler Gemini-Schalter erzeugt Issue. |
| `CheckUsabilityAsync_ShouldReportMissingVisionServiceAccount` | `AIFotoImportPluginTests` (neu) | Fehlende Vision-Service-Account-Datei erzeugt Issue. |
| `CheckUsabilityAsync_ShouldReportDisabledGlobalVision` | `AIFotoImportPluginTests` (neu) | Deaktiviertes globales Google Vision erzeugt Issue. |
| `CheckUsabilityAsync_ShouldReturnUsable_WhenAllGlobalPrerequisitesMet` | `AIFotoImportPluginTests` (neu) | AIFoto meldet nutzbar bei vollständiger Konfiguration. |
| `DefaultCheckUsabilityAsync_ShouldReturnUsable_ForPluginWithoutOverride` | `PluginManagerTests` | Ein Plugin ohne Override (z. B. Backup) gilt als nutzbar. |
| `GetPluginsUsabilityAsync_ShouldReturnResultsForLoadedPlugins` | `PluginManagerTests` | Live-Prüfung liefert Ergebnisse je geladenem Plugin. |
| `GetPluginsUsabilityAsync_ShouldTreatCheckExceptionAsNotUsable` | `PluginManagerTests` | Ausnahme in `CheckUsabilityAsync` → nicht nutzbares Ergebnis, kein Gesamtabbruch. |
| `GetPluginsAsync_ShouldPopulateUsabilityForLoadedPlugins` | `PluginSettingsServiceTests` | DTO enthält Nutzbarkeitsergebnis aus dem `PluginManager`. |
| Fake-Plugin mit überschreibbarem `CheckUsabilityAsync` (Hilfstyp) | `PluginManagerTests` / Testfixtures | Stellt ein Plugin mit steuerbarem Nutzbarkeitsergebnis bereit. |
| Fake `IPluginManager` mit steuerbarem `GetPluginsUsabilityAsync` (Hilfstyp) | `PluginSettingsServiceTests` | Liefert deterministische Nutzbarkeitsergebnisse für den Service-Test. |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `PluginSettingsServiceTests` | `PluginSettingsService`-Konstruktor erhält neue Abhängigkeiten (`IPluginManager`, `IServiceProvider`); Setup muss diese bereitstellen. |
| `PluginManagerTests` | Falls Tests `ImportPluginDescriptor` direkt konstruieren, ist der neue `PluginType`-Parameter zu ergänzen. |

### E2E-Tests (Pflicht)

Im Repository existiert kein Browser-/UI-E2E-Harness (kein Playwright, kein bUnit). Der Happy Path wird daher durch einen dienstübergreifenden Integrationstest abgedeckt, der `PluginSettingsService` → `PluginManager` → Plugin durchläuft.

| Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium |
|----------|------------------------|-------------------------------|
| Admin lädt Plugin-Liste; nicht nutzbares KI-Plugin liefert Nutzbarkeitsstatus mit Fehlerursache und Hinweis | `PluginSettingsServiceTests.GetPluginsAsync_ShouldExposeUsabilityIssuesForMisconfiguredAiPlugin` (neu) | Nutzbarkeit inkl. Fehlerursachen/Hinweisen wird für die UI bereitgestellt |
| Admin lädt Plugin-Liste; vollständig konfiguriertes Plugin ist nutzbar (keine Issues) | `PluginSettingsServiceTests.GetPluginsAsync_ShouldReportUsableForFullyConfiguredPlugin` (neu) | Nutzbare Plugins zeigen keinen Fehlerbereich |

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| Keine. | — |

## Offene Punkte

Keine.

Die zuvor offenen Punkte wurden entschieden und sind in den Plan eingearbeitet:

- **Prüffrequenz/Persistenz:** Nutzbarkeit wird ausschließlich live beim Laden der Admin-Seite berechnet — keine periodische Hintergrund-Neuberechnung, keine Persistenz in `PluginSetting`, kein Hintergrund-/Migrations-Service (siehe Designentscheidung „Prüffrequenz / Persistenz" und „Datenbankmigrationen: Keine").
- **Plugin-Abhängigkeiten:** Abhängigkeiten zwischen Plugins werden nicht berücksichtigt (außerhalb des Scopes; siehe Designentscheidung „Plugin-Abhängigkeiten").
