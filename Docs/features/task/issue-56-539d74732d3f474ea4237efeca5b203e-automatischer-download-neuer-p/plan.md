# Umsetzungsplan: Automatischer Download neuer Plugins

## Ziel und Abgrenzung

Beim Anwendungsstart werden die in den administrativen Einstellungen konfigurierten GitHub-Pluginquellen einmalig geprüft. Für jede aktivierte Quelle wird das neueste veröffentlichte GitHub-Release ermittelt, dessen Release-Tag als Version verwendet wird. Das ZIP-Asset dieses Releases wird serverseitig geladen, sicher geprüft und erst nach erfolgreicher Assembly-Discovery in den aktiven Pluginbestand übernommen.

Die Quellen sind global für die Anwendung. Als Administrator gekennzeichnete Anwender verwalten sie in den Einstellungen; es gibt keine separate Martin-Identität und keine zusätzliche Einschränkung privater Quellen. Die Nutzung bereits geladener Plugins bleibt unverändert. Der Plan umfasst ausschließlich den Download und die Installation durch den Administrator, der die Webanwendung hostet.

Es gibt keinen periodischen Hintergrundprozess und keinen manuellen laufenden Updateprozess. Die Einstellungen dienen der Konfiguration; die Prüfung erfolgt ausschließlich beim Anwendungsstart. Eine verbindliche ZIP-Signaturprüfung ist nicht Bestandteil dieser Umsetzung.

## Leitentscheidungen und Randbedingungen

- `PluginSource` wird als globale Konfiguration persistiert. URL, Sichtbarkeit, Aktivierung, Vertrauensbestätigung und Update-/Fehlerstatus stehen nicht im Benutzerkontext.
- Nur Administratoren dürfen Quellen in der Plugin-Einstellungsseite hinzufügen, ändern, aktivieren, deaktivieren oder entfernen. Die serverseitige Autorisierung ist maßgeblich; die UI-Ausblendung allein genügt nicht.
- Die Vertrauensbestätigung ist ausschließlich beim Hinzufügen einer Quelle erforderlich. Änderungen an einer bestehenden Quelle lösen keine erneute Bestätigung aus, sofern die Quelle weiterhin dieselbe gespeicherte Konfiguration darstellt.
- Für private Repositories wird der PAT ausschließlich serverseitig aus dem verschlüsselten Secret-Storage des Systems gelesen. Die Einstellungen speichern die notwendige Secret-Referenz bzw. Konfiguration in der Datenbank; der PAT selbst wird nicht in der Datenbank, in UI-Modellen oder Logs abgelegt. Administratoren können ihn über die Einstellungen rotieren.
- Der GitHub-Release-Tag ist die verbindliche Releaseversion. Die neueste veröffentlichte Version wird anhand der GitHub-Releaseinformationen bestimmt.
- Der Assetname ist nicht fest auf `release.zip` verdrahtet. Es wird das ZIP-Asset des neuesten Releases ausgewählt; `latest-release.zip` ist ein gültiges Beispiel. Fehlt ein geeignetes ZIP-Asset, wird die Quelle mit Fehlerstatus beendet und der bestehende Bestand bleibt unverändert.
- Das Archiv darf mehrere Plugin-Unterverzeichnisse enthalten. Nach Prüfung werden diese Unterverzeichnisse in das Plugin-Verzeichnis der Anwendung übernommen. Die bestehende Plugin-Discovery bleibt die fachliche Quelle für die Erkennung.
- Als Lauffähigkeitsprüfung genügt Assembly-Discovery über den bestehenden Mechanismus, ausgeführt gegen das temporär entpackte Paket. Eine isolierte Instanziierung und Ausführung von Plugins ist nicht erforderlich.
- Die Prüfung und Installation werden über einen dedizierten Dienst mit Anwendungssperre serialisiert. Der alte Bestand bleibt bis zur erfolgreichen Validierung unverändert. Austausch und Reload erfolgen wiederherstellbar; bei einem Fehler wird der vorherige Bestand zurückgespielt.
- Fehlgeschlagene Releases und Verarbeitungsschritte werden pro Quelle und Releaseversion dauerhaft gespeichert, damit ein Startlauf nicht dieselbe fehlerhafte Version endlos erneut verarbeitet. Ein erneuter Versuch ist Bestandteil eines späteren Anwendungsstarts oder einer administrativen Konfigurationsänderung.
- PAT, Authorization-Header und Secretwerte dürfen weder protokolliert noch an das Frontend übertragen werden.

## Umsetzungsschritte

### 1. Fachliches Datenmodell und Einstellungen

1. Eine globale `PluginSource`-Entität für kanonische GitHub-Repository-URL, öffentliche/private Sichtbarkeit, Aktivierung, Vertrauensbestätigung, Secret-Referenz und Zeitstempel ergänzen.
2. Eine Release-/Paketpersistenz für Release-Tag, GitHub-Release-/Assetidentität, Download-, Validierungs-, Installations- und Reloadstatus, Fehlertext sowie letzte erfolgreiche Version anlegen.
3. Eindeutige Indizes für Quelle und Releaseverarbeitung definieren und Statusänderungen nachvollziehbar persistieren.
4. `RezepteDbContext` erweitern und eine EF-Core-Migration für die bestehende SQLite-Konfiguration erstellen.
5. Alle erforderlichen nicht geheimen Optionen in der administrativen Plugin-Einstellungsseite erfassbar machen und in der Datenbank speichern. PAT-Werte werden über den System-Secret-Storage verschlüsselt gespeichert und über dieselbe Seite rotiert.

### 2. GitHub-Releaseermittlung und Download

