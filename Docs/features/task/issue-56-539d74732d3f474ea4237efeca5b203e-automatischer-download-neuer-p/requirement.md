# Fachliche Zusammenfassung

Die bestehende Plugin-Verwaltung wird um konfigurierbare GitHub-Plugin-Quellen und einen regelmäßigen Updateprozess erweitert. Für jede Quelle wird die neueste veröffentlichte `release.zip` ermittelt, anhand ihrer Version mit bereits geladenen Versionen verglichen und bei einer neuen Version serverseitig heruntergeladen. Vor dem Installieren oder Überschreiben werden Inhalt und Lauffähigkeit der enthaltenen Plugins in einem temporären Verzeichnis geprüft.

Öffentliche und private Repositories werden unterstützt. Private Repositories dürfen ausschließlich durch Martin verwendet werden; der dafür erforderliche Personal Access Token (PAT) bleibt im Backend und wird weder an das Frontend ausgegeben noch protokolliert.

# Betroffene Klassen und Komponenten

Die folgenden Artefakte sind voraussichtlich betroffen oder neu zu erstellen. Die konkreten Namen sind Vorschläge, da im Projekt keine zentrale Feature-Dokumentation unter `docs/features.md` vorhanden ist.

- **Datenmodell und Persistenz:**
  - Neue Entität `PluginSource` für Repository-URL, Sichtbarkeit, Vertrauensbestätigung, Aktivierungsstatus und Updateinformationen.
  - Neue Entität oder Erweiterung der Plugin-Persistenz für geladene Release-Versionen und Download-/Validierungsstatus.
  - Erweiterung von `RezepteDbContext` und neue EF-Core-Migration.
- **Plugin-Verwaltung:**
  - Erweiterung von `PluginManager` und `IPluginManager` um kontrolliertes Installieren, Überschreiben und anschließende Erkennung von Plugins.
  - Erweiterung von `PluginSettingsService` und `PluginSettingsItem` zur Anzeige der Quellen- und Updatezustände.
  - Abstimmung mit `PluginStartupService`, damit laufende Plugins während Download, Prüfung und Austausch nicht in einen inkonsistenten Zustand geraten.
- **Download und Updateprozess:**
  - Neuer Dienst, beispielsweise `PluginUpdateService`, für die regelmäßige Prüfung aktivierter Quellen und die Verarbeitung neuer Releases.
  - Neuer Hintergrunddienst, beispielsweise `PluginUpdateHostedService`, für den regelmäßigen Auslöser.
  - GitHub-Client, beispielsweise `IGitHubReleaseClient` und `GitHubReleaseClient`, für öffentliche API-Aufrufe und serverseitig authentifizierte Zugriffe auf private Repositories.
  - Konfigurationsoptionen für Prüfintervall, GitHub-API und serverseitig hinterlegten PAT bzw. dessen Secret-Namen.
- **Validierung und Installation:**
  - ZIP-Validator, beispielsweise `PluginPackageValidator`, der genau eine erforderliche `release.zip`, die erlaubte Verzeichnisstruktur und zulässige Meta-Dateien prüft.
  - Temporärer Ausführbarkeits- bzw. Ladeprüfer, beispielsweise `PluginRuntimeValidator`, vor jeder Installation oder Überschreibung.
  - Installationskomponente, beispielsweise `PluginPackageInstaller`, mit atomischem bzw. wiederherstellbarem Austausch der Plugin-Verzeichnisse.
- **Benutzeroberfläche und Autorisierung:**
  - Erweiterung der administrativen Einstellungen unter `Rezepte.Web.Components.Settings.PluginSettings` um Quellenverwaltung, Vertrauensbestätigung, Statusanzeige und manuelles Update-Auslösen.
  - Anpassung von `SettingsViewModel` und gegebenenfalls eines zugehörigen ViewModels bzw. Controllers.
  - Autorisierungsprüfung für private Quellen auf den Benutzer Martin; PAT-Werte dürfen nicht in UI-Modellen oder API-Antworten enthalten sein.
- **Tests:**
  - Tests für Quellenpersistenz, Vertrauensbestätigung und Berechtigungen.
  - Tests für GitHub-Releaseermittlung, Versionsvergleich und PAT-Ausleitung.
  - Tests für ZIP-Struktur, fehlende oder falsche Release-Assets, Pfadüberläufe, beschädigte Archive und unzulässige Inhalte.
  - Tests für temporäre Plugin-Ladeprüfung, Installation, Überschreiben, Fehlerbehandlung und regelmäßige Ausführung.

# Implementierungsansatz

