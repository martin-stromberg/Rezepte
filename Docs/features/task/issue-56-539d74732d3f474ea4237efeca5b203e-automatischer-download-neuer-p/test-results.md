# Testergebnisse: Automatischer Download neuer Plugins

Ausgefuehrt am 2026-07-18.

## Relevante Tests

Kommando:

```text
dotnet test Rezepte.Tests\Rezepte.Tests.csproj --no-restore --filter "FullyQualifiedName~Rezepte.Tests.Services.Import" --logger "console;verbosity=minimal"
```

Ergebnis: **Bestanden**

- 37 Tests ausgefuehrt
- 37 bestanden
- 0 fehlgeschlagen
- 0 uebersprungen

Damit sind die Tests fuer die Import-/Plugin-Funktionalitaet der aktuellen Anforderung erfolgreich.

## Vollstaendiger Testlauf

Kommando:

```text
dotnet test --no-restore --logger "console;verbosity=minimal"
```

Ergebnis: **162 von 163 Tests bestanden**

- 162 bestanden
- 1 fehlgeschlagen
- 0 uebersprungen
- Fehlgeschlagener Test: `Rezepte.Tests.Deployment.DeploymentDocumentationTests.FrameworkDependentLinuxPublish_ShouldProduceDocumentedEntrypointAndRuntimeFrameworks`

Der Fehler tritt im bestehenden Deployment-Publish-Test auf. Beim `dotnet publish` fuer `linux-x64` kann die Referenz auf `Rezepte.Web` im Publish-Kontext der externen Projekte `Rezepte.Import.Plugins.AIFoto` und `Rezepte.Import.Plugins.AIUrl` nicht aufgeloest werden. Dadurch entstehen Folgefehler bei den fehlenden Web-Typen. In diesem Schritt wurden keine produktiven Codeaenderungen vorgenommen.

Zusaetzlich wurde die bekannte NuGet-Warnung `NU1903` fuer `SQLitePCLRaw.lib.e_sqlite3` 2.1.11 ausgegeben.
