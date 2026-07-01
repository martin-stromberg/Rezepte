# Projekt- und Frameworkbestand

## Solution

Datei: `Rezepte.sln`

Enthaltene Projekte:

| Projekt | Pfad | Typ | Aktuelles Target Framework |
|---|---|---|---|
| Rezepte.Web | `Rezepte.Web/Rezepte.Web.csproj` | `Microsoft.NET.Sdk.Web` | `net9.0` |
| Rezepte.Tests | `Rezepte.Tests/Rezepte.Tests.csproj` | `Microsoft.NET.Sdk` | `net9.0` |

Die Solution enthaelt Debug- und Release-Konfigurationen fuer beide Projekte.

## Rezepte.Web

Datei: `Rezepte.Web/Rezepte.Web.csproj`

Relevante Eigenschaften:

- SDK: `Microsoft.NET.Sdk.Web`
- `TargetFramework`: `net9.0`
- `Nullable`: `enable`
- `ImplicitUsings`: `enable`

Spezielle Content-Regeln:

- `google.application-credentials.json` und `google.gemini.api-key.json` werden immer ins Build-Output kopiert.
- `test.recipe-import.json` wird nicht ins Publish-Verzeichnis kopiert.
- `test.recipe-import.json` wird in Debug mit `PreserveNewest` ins Build-Output kopiert.

Upgrade-Relevanz:

- `TargetFramework` ist direkt auf `net10.0` umzustellen.
- Content-Regeln sind nicht frameworkgebunden, sollten aber nach Publish/Test verifiziert werden.
- Projekt nutzt EF Core Migrations unter `Rezepte.Web/Migrations`; EF-Core-Upgrade muss Migration-Snapshot und Design-Time-Build beachten.

## Rezepte.Tests

Datei: `Rezepte.Tests/Rezepte.Tests.csproj`

Relevante Eigenschaften:

- SDK: `Microsoft.NET.Sdk`
- `TargetFramework`: `net9.0`
- `IsPackable`: `false`
- `Nullable`: `enable`
- `ImplicitUsings`: `enable`
- `ProjectReference`: `../Rezepte.Web/Rezepte.Web.csproj`

Upgrade-Relevanz:

- `TargetFramework` ist direkt auf `net10.0` umzustellen.
- Testprojekt muss nach Upgrade weiter gegen `Rezepte.Web` referenzieren.
- Testpakete sollten gemeinsam mit Framework-Upgrade aktualisiert werden.

## Lokale .NET-Installation

`dotnet --info` meldet:

- SDK: `10.0.301`
- Host: `10.0.9`
- Installierte Runtimes: `Microsoft.AspNetCore.App` `8.0.28`, `9.0.17`, `10.0.9`; `Microsoft.NETCore.App` `8.0.28`, `9.0.17`, `10.0.9`; `Microsoft.WindowsDesktop.App` `8.0.28`, `9.0.17`, `10.0.9`
- `global.json`: nicht vorhanden

Upgrade-Relevanz:

- Die lokale Umgebung kann `net10.0` grundsaetzlich bauen.
- Ohne `global.json` waehlt `dotnet` das installierte neueste passende SDK. Das ist aktuell `10.0.301`.
- Falls reproduzierbare SDK-Versionen gefordert sind, waere eine neue `global.json` eine bewusste Planungsentscheidung.
