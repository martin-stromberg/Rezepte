# Anforderungsübersetzung: Vermisste Plugins aus der Verwaltung entfernen (Schritt 3, Issue #179)

## Fachliche Zusammenfassung

Die Pluginverwaltung erhält eine Entfernen-Funktion auf Service-Ebene: `PluginSetting`-Datensätze, die von der Discovery-Synchronisation mit `PluginStatus.Missing` markiert wurden, können dauerhaft aus der Verwaltung gelöscht werden. Die Funktion ist Administratoren vorbehalten (`EnsureAdmin`), steht nur für Einträge im Vermisst-Status zur Verfügung und löscht ausschließlich den Verwaltungseintrag — keine Dateisystemoperationen. Wird das Plugin später wieder entdeckt, greift der bestehende Neu-Erfassungspfad in `SynchronizeSettingsAsync`: frischer `DiscoveredAt`-Zeitpunkt und Anhängen ans Ende der `OrderIndex`-Reihenfolge.

## Fachlicher Scope

- **Verarbeitungseinheit:** Ein einzelner `PluginSetting`-Verwaltungseintrag mit `Status == PluginStatus.Missing`.
- **Auslösende Ereignisse:** Manuelle Nutzeraktion „Entfernen" eines Administrators an einem vermissten Plugin. (Der UI-Aufruf selbst ist Gegenstand von Schritt 5; dieser Schritt liefert den Service-Vertrag.)
- **Erwartete Wirkung:** Der Eintrag verschwindet aus `GetPluginsAsync` und damit aus der Pluginverwaltung. Bei einer späteren Wiederentdeckung durch `PluginManager.SynchronizeSettingsAsync` entsteht ein neuer Datensatz mit neuem `DiscoveredAt`, `OrderIndex` am Ende der Reihenfolge und `Enabled` gemäß Neu-Plugin-Logik (`Status == Loaded`).
- **Umgang mit fehlenden Objekten:**
  - `PluginSetting`-Datensatz zur übergebenen `pluginId` nicht vorhanden: Fehlerfall — `InvalidOperationException` „not found" (Muster: `SetEnabledAsync`, `MoveAsync`).
  - `PluginSetting` vorhanden, aber `Status != PluginStatus.Missing`: verwerfen — Entfernen wird abgelehnt (`InvalidOperationException`), da die Funktion nur für Vermisst-Einträge zulässig ist.
  - Plugindateien im Dateisystem: per Definition nicht mehr im Discovery-Pfad vorhanden; es gibt nichts zu bereinigen.
- **Scope-Ausschlüsse:**
  - Keine UI-Anbindung (Button, Bestätigungsdialog, Anzeige) — folgt in Schritt 5.
  - Keine Dateisystem-Löschung — Pluginverzeichnisse/Löschmarkierungen sind Schritt-1-Thema und für `Missing`-Plugins ohnehin nicht mehr auffindbar.
  - Keine Entfernen-Funktion für andere Status (`Loaded`, `Incompatible`, `LoadFailed`, `RuntimeFailed`).
  - Kein Entfernen von `PluginSource`-Einträgen — dafür existiert `DeleteSourceAsync`.
  - Kein Audit-Log/Protokolleintrag für die Entfernung — nicht angefordert.
  - Keine Änderung am Systemexport: `PluginSettings` bleiben Teil des Exports (`ExportService`); ein wiederhergestelltes Backup kann entfernte Einträge zurückbringen — dieses Verhalten bleibt bewusst unverändert.

## Betroffene Klassen und Komponenten

- **Interfaces:** `IPluginSettingsService` (`Rezepte.Web/Services/Import/Plugins/IPluginSettingsService.cs`) — neue Methode, Vorschlag `Task RemovePluginAsync(string pluginId, CancellationToken ct = default)` (Name als Umsetzungsentscheidung).
- **Logikklassen / Services:** `PluginSettingsService` (`Rezepte.Web/Services/Import/Plugins/PluginSettingsService.cs`) — Implementierung mit `EnsureAdmin()`, Datensatz laden, Status-Prüfung auf `PluginStatus.Missing`, `db.PluginSettings.Remove(...)`, `SaveChangesAsync`. Orientierung an `DeleteSourceAsync` (Zeilen 172–184).
- **Datenmodell:** `PluginSetting` (`Rezepte.Web/Entities/PluginSetting.cs`) — unverändert, keine Schemaänderung.
- **Enums / Status:** `PluginStatus.Missing` (`Rezepte.Web/Services/Import/Plugins/PluginStatus.cs`, Zeile 15) — vorhanden, kein neuer Status.
- **Bereits abgedeckt (keine Änderung):** `PluginManager.SynchronizeSettingsAsync` (`Rezepte.Web/Services/Import/Plugins/PluginManager.cs`, Zeilen 422–476) — Neu-Erfassung mit `DiscoveredAt = now` und `OrderIndex = nextOrder++` stellt das Wiederentdeckungs-Verhalten bereits sicher.
- **UI-Komponenten:** Keine in diesem Schritt.
- **Tests:** `Rezepte.Tests/Services/Import/PluginSettingsServiceTests.cs` — Entfernen eines `Missing`-Eintrags; Ablehnung bei Nicht-`Missing`-Status und bei unbekannter `pluginId`; Admin-Gate (nicht authentifizierter/nicht-administrativer Aufruf → `UnauthorizedAccessException`).

## Implementierungsansatz

- Erweiterung der bestehenden Klasse `PluginSettingsService` und des Interfaces `IPluginSettingsService` — kein neuer Service erforderlich.
- Ablauf identisch zum `DeleteSourceAsync`-Muster: `EnsureAdmin()` → `db.PluginSettings.FindAsync([pluginId], ct)` → `null` ⇒ `InvalidOperationException` → `Status != PluginStatus.Missing` ⇒ `InvalidOperationException` → `Remove` + `SaveChangesAsync`.
- Wiederentdeckung benötigt keine Sonderbehandlung: Nach dem Löschen ist die `pluginId` in `SynchronizeSettingsAsync` unbekannt, sodass der `else`-Zweig (Zeilen 450–465) einen frischen Datensatz mit `DiscoveredAt = now` und `OrderIndex` hinter dem aktuellen Maximum anlegt.
- Zu beachten: `SetEnabledAsync` und `MoveAsync` prüfen aktuell **kein** `EnsureAdmin` — die Entfernen-Funktion soll jedoch laut Anforderung administratorenvorbehalten sein, also `EnsureAdmin` aufrufen (Konsistenz mit den Quellen-Methoden).

## Konfiguration

Keine — das Verhalten ist nicht konfigurierbar.

## Offene Fragen

1. Soll die Entfernen-Funktion bei einem `PluginSetting`, das zwar `Missing` ist, aber `Enabled == true` hat, trotzdem ohne Rückfrage löschen? (Annahme: ja — `Missing` bedeutet, das Plugin ist nicht mehr auffindbar und damit faktisch inaktiv; `Enabled` hat dann keine Wirkung.)
2. Soll `EnsureAdmin` künftig auch auf `SetEnabledAsync`/`MoveAsync` ausgeweitet werden? (Außerhalb dieses Schritts — hier nur für die neue Methode gefordert; als Folge-Beobachtung notiert.)
