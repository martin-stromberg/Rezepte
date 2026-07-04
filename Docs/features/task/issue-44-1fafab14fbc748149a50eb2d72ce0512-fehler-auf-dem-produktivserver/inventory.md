# Bestandsaufnahme

Anforderung: Profil-Einstellungen brechen auf dem produktiven Server beim serverseitigen Blazor-Rendering mit `System.IO.FileNotFoundException` fuer `System.Runtime.Serialization.Primitives, Version=10.0.0.0` ab.

Hinweis zur Ausfuehrung: Der Lifecycle sieht fuer `/inventory` einen Unteragenten vor. In dieser Umgebung wurde die Bestandsaufnahme direkt ausgefuehrt und hier dokumentiert.

## Ergebnisuebersicht

- Die fachlich betroffene Stelle ist `Rezepte.Web/Components/Settings/UserProfile.razor`. Dort werden mehrere `InputText`-Felder und `ValidationMessage`-Expressions auf `Vm.Profile.*` und `Vm.Password.*` gerendert.
- Der API-Pfad `GET /api/users/me` ist vom Renderfehler getrennt: `UserProfileViewModel.LoadAsync` ruft den Endpunkt ab, aktualisiert die bestehende Profil-Instanz und loest danach ueber `Notify()` ein erneutes Rendern aus.
- Das Web- und Testprojekt zielen beide auf `net10.0`. Die ASP.NET-/EF-Core-Pakete im Webprojekt stehen sichtbar auf `10.0.9`; es gibt keine zentrale `Directory.Build.props`, `Directory.Packages.props` oder `global.json`.
- Die Anwendung nutzt Blazor Interactive Server Components. Damit muss der produktive Host bei framework-abhaengigem Publish ein passendes `Microsoft.NETCore.App`- und `Microsoft.AspNetCore.App`-Shared-Framework bereitstellen.
- Die Deployment-Doku beschreibt ein framework-abhaengiges Publish fuer `linux-x64`. Im lokalen Publish liegt `System.Runtime.Serialization.Primitives.dll` nicht als Datei im Publish-Verzeichnis, sondern wird vom Shared Framework erwartet. Ein Produktionsserver ohne passende .NET-10-Runtime kann deshalb genau an dieser Stelle scheitern.
- Auffaellig: `Docs/install.md` startet im systemd-Beispiel `/var/www/rezepte/Rezepte.dll`, der lokale Publish erzeugt aber `Rezepte.Web.dll` und den nativen Host `Rezepte.Web`.
- Es gibt derzeit keine Komponenten-/Render-Tests fuer `UserProfile`; vorhandene Tests decken Service-Logik ab. Eine Regression sollte daher ueber Publish-/Runtime-Verifikation oder einen neuen Render-/Smoke-Test abgesichert werden.

## Detaildokumente

- [UI- und API-Fluss](inventory/user-profile-rendering.md)
- [Runtime-, Paket- und Publish-Befund](inventory/runtime-publish.md)
- [Test- und Verifikationslage](inventory/tests-verification.md)

## Relevante Dateien

- `Rezepte.Web/Components/Settings/UserProfile.razor`
- `Rezepte.Web/ViewModels/UserProfileViewModel.cs`
- `Rezepte.Web/Controllers/UsersController.cs`
- `Rezepte.Web/Contracts/UserDtos.cs`
- `Rezepte.Web/Rezepte.Web.csproj`
- `Rezepte.Tests/Rezepte.Tests.csproj`
- `Rezepte.Web/Program.cs`
- `Rezepte.Web/Extensions/ServiceCollectionExtensions.cs`
- `README.md`
- `Docs/install.md`
- `Docs/dependencies.md`

## Ausgefuehrte Pruefungen

- `git branch --show-current`
- `rg --files`
- `rg -n` ueber betroffene Dateien und Konfigurationen
- `dotnet --info`
- `dotnet list Rezepte.sln package --include-transitive`
- `dotnet publish Rezepte.Web -c Release -f net10.0 -r linux-x64 --self-contained false -o <temp>`

