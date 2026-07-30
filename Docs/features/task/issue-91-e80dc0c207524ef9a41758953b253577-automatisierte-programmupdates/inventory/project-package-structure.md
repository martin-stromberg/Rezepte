# Projekt- und Paketstruktur fuer msTools.Updater

## Solution-Struktur

Die Solution enthaelt:

- `Rezepte.Web` als ASP.NET Core/Blazor-Webanwendung
- `Rezepte.Import.Abstractions`
- `Rezepte.Import.PluginSdk`
- mehrere eingebaute Import-Plugin-Projekte
- `Rezepte.Tests`
- `Rezepte.Tests.Browser`
- `Rezepte.Tests.PluginFixture`

## Rezepte.Web.csproj

`Rezepte.Web/Rezepte.Web.csproj` verwendet:

- `Microsoft.NET.Sdk.Web`
- `TargetFramework` `net10.0`
- `Nullable` und `ImplicitUsings`
- direkte `PackageReference`-Eintraege
- `ProjectReference` auf `Rezepte.Import.Abstractions`
- eigene MSBuild-Items/Targets fuer Import-Plugin-Projekte

Aktuelle Paketreferenzen:

- `Google.Cloud.Vision.V1`
- `Microsoft.AspNetCore.Authentication.JwtBearer`
- `Microsoft.EntityFrameworkCore.Design`
- `Microsoft.EntityFrameworkCore.Sqlite`
- `QuestPDF`
- `Serilog.AspNetCore`
- `Serilog.Sinks.Console`
- `Serilog.Sinks.File`

Es gibt keine zentrale Paketversionsverwaltung (`Directory.Packages.props` wurde nicht gefunden). `Directory.Build.props` existiert, enthaelt aber keine beobachtete zentrale Paketversionierung fuer `Rezepte.Web`.

## Moegliche Einbindung

Abhaengig davon, was `msTools.Updater` bereitstellt, kommen mehrere Wege in Frage:

- NuGet-Paket: `PackageReference` in `Rezepte.Web.csproj`.
- Projekt-Referenz: zusaetzliches Projekt im Repository oder Submodule-/externes Checkout plus `ProjectReference`.
- Binary-/Tool-Integration: separater Build-/Publish-Schritt, falls der Updater nicht als Library gedacht ist.
- Source-Einbindung: nur als letzte Option, wenn kein Paket/Projekt nutzbar ist und Lizenz/Updatepfad geklaert sind.

## Ergebnis externer Kurzpruefung

Die Anforderung nennt `https://github.com/martin-stromberg/msTools.Updater.git`. In der Websuche war dieser konkrete Repository-Inhalt nicht belastbar auffindbar. Eine NuGet-Suche nach `msTools.Updater` ergab keinen eindeutigen Treffer fuer ein gleichnamiges Paket; sichtbare Treffer waren andere Pakete wie `MSToolKit`, `MST.Tools`, `MSTest` und `dotnet-updatr`.

Das bedeutet fuer die Planung: Paketname, Version, Ziel-Frameworks, Extension-Methoden und Pre-Install-Event muessen vor Implementierung direkt gegen das Repository oder einen privaten Feed verifiziert werden.

## Kompatibilitaetsfragen

- Unterstuetzt `msTools.Updater` `net10.0` oder mindestens kompatible `netstandard`/`net8+`-Targets?
- Ist die Komponente fuer ASP.NET Core Web Apps vorgesehen oder fuer Desktop/Console?
- Arbeitet die Komponente innerhalb desselben Prozesses oder startet sie einen externen Updater-Prozess?
- Wie signalisiert ein Pre-Install-Callback Fehler: Exception, bool, Result-Objekt, CancellationToken?
- Kann der Callback async sein?
- Muss der Prozess fuer Installation beendet werden, und falls ja, wann wird der Backup-Hook ausgefuehrt?
- Welche Release-Quelle erwartet der Updater?

## Build-/Publish-Auswirkungen

`Rezepte.Web.csproj` hat bereits Publish-Targets fuer Plugins. Eine Updater-Integration darf diese Targets nicht mit Web-App-Update-Artefakten vermischen. Falls der Updater Publish-Dateien benoetigt, sollte eine getrennte MSBuild-Property/Target-Struktur verwendet werden.
