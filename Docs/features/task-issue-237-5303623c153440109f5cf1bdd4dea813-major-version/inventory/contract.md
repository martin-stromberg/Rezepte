# Bestandsaufnahme: Import-Contract, `contract-baselines/` und Plugin-Ladepfad

## `ImportContractVersion` — Rolle und Verwendung

- `Directory.Build.props:6`: `<ImportContractVersion>0.3.0</ImportContractVersion>` — eine einzige Property für die ganze Solution.
- Verwendet als `Version`/`AssemblyVersion`/`FileVersion` der beiden Vertrags-Assemblies:
  - `Rezepte.Import.Abstractions/Rezepte.Import.Abstractions.csproj:7-9`
  - `Rezepte.Import.PluginSdk/Rezepte.Import.PluginSdk.csproj:5-7`
- Gelesen von `scripts/resolve-release-version.mjs:60-69` für den erwarteten Asset-Namen `rezepte-import-contract-<version>.zip` (Zeilen 81).
- `scripts/Export-ImportContract.ps1`: liest die Property aus `Directory.Build.props` (Z. 313-315), setzt `-p:ImportContractVersion=` beim Contract-Build (Z. 235) und bricht ab, wenn `-ContractVersion` abweicht (Z. 536).

## `contract-baselines/import-contract/`

- Enthält je einen Ordner pro Vertragsversion: `0.2.0/` und `0.3.0/` mit jeweils `Rezepte.Import.Abstractions.dll` + `Rezepte.Import.PluginSdk.dll` (eingefrorene Referenz-Builds).
- Wenn der Ordner existiert, installieren `pr-staging-ci.yml` (163-188), `staging-ci.yml` (150-173) und `release.yml` (150-195) `Microsoft.DotNet.ApiCompat.Tool` und lassen den Export **hart** gegen die höchste Baseline ≤ aktueller Contract-Version laufen (`--strict-mode`, auch additive Änderungen `CP0001`/`CP0002` schlagen fehl — `Docs/help/import-plugins.md:155`).
- Bump-Prozedere ist dokumentiert: `Docs/help/import-plugins.md:153-190` (Version anheben → Baseline ohne Vergleich erzeugen → neue Baseline committen → `ContractExportScriptTests.cs` an 4 Stellen anpassen → voller `dotnet test`). `Rezepte.Tests/ContractExport/ContractExportScriptTests.cs` hält `0.3.0` wörtlich (Konstante Z. 62, Baseline-Pfade Z. 147-148, Fehlertext Z. 295).

## Bezug zur Plugin-Lade-Kompatibilität

- Der Host lädt externe Plugins über einen eigenen `PluginLoadContext` (`AssemblyLoadContext`): `Rezepte.Web/Services/Import/Plugins/PluginManager.cs:314-376` (`LoadFromAssemblyPath`, Descriptors, Ladefehler → `LoadFailed`/Placeholder-Status, Z. 390-407). **Keine** Versionsprüfung gegen `ImportContractVersion` zur Laufzeit — Kompatibilität ist rein strukturell (Assembly lädt + Handler-Typ vorhanden/kompatibel).
- `ImportContractVersion` versioniert nur den **Contract-Export** (ZIP-Asset-Name, Assembly-Versionen, ApiCompat-Baseline-Zuordnung) — sie ist laut `Docs/help/import-plugins.md:159` explizit „unabhängig von den Git-Tag-basierten Anwendungs-Releases".
- Die Inkompatibilität, die Issue #237 als Breaking Change behandelt, entsteht also nicht durch den Vertrags-Mechanismus, sondern durch den Framework-Sprung (externe Plugins wurden gegen `net10.0`-Contract-/Runtime-Umgebung gebaut). Ob die Vertragsversion deshalb angehoben werden muss, ist offene Frage 2 — die API-Fläche der Vertrags-Assemblies ändert sich durch den TFM-Wechsel nicht (ApiCompat vergleicht API, nicht TFM).
- Gegenstelle im Plugin-Repo: `contract/api-baseline/*.dll` + `Test-ContractApiCompatibility.ps1` mit `RequireBaseline` prüfen Plugin-Binary-Kompatibilität gegen den importierten Host-Contract (`docs/help/ci-release-package.md`).

## Update-Konsumenten (Seiteneffekt-Fläche)

- App-Updates: `Rezepte.Web/appsettings.json:32-44` `ApplicationUpdates` (`RepositoryOwner/Name: martin-stromberg/Rezepte`, `ManifestAssetName: update.json`) — ein `1.x`-Maintenance-Release erzeugt ebenfalls `update.json`; wie der externe `msTools.Updater` die „neueste" Release bestimmt, ist hier nicht sichtbar (Risiko: Downgrade-Angebot bei `latest`-Markierung).
- Plugin-Updates der App-Seite: `Rezepte.Web/Services/Import/Plugins/GitHubReleaseClient.cs:28` nutzt `releases/latest` — betrifft aber das **Plugin-**Repo, nicht das App-Repo.
