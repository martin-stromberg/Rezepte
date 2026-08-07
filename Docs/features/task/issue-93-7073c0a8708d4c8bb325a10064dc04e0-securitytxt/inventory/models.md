## `AppSetting`
Datei: `src/Rezepte.Web/Entities/AppSetting.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Key` | `string` | Primärschlüssel; identifiziert die Einstellung (z. B. `"AiEnabled"`, `"GlobalGeminiEnabled"`). Für security.txt werden Schlüssel der Form `SecurityTxt.<Direktive>` erwartet. |
| `Value` | `string` | Wert der Einstellung als serialisierter String (bool als `"True"`/`"False"`, Zahlen als Zeichenkette, freier Text direkt). |

Die Klasse hat keinen Fremdschlüssel und keine Navigationseigenschaften — sie ist eine reine Key-Value-Tabelle.

---

## `SettingsViewModel` / `Item`
Datei: `src/Rezepte.Web/ViewModels/SettingsViewModel.cs`

### `SettingsViewModel`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Items` | `IReadOnlyList<Item>` | Alle konfigurierten Einstellungs-Navigationspunkte (Profil, Einstellungen, Benutzer, Plugins, Datenexport, Sicherung, Nutzungsstatistiken). |
| `SelectedItem` | `Item` | Aktuell ausgewählter Menüpunkt; initialisiert auf `Items.First()`. |
| `Visible` | `bool` | (Eigenschaft deklariert, aber im Konstruktor nicht befüllt — immer `false` per Default.) |

### `Item` (nested sealed class)

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Title` | `string` | Anzeigename im Menü. |
| `Icon` | `string` | Emoji-Icon. |
| `ComponentType` | `Type` | Blazor-Komponenten-Typ, der beim Anklicken gerendert wird. |
| `Visible` | `bool` | Steuert, ob das Menüelement angezeigt wird (`isAdmin`-Abhängigkeit für Admin-only-Elemente). |

Bestehende Admin-only Items: `"Benutzer"` (`typeof(UserAdmin)`), `"Plugins"` (`typeof(PluginSettings)`).  
Ein neues `SecurityTxtSettings`-Item muss hier analog eingetragen werden.
