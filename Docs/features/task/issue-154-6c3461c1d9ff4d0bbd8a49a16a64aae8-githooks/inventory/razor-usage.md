# Detail: razor-usage-check --all --strict (Exit 1)

18 `.razor`-Dateien als „possibly orphaned" gemeldet. **Alle 18 sind falsch-positive Befunde** — verifiziert durch Byte-Checks, Grep und Lektüre des Check-Skripts.

## Ursachenanalyse des Checks (`.githooks/razor-usage-check.py`)

- `all_razor_files` (Z. 55–61) sammelt nur `*.razor`; Referenzsuche erfolgt ausschließlich innerhalb dieser Dateiinhalte (Z. 91–102). `.cs`-Dateien werden **nicht** durchsucht.
- `is_entry_point` (Z. 79–88): `re.search(r"^\s*@page\s+", content, re.MULTILINE)`. Dateien werden mit `encoding='utf-8'` gelesen (Z. 111) — die UTF-8-BOM bleibt als `\ufeff` am Dateianfang stehen. `\ufeff` ist **kein** Whitespace in Python-Regex, daher schlägt `^\s*@page` bei BOM-Dateien fehl.
- `TYPEOF_TEMPLATE` (Z. 33): `typeof\s*\(\s*{Name}\s*\)` — matcht keine qualifizierten Namen wie `typeof(Layout.MainLayout)`.

## Befundgruppen

### Gruppe A: `@page`-Seiten mit UTF-8-BOM (8 Dateien)

BOM verifiziert per `File.ReadAllBytes` (`EF BB BF`). Trotz `@page`-Direktive als verwaist gemeldet:

| Datei | `@page`-Route |
|-------|---------------|
| `Rezepte.Web/Components/Pages/Calendar.razor` | `/calendar` |
| `Rezepte.Web/Components/Pages/CookbookDetails.razor` | `/cookbooks/{Id}/edit` |
| `Rezepte.Web/Components/Pages/CookbookPage.razor` | `/cookbooks/{Id}` |
| `Rezepte.Web/Components/Pages/Cookbooks.razor` | `/cookbooks` |
| `Rezepte.Web/Components/Pages/Error.razor` | `/Error` |
| `Rezepte.Web/Components/Pages/Home.razor` | `/` |
| `Rezepte.Web/Components/Pages/RecipePage.razor` | `/recipes/{Id}` |
| `Rezepte.Web/Components/Pages/Settings.razor` | `/settings` |

### Gruppe B: Layout mit qualifizierter `typeof`-Referenz (1 Datei)

- `Rezepte.Web/Components/Layout/MainLayout.razor` — hat BOM, kein `@page` (ist `LayoutComponentBase`). Referenziert in `Rezepte.Web/Components/Routes.razor` Zeile 5: `DefaultLayout="typeof(Layout.MainLayout)"` — das `typeof(Layout.MainLayout)`-Muster matcht nicht `typeof(\s*MainLayout\s*)`.

### Gruppe C: Settings-Komponenten, nur in `.cs` referenziert (9 Dateien)

Referenzen in `Rezepte.Web/ViewModels/SettingsViewModel.cs` (Zeilen 33–41), jeweils `typeof(...)` in der Navigationsliste:

| Zeile | Komponente |
|-------|-----------|
| 33 | `typeof(UserProfile)` → `Components/Settings/UserProfile.razor` (kein BOM) |
| 34 | `typeof(AiSettings)` → `Components/Settings/AiSettings.razor` (BOM) |
| 35 | `typeof(UserAdmin)` → `Components/Settings/UserAdmin.razor` (BOM) |
| 36 | `typeof(PluginSettings)` → `Components/Settings/PluginSettings.razor` (kein BOM) |
| 37 | `typeof(ApplicationUpdates)` → `Components/Settings/ApplicationUpdates.razor` (kein BOM) |
| 38 | `typeof(SecurityTxtSettings)` → `Components/Settings/SecurityTxtSettings.razor` (kein BOM) |
| 39 | `typeof(Rezepte.Web.Components.Settings.ExportData)` → `Components/Settings/ExportData.razor` (kein BOM) |
| 40 | `typeof(Rezepte.Web.Components.Settings.BackupRestore)` → `Components/Settings/BackupRestore.razor` (kein BOM) |
| 41 | `typeof(Rezepte.Web.Components.Settings.UsageStats)` → `Components/Settings/UsageStats.razor` (BOM) |

Da der Check nur `.razor`-Dateien durchsucht, zählen diese Referenzen nicht.

### Zusätzlich mit BOM (aus den 18, bereits in Gruppen enthalten)

BOM-Dateien gesamt: `MainLayout`, `Calendar`, `CookbookDetails`, `CookbookPage`, `Cookbooks`, `Error`, `Home`, `RecipePage`, `Settings`, `AiSettings`, `UsageStats`, `UserAdmin` (12 Dateien). Bei `AiSettings`, `UsageStats`, `UserAdmin` ist die BOM für den Befund irrelevant (kein `@page`, Gruppe C). Bei `MainLayout` verhindert die BOM zusätzlich nichts — der Befund entsteht durch den qualifizierten `typeof`-Namen.

## Konsequenz (Feststellung, keine Planung)

Die Check-Logik darf laut Anforderung nicht geändert werden. Auf Anwendungsseite auflösbar durch: BOM-Entfernung (stellt `@page`-Erkennung wieder her), unqualifizierte `typeof(MainLayout)`-Referenz, bzw. Tag-/Referenznutzung der Settings-Komponenten innerhalb einer `.razor`-Datei (z. B. `<DynamicComponent>`/`@` im `Settings.razor`).