1. Einen mockbaren `IGitHubReleaseClient` mit Releaseermittlung und Assetdownload ergänzen. Öffentliche Zugriffe erfolgen ohne Token; private Zugriffe verwenden ausschließlich den serverseitig gelesenen PAT.
2. Repository-URLs kanonisieren und nur zulässige GitHub-Repositories akzeptieren.
3. Das neueste veröffentlichte Release bestimmen, den Release-Tag als Version übernehmen und daraus das geeignete ZIP-Asset auswählen. Der konkrete Assetname darf variieren; ein nicht vorhandenes oder nicht auswertbares ZIP führt zu einem dauerhaften Fehlerstatus ohne Installation.
4. API-Fehler, Rate-Limits, Timeouts und Cancellation kontrolliert behandeln. Download und Verarbeitung müssen anhand von Quelle, Release-Tag und Assetidentität idempotent sein.

### 3. ZIP- und Discovery-Validierung

1. Das Archiv in einem eindeutig erzeugten temporären Arbeitsverzeichnis entpacken und absolute Pfade, Pfadüberläufe, beschädigte Archive sowie unerlaubte Inhalte ablehnen.
2. Die im Archiv enthaltenen Plugin-Unterverzeichnisse und die projektseitig festgelegten zulässigen Meta-Dateien validieren. Mehrere Plugin-Unterverzeichnisse sind zulässig; Identität und Konflikte werden gegen die bestehende Plugin-Discovery und Persistenz geprüft.
3. Die Discovery gegen das temporäre Plugin-Verzeichnis ausführen. Das Ergebnis darf vor der Installation weder den aktiven Bestand noch produktive Pluginsettings verändern.
4. Validierungsfehler mit Quelle und Release-Tag persistieren. Nur ein vollständig erfolgreich entdecktes Paket erreicht die Installationsphase.

### 4. Installation, Rollback und Reload

1. Eine Installationskomponente für Backup, wiederherstellbaren Verzeichniswechsel und Bereinigung ergänzen. Die geprüften Plugin-Unterverzeichnisse werden in das Plugin-Verzeichnis der Anwendung übernommen.
2. Den Austausch gegen parallele Läufe sperren und laufende Handler sowie nicht sammelnde `AssemblyLoadContext`s mit dem vorhandenen Plugin-Lebenszyklus koordinieren.
3. Nach dem Austausch `PluginManager.InitializeAsync` oder einen gleichwertigen kontrollierten Reload ausführen. Bei Austausch- oder Reloadfehlern den Backupbestand wiederherstellen und die vorherige Version aktiv lassen.
4. Eine Releaseversion erst nach erfolgreichem Reload als erfolgreich geladen markieren; Zwischen- und Fehlerzustände bleiben dauerhaft nachvollziehbar.

### 5. Startausführung

1. Einen scoped `PluginUpdateService` für die sequenzielle Verarbeitung aller aktivierten globalen Quellen implementieren.
2. Einen dünnen, einmalig beim Anwendungsstart laufenden Hosted-Service registrieren. Er startet nach der bestehenden Plugin-Grundinitialisierung, respektiert Cancellation und beendet sich nach der Prüfung.
3. Keinen periodischen Timer und keinen manuellen Update-Trigger implementieren. Konfigurationsänderungen und PAT-Rotation erfolgen ausschließlich über die administrativen Einstellungen und wirken beim nächsten Anwendungsstart.
4. Strukturierte Logs auf Quelle, Release und Status begrenzen; Geheimnisse bleiben ausgeschlossen.

### 6. Administrative Einstellungen und Autorisierung

1. `PluginSettingsService`, `PluginSettingsItem`, `SettingsViewModel` und `PluginSettings.razor` um globale Quellenverwaltung, Vertrauensbestätigung beim Hinzufügen, Aktivierung, Secret-Rotation und Statusanzeige erweitern.
2. Serverseitig sicherstellen, dass ausschließlich als Administrator gekennzeichnete Anwender diese Konfiguration verwalten können.
3. PAT und sensible Secretinformationen niemals in UI-Modelle, API-Antworten, Blazor-State oder Logs übernehmen.
4. Bestehende lokale Pluginaktivierung, Reihenfolge und Pluginnutzung unverändert weiterführen.

### 7. Tests und Abnahmekriterien

1. Persistenztests für globale Quellen, Administratorgrenzen, Vertrauensbestätigung, Secret-Referenz, Releasehistorie und idempotente Statusübergänge ergänzen.
2. GitHub-Clienttests für Releaseauswahl nach Tag, variable ZIP-Assetnamen, öffentliche/private Authentifizierung, Rate-Limit, Timeout, Cancellation und PAT-Ausschluss ergänzen.
3. Validator-Tests für gültige Mehrfach-Pluginpakete, absolute Pfade, Pfadüberläufe, unerlaubte Inhalte und beschädigte Archive ergänzen.
4. Discovery-, Installations-, Überschreibungs-, Rollback- und Reloadtests ergänzen; ein Fehler darf weder den aktiven Bestand ersetzen noch eine Version als erfolgreich markieren.
5. Tests für die einmalige Startausführung, Cancellation, deaktivierte Quellen, bereits verarbeitete Releases und geheime Werte in UI/Logs ergänzen.

## Reihenfolge und Lieferumfang

Die Implementierung erfolgt in der Reihenfolge Datenmodell/Einstellungen, GitHub-Client, ZIP-/Discovery-Validierung, Installer/Reload, einmalige Startausführung, Autorisierung und Tests. Migration, DI-Registrierung und fokussierte Tests werden jeweils mit der zugehörigen Komponente geliefert. Dieser Plan enthält keine Änderungen an produktivem Code; diese folgen erst in Schritt 6.

## Offene Punkte

Keine