1. Plugin-Quellen werden als eigene, aktivierbare Konfiguration persistiert. Das Hinzufügen einer Quelle ist erst nach einer expliziten Vertrauensbestätigung zulässig. Die Vertrauensentscheidung und die Quelle müssen eindeutig einem Benutzer bzw. dem vorgesehenen administrativen Bereich zugeordnet werden.
2. Ein regelmäßig laufender Hintergrunddienst verwendet einen abstrahierten GitHub-Client. Bei öffentlichen Repositories erfolgt der Zugriff ohne Geheimnis; bei privaten Repositories verwendet ausschließlich das Backend einen aus sicherem Secret-Speicher gelesenen PAT. Der PAT wird nicht an das Frontend weitergegeben und nicht geloggt.
3. Der Client ermittelt das neueste veröffentlichte Release und sucht gezielt nach einem Asset mit dem Namen `release.zip`. Fehlt das Release, das Asset oder eine auswertbare Version, wird kein Download bzw. keine Installation durchgeführt. Die zuletzt erfolgreich verarbeitete Version wird persistiert und zur Vermeidung erneuter Downloads verwendet.
4. Das ZIP wird in einem isolierten temporären Verzeichnis entpackt. Die Paketvalidierung erlaubt nur Plugin-Ordner und ausdrücklich zugelassene Meta-Dateien. Absolute Pfade, Pfadüberläufe, unerwartete Dateitypen und sonstige Inhalte führen zum Abbruch.
5. Vor dem Austausch des aktiven Plugin-Bestands werden die entpackten Plugins im temporären Verzeichnis mit dem vorhandenen Discovery-/Lademechanismus geprüft. Nur ein vollständig erfolgreich validiertes Paket darf installiert werden. Fehler werden pro Quelle bzw. Version dauerhaft statusbehaftet erfasst; der bisherige Bestand bleibt bei einem Fehler unverändert.
6. Nach erfolgreicher Installation wird die bestehende Erkennung über `PluginManager.InitializeAsync` wieder ausgeführt oder durch einen geeigneten, atomaren Reload-Mechanismus ersetzt. Die konkrete Strategie muss mit dem Lebenszyklus von `AssemblyLoadContext` und laufenden Handlern abgestimmt werden.

# Konfiguration

- **Plugin-Quellen:** benutzerspezifisch oder administrativ, abhängig von der bestehenden Berechtigungsstruktur der Plugin-Einstellungen; die Entscheidung ist vor der Implementierung zu bestätigen. Pro Quelle werden GitHub-Repository-URL, öffentlich/privat, Vertrauensbestätigung, Aktivierung und zuletzt erfolgreich geladene Version gespeichert.
- **Private Quellen:** serverseitige Anwendungskonfiguration bzw. Secret-Verwaltung. Gespeichert wird nur eine Referenz auf den Secret-Namen oder die Secret-Konfiguration, niemals der PAT im Frontend oder in einer regulären Datenbankausgabe. Die Nutzung privater Quellen wird auf Martin beschränkt.
- **Updateintervall:** Anwendungskonfiguration mit einem sinnvollen Standardwert; optional sollte zusätzlich ein manuelles Update über die Einstellungen möglich sein.
- **Sicherheits- und Integritätsprüfung:** Eine ZIP-Signatur ist laut Anforderungskonzept optional und sollte als spätere Erweiterung behandelt werden, sofern sie nicht verbindlich gemacht wird.

# Offene Fragen

- Welche konkrete Benutzeridentität kennzeichnet Martin, und welche Rollen bzw. Claims dürfen öffentliche Quellen verwalten?
- Sind Plugin-Quellen global für die Anwendung oder pro Benutzer konfiguriert? Wer darf eine Quelle hinzufügen, ändern, aktivieren oder entfernen?
- Ist eine explizite Vertrauensbestätigung nur beim Hinzufügen erforderlich oder auch erneut bei Änderungen an URL, Besitzer oder Repository?
- Wie wird die Version der `release.zip` verbindlich bestimmt: GitHub-Release-Tag, Release-ID, Paket-Metadatei oder Plugin-Versionen?
- Muss das Asset exakt `release.zip` heißen, oder sind alternative Namen bzw. mehrere Assets zulässig?
- Wie häufig soll geprüft werden, und soll der Prozess beim Start zusätzlich sofort eine Prüfung ausführen?
- Welche Meta-Dateien sind konkret erlaubt, und wie wird bei mehreren Plugin-Ordnern deren Identität und Versionskonflikt behandelt?
- Was bedeutet „auf Lauffähigkeit geprüft“ technisch: reicht Assembly-Discovery, oder müssen Plugins in einer isolierten Testausführung instanziiert und mit einem Testkontext ausgeführt werden?
- Muss ein Update atomar zurückgerollt werden, wenn die Anwendung nach erfolgreicher Prüfung beim Austausch oder beim anschließenden Laden scheitert?
- Wie werden inkompatible, fehlerhafte oder bereits fehlgeschlagene Versionen markiert, damit sie nicht bei jeder Prüfung erneut geladen werden?
- Welche GitHub-API-Version, Rate-Limit-Behandlung, Timeout- und Retry-Regeln gelten?
- Wo wird der PAT konkret verwaltet, wie wird er rotiert, und soll zusätzlich eine Integritätsprüfung per ZIP-Signatur verbindlich umgesetzt werden?
- Dürfen private Quellen ausschließlich für Martin sichtbar sein, oder dürfen andere Benutzer den reinen Quellennamen und Status sehen?
