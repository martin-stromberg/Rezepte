# Konfiguration, CI, Deployment und Dokumentation

## Globale Build-/NuGet-Konfiguration

Nicht gefunden:

- `global.json`
- `Directory.Build.props`
- `Directory.Build.targets`
- `Directory.Packages.props`
- `NuGet.config`
- `packages.lock.json`

Gefundene `.props`/`.targets`/EditorConfig-Dateien lagen nur unter `obj/` und sind generierte Build-Artefakte.

Upgrade-Relevanz:

- Paketversionen werden direkt in den `.csproj`-Dateien gepflegt.
- Es gibt keine zentrale Stelle fuer Target Framework oder Paketversionen.
- Keine SDK-Pinning-Datei verhindert aktuell die Nutzung des lokalen .NET-10-SDKs.

## CI und GitHub

Gefunden:

- `.github/copilot-instructions.md`

Nicht gefunden:

- `.github/workflows/*.yml`
- `.github/workflows/*.yaml`
- `.gitlab-ci.yml`
- `azure-pipelines.yml`

In `.github/copilot-instructions.md` stehen Qualitaetserwartungen wie:

- `dotnet build`
- `dotnet test`
- `dotnet format --verify-no-changes`
- regelmaessige Abhaengigkeitspruefung per Dependabot/Renovate als Empfehlung

Upgrade-Relevanz:

- Es gibt keinen ausfuehrbaren CI-Workflow, der auf .NET 10 angepasst werden muss.
- Die Copilot-Anweisungen enthalten Prozessvorgaben, aber keine konkrete SDK-Version.

## Docker und Deployment-Artefakte

Nicht gefunden:

- `Dockerfile`
- `docker-compose*`
- Container-spezifische YAML-Dateien

Upgrade-Relevanz:

- Kein Container-Basisimage zu aktualisieren.

## Dokumentationsstellen mit .NET-9-Bezug

| Datei | Stelle | Inhalt |
|---|---:|---|
| `README.md` | Zeile 24 | Voraussetzung `.NET 9` |
| `README.md` | Zeile 112 | Publish-Beispiel mit `-f net9.0` |
| `Docs/install.md` | Zeile 5 | `Framework net9.0` |
| `Docs/Anforderungskatalog.md` | Zeile 1 | Titel nennt `Blazor Server, .NET 9, SQLite` |

Upgrade-Relevanz:

- Diese Verweise sollten nach erfolgreicher technischen Umstellung auf .NET 10 synchronisiert werden.
- `Docs/install.md` beschreibt systemd-Betrieb und frameworkabhaengiges Publish; nach Upgrade sollte ein Publish-Test fuer `net10.0` eingeplant werden.

## App-Konfiguration

Gefundene relevante Runtime-Konfiguration:

- `Rezepte.Web/appsettings.json`
- `Rezepte.Web/appsettings.Development.json`
- `Rezepte.Web/Properties/launchSettings.json`

Kein direkter Framework-Bezug in den gefundenen App-Konfigurationsdateien ausser `dotnetRunMessages` in `launchSettings.json`.

## Bestehender Git-Zustand

`git status --short` zeigte vor dem Erstellen dieser Inventory-Artefakte:

- geaendert: `.gitignore`
- untracked: `Docs/features/`

Diese Bestandsaufnahme hat keine vorhandenen Aenderungen zurueckgesetzt und keine Produktionsdateien geaendert.
