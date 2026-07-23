# Datenmodelle und Entities

## `PluginSetting`
Datei: `Rezepte.Web/Entities/PluginSetting.cs`

Entity für die persistierte Plugin-Konfiguration in der Datenbank.

| Eigenschaft | Typ | Beschreibung |
|---|---|---|
| `PluginId` | `string` | Eindeutige Kennung des Plugins (Primary Key) |
| `DisplayName` | `string` | Anzeigebezeichnung des Plugins |
| `Description` | `string?` | Optionale Beschreibung des Plugins |
| `AssemblyName` | `string` | Name der Assembly, in der das Plugin implementiert ist |
| `TypeName` | `string` | Vollständiger Typname der `IImportPlugin`-Implementierung |
| `Enabled` | `bool` | Gibt an, ob das Plugin aktiviert ist (Default: `true`) |
| `OrderIndex` | `int` | Sortierungsreihenfolge der Plugins |
| `Status` | `string` | Aktueller Status des Plugins (z. B. `PluginStatus.Loaded`, siehe Enum `PluginStatus`) |
| `Error` | `string?` | Fehlermeldung, falls vorhanden (z. B. bei LoadFailed, RuntimeFailed) |
| `DiscoveredAt` | `DateTime` | Zeitstempel, wann das Plugin zum ersten Mal entdeckt wurde |
| `LastSeenAt` | `DateTime` | Zeitstempel der letzten Discovery-Aktualisierung |

**Status:** Felder für Nutzbarkeitsinformationen wie `UsabilityStatus`, `UsabilityErrors`, `UsabilityHints` sind **nicht vorhanden**.

---

## `PluginSettingsItem`
Datei: `Rezepte.Web/Services/Import/Plugins/PluginSettingsItem.cs`

DTO (Data Transfer Object) für die Übertragung von Plugin-Einstellungen zwischen Service und UI.

```csharp
public sealed record PluginSettingsItem(
    string PluginId,
    string DisplayName,
    string? Description,
    string AssemblyName,
    string TypeName,
    bool Enabled,
    int OrderIndex,
    string Status,
    string? Error,
    DateTime DiscoveredAt,
    DateTime LastSeenAt);
```

Entspricht den Feldern aus `PluginSetting`. 

**Status:** Nutzbarkeitsinformationen sind **nicht vorhanden**.

---

## `ImportPluginDescriptor`
Datei: `Rezepte.Web/Services/Import/Plugins/ImportPluginDescriptor.cs`

Record für die interne Representation von entdeckten Plugins während des Discovery-Prozesses.

```csharp
public sealed record ImportPluginDescriptor(
    string Id,
    string DisplayName,
    string? Description,
    string Version,
    string AssemblyName,
    string TypeName,
    Type? HandlerType,
    int DefaultPriority,
    string Status,
    string? Error);
```

Wird von `PluginManager` verwendet, um Plugins zu repräsentieren, bevor sie in `PluginSetting` Entities umgewandelt werden.

**Status:** Keine Nutzbarkeitsinformationen.

---

## `PluginImportHandler`
Datei: `Rezepte.Web/Services/Import/Plugins/PluginImportHandler.cs`

Vereinigte Representation eines Plugins mit seinem Handler.

Wird vom `PluginManager` verwendet, um aktive Plugin-Handler bereitzustellen.

**Status:** Keine Nutzbarkeitsinformationen.
