# Bestandsaufnahme

## Zusammenfassung

Die Anforderung betrifft einen reinen Versionswechsel des NuGet-Pakets `msTools.Updater` von `0.7.0-rc.11` auf `0.10.0`. Im Repository existieren drei direkte `PackageReference`-Einträge, die aktualisiert werden müssen. Die neue Paketdatei liegt bereits unter `lib/nuget/msTools.Updater.0.10.0.nupkg`.

Die bestehende Updater-Laufzeitintegration verwendet die öffentlichen Typen und Erweiterungen des Pakets in `Rezepte.Web` sowie im eigenständigen `Rezepte.Updater.TestHost`. Deshalb ist nach dem Versionswechsel insbesondere die API-/Build-Kompatibilität dieser Integrationen und der zugehörigen Tests zu prüfen. Eine Änderung an Fachlogik, UI, Konfiguration oder Paketquellen ist aus dem aktuellen Bestand nicht erforderlich.

## Betroffene Bereiche

| Bereich | Befund | Relevanz |
|---|---|---|
| Paketreferenzen | Drei Projekte referenzieren `0.7.0-rc.11` direkt | Version auf `0.10.0` ändern |
| Lokale Paketquelle | `NuGet.config` bindet `lib/nuget` ein; das neue `.nupkg` ist vorhanden | Restore muss die bereitgestellte Version auflösen |
| Web-Anwendung | Updater-DI, Hosted Service, Controller und Einstellungsservice verwenden `msTools.Updater` | API-Kompatibilität und Build prüfen |
| Updater-Testhost | Konfiguriert und orchestriert Preflight, Check, Download, Install und Run | Build und Testhost-Verhalten prüfen |
| Tests | Vier Testklassen prüfen direkt oder indirekt die Updater-Integration | Nach Restore gezielt ausführen |
| Konfiguration | `ApplicationUpdateOptions` und bestehende `ApplicationUpdates`-Konfiguration vorhanden | Unverändert weiterverwenden |

## Detaildokumente

- [Paketreferenzen und Quelle](inventory/package-references.md)
- [Laufzeitintegration](inventory/runtime-integration.md)
- [Tests und Testhost](inventory/tests-and-testhost.md)

## Änderungsgrenzen

Voraussichtlich zu ändern sind ausschließlich die drei `PackageReference`-Versionen. Zu prüfen, aber nicht vorab zu ändern, sind die in den Detaildokumenten genannten Updater-Aufrufe, Tests und Build-/Restore-Konfigurationen. Die Paketdatei `lib/nuget/msTools.Updater.0.10.0.nupkg` ist im Arbeitsbaum vorhanden und aktuell nicht versioniert; ihr Umgang ist im weiteren Lifecycle zu berücksichtigen.

## Abweichung vom Workflow

Für diesen Schritt war kein ausführbarer Unteragent verfügbar. Die Bestandsaufnahme wurde daher durch den Hauptagenten anhand des Repository-Inhalts durchgeführt.
