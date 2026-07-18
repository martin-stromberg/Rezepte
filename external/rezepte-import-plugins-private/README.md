# Rezepte Import Plugins

Private repository structure for the productive online recipe import plugins.

## Build

```powershell
dotnet test Rezepte.Import.Plugins.sln
```

## Manual Runner

List available options:

```powershell
dotnet run --project Rezepte.Import.PluginRunner -- --help
```

Stable local Chefkoch demo:

```powershell
dotnet run --project Rezepte.Import.PluginRunner -- --plugin chefkoch --file tests/fixtures/chefkoch-recipe.html
```

URL demo:

```powershell
dotnet run --project Rezepte.Import.PluginRunner -- --plugin chefkoch --url https://www.chefkoch.de/rezepte/1234567890/Demo-Rezept.html
```

The runner first calls `CanHandleAsync`. If the selected plugin cannot process the input, it prints a clear message. If processing succeeds, it prints success state, error text, recipe count, title, source, portions, times, ingredients and steps.

## Host Plugin Output

Publish all online plugins into the folder structure expected by the host:

```powershell
./publish-plugins.ps1
```

Output shape:

```text
artifacts/plugins/Rezepte.Import.Plugins.Chefkoch/Rezepte.Import.Plugins.Chefkoch.dll
artifacts/plugins/Rezepte.Import.Plugins.SecondSource/Rezepte.Import.Plugins.SecondSource.dll
```

Copy the generated plugin folders into the host application's `plugins/` directory. The host prefers its own `Rezepte.Import.Abstractions` assembly while loading plugins, so plugin and host contract versions must stay compatible.

When this repository structure is checked out next to the host as `external/rezepte-import-plugins-private`, the host project can also consume the generated `artifacts/plugins` folder during build and publish. Run `publish-plugins.ps1` first, then build or publish `Rezepte.Web`.
