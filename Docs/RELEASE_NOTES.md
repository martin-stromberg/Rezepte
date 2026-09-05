# Release Notes

## Important Notes Before Update

- Install the Git hooks after checkout by running `.githooks/install-hooks.cmd` (Windows) or `.githooks/install-hooks.sh` (Linux/macOS); pre-commit and pre-push checks are enforced locally.
- Direct pushes to the `main` and `staging` branches are now blocked by the pre-push hook; use feature branches and pull requests.

## What's New

- Adopted Git hooks from the Pattern-Collection repository (`pre-commit` and `pre-push` with branch protection for `main`/`staging` and checks for localization, XML documentation, stubs, enum coverage, formatting, and encoding).
- Enforced complete XML documentation in all projects (CS1591 as error).
- Localized all UI strings in 40 Razor files using `IStringLocalizer<UiStrings>` and `UiStrings.resx`.
- Reworked throw-only test stubs to satisfy the no-throw-stub check.
- Added and extended enum test coverage.

## Wichtige Hinweise vor dem Update

- Git-Hooks nach dem Checkout installieren: `.githooks/install-hooks.cmd` (Windows) oder `.githooks/install-hooks.sh` (Linux/macOS); pre-commit- und pre-push-Pruefungen gelten lokal.
- Direkte Pushes auf die `main`- und `staging`-Branches werden vom pre-push-Hook blockiert; Feature-Branches und Pull-Requests verwenden.

## Neuerungen

- Git-Hooks aus dem Pattern-Collection-Repository uebernommen (`pre-commit` und `pre-push` mit Branch-Schutz fuer `main`/`staging` sowie Pruefungen fuer Lokalisierung, XML-Doku, Stubs, Enum-Abdeckung, Formatierung und Encoding).
- Vollstaendige XML-Dokumentation in allen Projekten erzwungen (CS1591 als Fehler).
- Alle UI-Strings in 40 Razor-Dateien ueber `IStringLocalizer<UiStrings>` und `UiStrings.resx` lokalisiert.
- throw-only-Test-Stubs umgebaut, um den No-Throw-Stub-Check zu bestehen.
- Enum-Testabdeckung ergaenzt und erweitert.
