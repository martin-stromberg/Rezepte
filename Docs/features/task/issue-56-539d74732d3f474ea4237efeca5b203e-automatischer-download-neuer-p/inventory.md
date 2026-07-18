# Bestandsaufnahme: Automatischer Download neuer Plugins

## Ergebnis

Die Anwendung besitzt bereits eine lokale Plugin-Discovery, Persistenz der Plugin-Einstellungen, Startup-Initialisierung und eine administrative Einstellungsseite. Die bestehende Architektur ist damit ein sinnvoller Integrationspunkt für einen Updateprozess, enthält aber noch keine GitHub-Quellen, Release-Abfragen, Paketvalidierung, temporäre Laufzeitprüfung, Installation/Rollback oder periodische Updateausführung.

Die fachliche Umsetzung muss insbesondere die dauerhafte Plugin-Identität von der Release-Version trennen. Heute wird ein Plugin über `PluginSetting.PluginId` verwaltet; Version und Ladefehler stammen ausschließlich aus der Discovery und werden nicht als Releasehistorie gespeichert.

## Detaildokumente

- [Laufzeit und Plugin-Discovery](inventory/runtime-and-discovery.md)
- [Persistenz und Konfiguration](inventory/persistence-and-configuration.md)
- [UI und Autorisierung](inventory/ui-and-authorization.md)
- [Tests und offene technische Lücken](inventory/tests-and-gaps.md)

## Relevante Erweiterungspunkte

| Bereich | Bestehender Einstiegspunkt | Auswirkung der Anforderung |
|---|---|---|
| Discovery und Reload | `Rezepte.Web/Services/Import/Plugins/PluginManager.cs` | Validierte temporäre Pakete prüfen und danach kontrolliert neu laden |
| Plugin-Lebenszyklus | `PluginStartupService`, `IPluginManager` | Synchronisierung mit laufenden Handlern und AssemblyLoadContexts |
| Persistenz | `RezepteDbContext`, `PluginSetting`, EF-Migrationen | Quellen, Versionen, Status und Fehler dauerhaft speichern |
| Einstellungen | `PluginSettingsService`, `PluginSettings.razor` | Quellenverwaltung, Vertrauensbestätigung, Status und manueller Trigger |
| Hintergrundverarbeitung | `BackgroundJobHostedService` als lokales Muster, aktuell `PluginStartupService` als einziger Plugin-Hosted-Service | Neuer dedizierter, über DI registrierter Update-Hosted-Service |
| Berechtigungen | `SettingsViewModel` blendet Plugin-Einstellungen für Nicht-Administratoren aus; `User.IsAdmin` | Private Quellen zusätzlich auf die fachliche Identität Martin begrenzen |
| Tests | `PluginManagerTests`, `PluginSettingsServiceTests`, SQLite-Testmuster | Neue Tests für Sicherheit, GitHub, ZIP, Installation, Reload und Wiederholungen |

## Festgestellte Risiken

- `PluginManager` verwendet nicht-sammelnde `AssemblyLoadContext`s. Ein Austausch von DLLs darf daher nicht einfach neben bereits geladenen Typen erfolgen.
- `InitializeAsync` entdeckt direkt aus den produktiven Plugin-Verzeichnissen und synchronisiert anschließend die Datenbank. Eine separate Validierungs- und Installationsphase fehlt.
- `PluginSetting` hat derzeit nur Plugin- und Laufzeitstatus; Download-, Release- und Validierungsfehler können nicht differenziert persistiert werden.
- Das aktuelle UI ist auf Aktivierung und Reihenfolge lokaler Plugins ausgelegt. PAT, private Repositorydaten und interne Secretwerte dürfen nicht in neue UI-Modelle gelangen.
- Die Datenbank wird beim Anwendungsstart per Migration aktualisiert. Neue Entitäten benötigen daher eine EF-Core-Migration und passende SQLite-Kompatibilität.

## Lifecycle-Hinweis

Die Bestandsaufnahme wurde in dieser Umgebung direkt ausgeführt, weil kein separater Unteragent verfügbar war. Die fachlichen Artefakte wurden ausschließlich unter diesem Feature-Verzeichnis erstellt.
