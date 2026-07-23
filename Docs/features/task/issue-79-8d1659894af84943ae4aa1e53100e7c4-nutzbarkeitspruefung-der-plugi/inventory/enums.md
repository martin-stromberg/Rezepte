# Enums

## `PluginStatus`
Datei: `Rezepte.Web/Services/Import/Plugins/PluginStatus.cs`

Statische Klasse mit Konstanten für verschiedene Plugin-Status.

| Wert | Konstanten-Name | Bedeutung |
|---|---|---|
| `"Loaded"` | `PluginStatus.Loaded` | Plugin wurde erfolgreich geladen und ist einsatzbereit |
| `"Missing"` | `PluginStatus.Missing` | Plugin wurde vorher entdeckt, ist aber nicht mehr vorhanden |
| `"Incompatible"` | `PluginStatus.Incompatible` | Plugin implementiert nicht das erforderliche Interface oder Handler ist nicht kompatibel |
| `"LoadFailed"` | `PluginStatus.LoadFailed` | Plugin konnte nicht geladen werden (z. B. Assembly-Fehler, Instanziierungsfehler) |
| `"RuntimeFailed"` | `PluginStatus.RuntimeFailed` | Plugin wurde zwar geladen, schlägt aber zur Laufzeit fehl (z. B. bei Handler-Instanziierung) |

**Verwendung:** Diese Status werden in `PluginSetting.Status` und `ImportPluginDescriptor.Status` verwendet.

**Status:** Dieser Enum deckt Lade- und Kompatibilitätsfehler ab, enthält aber keine dedizierte Kategorien für **Nutzbarkeitsfehler** (z. B. fehlende Credentials, deaktivierte Einstellungen).

---

## `PluginSourceReleaseStatus`
Datei: `Rezepte.Web/Services/Import/Plugins/PluginSourceReleaseStatus.cs`

Statische Klasse mit Konstanten für den Status von GitHub-Release-Aktualisierungen.

| Wert | Bedeutung |
|---|---|
| `"Pending"` | Update steht noch an |
| `"InProgress"` | Update läuft gerade |
| `"Success"` | Update war erfolgreich |
| `"Failed"` | Update ist fehlgeschlagen |

**Verwendung:** Wird für Plugin-Source-Updates verwendet, nicht direkt für Plugin-Nutzbarkeitsprüfung.
