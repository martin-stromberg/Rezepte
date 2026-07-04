# Detail: Runtime-, Paket- und Publish-Befund

## Projekt- und Paketstand

`Rezepte.Web/Rezepte.Web.csproj`:

- `TargetFramework`: `net10.0`
- ASP.NET/JWT-Paket: `Microsoft.AspNetCore.Authentication.JwtBearer` `10.0.9`
- EF-Core-Pakete: `Microsoft.EntityFrameworkCore.Design` und `Microsoft.EntityFrameworkCore.Sqlite` `10.0.9`
- Weitere direkte Pakete: `Google.Cloud.Vision.V1`, `QuestPDF`, `Serilog.*`

`Rezepte.Tests/Rezepte.Tests.csproj`:

- `TargetFramework`: `net10.0`
- Teststack: `Microsoft.NET.Test.Sdk`, xUnit, FluentAssertions, Moq, EF-Core InMemory

Es wurden keine `Directory.Build.props`, `Directory.Packages.props` oder `global.json` gefunden. Die sichtbare Versionierung liegt damit direkt in den Projektdateien und in der installierten lokalen SDK-/Runtime-Umgebung.

## Lokale .NET-Umgebung

`dotnet --info` meldet lokal:

- SDK: `10.0.301`
- Host: `10.0.9`
- installierte Shared Frameworks: `Microsoft.NETCore.App 10.0.9` und `Microsoft.AspNetCore.App 10.0.9`
- `global.json`: nicht vorhanden

Die lokale Entwicklungsumgebung ist damit fuer `net10.0` und die verwendeten `10.0.9`-Pakete passend ausgestattet.

## Publish-Befund

Ausgefuehrter lokaler Publish:

```powershell
dotnet publish Rezepte.Web -c Release -f net10.0 -r linux-x64 --self-contained false -o <temp>
```

Ergebnis:

- Publish erfolgreich, aber mit bestehenden Warnungen.
- Output enthaelt `Rezepte.Web.dll`, `Rezepte.Web.deps.json`, `Rezepte.Web.runtimeconfig.json` und den nativen Host `Rezepte.Web`.
- `System.Runtime.Serialization.Primitives.dll` liegt nicht im Publish-Verzeichnis.
- `Rezepte.Web.runtimeconfig.json` referenziert `Microsoft.NETCore.App` und `Microsoft.AspNetCore.App` jeweils mit Mindestversion `10.0.0`.

Bewertung: Bei `--self-contained false` werden Framework-Assemblies wie `System.Runtime.Serialization.Primitives` aus dem installierten Shared Framework der Zielmaschine geladen. Das Publish-Ergebnis allein enthaelt diese Assembly nicht. Wenn der Produktionsserver kein passendes .NET-10-Shared-Framework bereitstellt oder ein unvollstaendiger Runtime-Ordner verwendet wird, ist die gemeldete `FileNotFoundException` plausibel.

## Deployment-Dokumentation

`README.md` beschreibt den typischen Publish-Befehl:

```powershell
dotnet publish Rezepte.Web -c Release -f net10.0 -r linux-x64 --self-contained false
```

`Docs/install.md` beschreibt ebenfalls:

- Release
- Framework `net10.0`
- framework-abhaengige Bereitstellung
- Zielruntime `linux-x64`

Auffaelligkeit: Das systemd-Beispiel in `Docs/install.md` nutzt:

```text
ExecStart=/usr/bin/dotnet /var/www/rezepte/Rezepte.dll
```

Der aktuelle Publish erzeugt jedoch `Rezepte.Web.dll`. Diese Abweichung ist nicht dieselbe Ursache wie die gemeldete Assembly-Exception, sollte aber im Deployment-Fix korrigiert werden, weil sie die Betriebsdokumentation unzuverlaessig macht.

## Paketlistenbefund

`dotnet list Rezepte.sln package --include-transitive` loest fuer `Rezepte.Web` unter anderem automatisch `Microsoft.AspNetCore.App.Internal.Assets 10.0.9` auf. Die Runtime-Assembly `System.Runtime.Serialization.Primitives` erscheint nicht als NuGet-Paket im Projekt, weil sie Bestandteil des .NET-Shared-Frameworks ist.

Die bekannte Warnung `NU1903` zu `SQLitePCLRaw.lib.e_sqlite3 2.1.11` ist bereits in `Docs/dependencies.md` dokumentiert und steht nicht erkennbar im Zusammenhang mit dem Profil-Renderfehler.

## Primaere Verdachtsrichtung

Der naheliegende Fixpfad liegt nicht in der fachlichen Profil-API, sondern in Runtime-/Deployment-Konsistenz:

- Produktionsserver auf installierte `Microsoft.NETCore.App`/`Microsoft.AspNetCore.App` Versionen pruefen.
- Sicherstellen, dass mindestens passende .NET-10-Shared-Frameworks vorhanden sind.
- Bei unsicherer Server-Runtime self-contained Publish fuer `linux-x64` erwaegen oder Deployment explizit an die Runtime-Version binden.
- Publish- und systemd-Dokumentation auf `Rezepte.Web.dll` bzw. den nativen Host `Rezepte.Web` korrigieren.

