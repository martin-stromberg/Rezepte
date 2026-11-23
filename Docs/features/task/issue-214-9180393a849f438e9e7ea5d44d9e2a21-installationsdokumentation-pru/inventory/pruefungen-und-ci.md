# Prüfungen: Dokumentations-Tests und CI-Gates

## `Rezepte.Tests/Deployment/DeploymentDocumentationTests.cs`

Drei Tests koppeln CI direkt an den Inhalt der Installationsdoku:

### 1. `InstallDocumentation_ShouldDescribeRuntimeRequirementsAndCorrectEntrypoint` (Z. 20–35)

Deterministisch (normale Test-Lane). Assertions auf `Docs/install.md`:

| Assertion | Literal |
|---|---|
| `NotContain` | `Rezepte.dll` (Vorsicht: jede Nennung dieses Dateinamens bricht den Test) |
| `Contain` | `Rezepte.Web.dll` |
| `Contain` | `Rezepte.Web` |
| `Contain` | `dotnet --info` |
| `Contain` | `Microsoft.NETCore.App` |
| `Contain` | `Microsoft.AspNetCore.App` |
| `Contain` | `System.Runtime.Serialization.Primitives.dll` |
| `Contain` | `--self-contained false` |
| `Contain` | `--self-contained true` |
| `Contain` | `chmod +x /var/www/rezepte/Rezepte.Web` |

→ Bei der Zwei-Pfad-Umstrukturierung müssen alle `Contain`-Literale
weiterhin wörtlich vorkommen (inkl. `/var/www/rezepte` als Beispielpfad);
die Begründung zu `System.Runtime.Serialization.Primitives.dll` sollte
inhaltlich erhalten bleiben.

### 2. `ReadmeDeploymentSection_ShouldPointToRuntimeCheckedDeploymentOptions` (Z. 41–50)

Assertions auf `README.md`: `Docs/install.md`, `.NET-10-Shared-Frameworks`,
`Microsoft.NETCore.App`, `Microsoft.AspNetCore.App`, `self-contained` —
bei README-Anpassungen erhalten bleiben.

### 3. `FrameworkDependentLinuxPublish_ShouldProduceDocumentedEntrypointAndRuntimeFrameworks` (Z. 56–105)

`[Trait(Category, OsInterface)]` — führt den **dokumentierten**
Publish-Befehl real aus (`dotnet publish Rezepte.Web -c Release -f net10.0
-r linux-x64 --self-contained false`) und prüft `Rezepte.Web.dll` +
runtimeconfig-Frameworks auf 10.x. Ändert die Doku den Publish-Aufruf,
muss geprüft werden, ob der Test-Schritt synchron bleibt (Test ist im
best-effort Browser-Job, nicht blockierend — trotzdem Konsistenz halten).

## CI-Workflows (`.github/workflows/`)

- `pr-staging-ci.yml` (PR → staging): Jobs `static-checks`
  (format/security/TreatWarningsAsErrors-Build), `build-and-test`
  (deterministische Lane + 70-%-Coverage-Gate), `browser-tests`
  (continue-on-error), `no-pending-backmerge`, `detect-backmerge`.
  **Keine Pfadfilter** — eine reine Doku-Änderung durchläuft die volle
  Pipeline.
- `staging-ci.yml`, `release.yml`, `security-scan.yml`, `pages.yml`,
  `staging-to-main-promotion.yml`, `sync-staging-with-main.yml`,
  `verify-pr-source.yml` — **kein** Markdown-Lint, kein Link-Check,
  keine Doku-spezifischen Gates.
- Für diese Aufgabe relevant sind nur die `DeploymentDocumentationTests`
  (deterministischer Teil läuft in `build-and-test`).

## Sinnvolle lokale Checks für die Doku-Änderung

```powershell
# Nur die docu-gekoppelten Tests (schnell, kein OsInterface-Filter nötig):
dotnet test Rezepte.Tests --filter "FullyQualifiedName~DeploymentDocumentationTests"
```

- Optional Gesamtlauf `dotnet test Rezepte.Tests` (deterministische Lane
  spiegelt `build-and-test`).
- Kein Build nötig für die Doku selbst; die Tests lesen die Dateien über
  `RepositoryPaths` relativ zum Repo-Root.
- Manuelle Link-Kontrolle: relative Links `install.md` ↔ `index.md` ↔
  `deployment-guide.md` ↔ `README.md` (kein automatisierter Link-Checker
  vorhanden).
