# UI und Autorisierung

## Bestehende UI

`SettingsViewModel` führt einen Eintrag `Plugins`, der an `User.IsAdmin` gebunden ist. `PluginSettings.razor` injiziert `IPluginSettingsService`, lädt lokale Pluginsettings und erlaubt derzeit nur Aktivieren/Deaktivieren sowie das Verschieben der Reihenfolge. Angezeigt werden Name, Plugin-ID, Status, Assembly, Handler-Typ und ein Fehlertext.

Die Komponente verwendet aktuell keinen eigenen Quellen- oder Updateworkflow und löst keinen manuellen Download aus. Es gibt daher auch kein UI-Modell für Repositorydaten, Releaseversionen oder Validierungsstatus.

## Autorisierung

Die vorhandene UI-Grenze ist administrativ. Für private Quellen verlangt die Anforderung zusätzlich eine Beschränkung auf Martin. Die konkrete Benutzeridentität ist im Requirement noch offen; `User` besitzt `Id`, `Username`, `Email` und `IsAdmin`, aber kein festes Martin-Claim oder Rollenmodell für diese fachliche Ausnahme.

Vor der Implementierung muss daher festgelegt werden, ob Martin über eine stabile User-ID, einen konfigurierten Benutzernamen/Claim oder eine dedizierte Berechtigung identifiziert wird. Diese Prüfung muss serverseitig erfolgen und darf nicht ausschließlich aus der sichtbaren UI abgeleitet werden.

## Sicherheitsgrenzen

- Private Quellen dürfen nur berechtigten Nutzern angezeigt und bearbeitet werden.
- PATs dürfen weder in `PluginSettingsItem`, API-Antworten, Blazor-State noch Logs erscheinen.
- Repository-URL, Besitzer, Sichtbarkeit und Vertrauensbestätigung müssen vor dem Speichern validiert werden.
- Änderungen an URL, Besitzer oder Sichtbarkeit sollten eine erneute Vertrauensbestätigung erzwingen.
