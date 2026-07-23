# Kundenanforderung: Nutzbarkeitsprüfung der Plugins

## Fachliche Zusammenfassung

Das Plugin-Interface muss um eine Prüfmethode erweitert werden, die zur Laufzeit überprüft, ob ein Plugin nutzbar ist. Diese Methode soll nicht nur angeben, ob das Plugin einsatzfähig ist, sondern auch konkrete Fehlerursachen (z. B. fehlende Credentials für KI-Dienste, deaktivierte Einstellungen) zurückliefern. Die Admin-UI der Pluginverwaltung soll diese Nutzbarkeit unterhalb des Status-Badges anzeigen und dem Administrator Lösungsvorschläge präsentieren.

## Betroffene Klassen und Komponenten

**Interfaces und Abstraktion:**
- `Rezepte.Import.Abstractions/IImportPlugin` – muss um eine neue Prüfmethode `GetUsabilityAsync()` oder ähnlich erweitert werden, die eine Struktur mit Nutzungsstatus, Fehlern und Lösungshinweisen zurückgibt
- `Rezepte.Import.Abstractions/IImportHandler` – möglicherweise: Bereitstellung von Zugriff auf die Prüfmethode zur Laufzeit

**Data und Entities:**
- `Rezepte.Web/Entities/PluginSetting` – Erweiterung um Felder für Nutzbarkeits-Status und Fehler (z. B. `UsabilityStatus`, `UsabilityErrors`, `UsabilityHints`)

**Services und Manager:**
- `Rezepte.Web/Services/Import/Plugins/PluginManager` – Integration der Prüflogik; Aufbau einer neuen Methode oder Service zur regelmäßigen/bedarfsgerechten Prüfung
- Neuer Service `IPluginUsabilityService` (optional) – zentrale Verwaltung der Prüflogik für alle aktivierten Plugins

**Data Transfer / API:**
- `Rezepte.Web/Services/Import/Plugins/PluginSettingsItem` – Erweiterung um Felder für Nutzbarkeits-Status und Fehler/Hinweise

**UI/Komponenten:**
- `Rezepte.Web/Components/Settings/PluginSettings.razor` – Anzeige der Nutzbarkeits-Fehlermeldungen und Lösungsvorschläge unterhalb des Status-Badges

**Tests:**
- Unit-Tests für die neue Prüfmethode in Plugins
- Integrationstests für die Nutzbarkeits-Prüfung im `PluginManager`

## Implementierungsansatz

1. **Plugin-Interface-Erweiterung:**
   - Ein neues Datenmodell (z. B. `PluginUsabilityResult`) mit Eigenschaften wie `IsUsable` (bool), `Errors` (List<string>), `HelpText` (string) definieren
   - Die Methode `Task<PluginUsabilityResult> CheckUsabilityAsync(IServiceProvider serviceProvider, CancellationToken ct)` zum Interface `IImportPlugin` hinzufügen
   - Dies erlaubt jedem Plugin, seine Nutzbarkeit eigenverantwortlich zu prüfen (z. B. KI-Plugins prüfen auf verfügbare Credentials, globale Einstellungen, Benutzerberechtigung)

2. **Plugin-Manager Erweiterung:**
   - Neue öffentliche Methode `Task<Dictionary<string, PluginUsabilityResult>> GetPluginsUsabilityAsync(IServiceProvider serviceProvider, CancellationToken ct)` hinzufügen
   - Diese Methode wird beim Laden der Pluginliste aufgerufen oder abrufbar gemacht
   - Fehler bei der Prüfung werden gesammelt und dem Plugin-Status hinzugefügt

3. **Persistierung (optional):**
   - Nutzbarkeits-Status können optional in der Datenbank (`PluginSetting`) gecacht werden, um die Anzahl der Prüfungen zu reduzieren
   - Ein Hintergrund-Service könnte regelmäßig Prüfungen durchführen

4. **UI-Integration:**
   - Die `PluginSettings.razor` zeigt unterhalb des Status-Badges einen Fehlerbereich an, wenn `IsUsable == false`
   - Fehler und Lösungshinweise werden formschön dargestellt (z. B. mit `<small class="text-danger">` und einer Alertbox)

## Konfiguration

Das Feature ist nicht explizit konfigurierbar. Die Nutzbarkeit wird auf Basis der aktuellen Anwendungskonfiguration (globale KI-Einstellungen, Benutzereinstellungen, Credentials) zur Laufzeit geprüft. Ein Admin kann keine Nutzbarkeits-Prüfung deaktivieren; die Prüfungen sind transparent und dienen der Information.

## Offene Fragen

1. **Prüffrequenz:** Soll die Nutzbarkeit beim Laden der Pluginliste aktualisiert werden, oder nur zu Beginn? Gibt es Caching, um zu häufige Prüfungen zu vermeiden (z. B. Prüfung nur alle 5 Minuten)?

2. **Asynchrone Prüfung:** Können Prüfungen (insbesondere für externe Services wie Google Gemini) dazu führen, dass die Admin-UI blockiert wird? Sollten Prüfungen asynchron und gecacht werden?

3. **Bestandteile der Fehlermeldung:** Welche Fehler sollen priorisiert werden? Z. B.
   - Fehlende Credentials (höchste Priorität)
   - Deaktivierte globale KI-Schalter (hohe Priorität)
   - Benutzer-KI-Berechtigung nicht vorhanden (mittlere Priorität)
   - Netzwerkfehler (niedrige Priorität / nur Logs)

4. **Mehrsprachigkeit:** Sollen die Lösungshinweise vom Plugin lokalisierbar sein, oder feste englische Meldungen?

5. **Abhängigkeiten zwischen Plugins:** Können Plugins voneinander abhängen? Müssen Abhängigkeits-Fehler berücksichtigt werden?
