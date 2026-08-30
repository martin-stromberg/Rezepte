# Paketreferenzen und lokale Quelle

## Direkte Referenzen

| Datei | Fundstelle | Aktuelle Version |
|---|---:|---|
| `Rezepte.Web/Rezepte.Web.csproj` | Zeile 22 | `0.7.0-rc.11` |
| `Rezepte.Tests/Rezepte.Tests.csproj` | Zeile 20 | `0.7.0-rc.11` |
| `Rezepte.Updater.TestHost/Rezepte.Updater.TestHost.csproj` | Zeile 18 | `0.7.0-rc.11` |

Alle drei Referenzen verwenden denselben Paketnamen und dieselbe alte Version. Es gibt keine weitere direkte `PackageReference` auf `msTools.Updater`.

## Paketquelle

`NuGet.config` konfiguriert die Quelle `local-repo-packages` mit dem Pfad `lib/nuget`. Dort liegen die bisherigen Pakete `0.7.0-rc.9`, `0.7.0-rc.10` und `0.7.0-rc.11` sowie die neue Datei `msTools.Updater.0.10.0.nupkg`.

Die neue Paketdatei ist im Arbeitsbaum vorhanden, aber beim Inventar noch nicht als Git-Datei erfasst. Restore und Build sollten mit der Repository-Konfiguration und der lokalen Quelle gegen genau diese Version geprüft werden.
