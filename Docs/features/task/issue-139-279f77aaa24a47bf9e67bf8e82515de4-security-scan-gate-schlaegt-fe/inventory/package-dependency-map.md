# Paket- und Abhangigkeitskarte

## Direkte Referenzen

- `Rezepte.Web/Rezepte.Web.csproj:17`: `Microsoft.EntityFrameworkCore.Sqlite` `10.0.9`.
- `Rezepte.Tests/Rezepte.Tests.csproj:21`: `bunit` `1.38.5`.
- `Rezepte.Tests.Browser/Rezepte.Tests.Browser.csproj:23`: Projektreferenz auf `Rezepte.Web`, keine eigene SQLite- oder AngleSharp-Referenz.

## Aufgeloste betroffene Pakete

Der Scan vom aktuellen Branch lost folgende Pakete auf:

| Projekt | Paket | Aufgelost | Herkunft |
|---|---|---:|---|
| `Rezepte.Web` | `SQLitePCLRaw.lib.e_sqlite3` | `2.1.11` | transitiv aus EF Core SQLite |
| `Rezepte.Tests` | `SQLitePCLRaw.lib.e_sqlite3` | `2.1.11` | transitiv uber `Rezepte.Web` |
| `Rezepte.Tests.Browser` | `SQLitePCLRaw.lib.e_sqlite3` | `2.1.11` | transitiv uber `Rezepte.Web` |
| `Rezepte.Tests` | `AngleSharp` | `1.2.0` | transitiv aus bUnit |

Der Outdated-Check meldet fur EF Core SQLite `10.0.11` als aktuelle direkte Version und fur die SQLitePCLRaw-Komponenten `3.0.5` beziehungsweise `3.53.3`. Diese Information ist eine Momentaufnahme der konfigurierten NuGet-Quellen; die Zielversion muss im Plan gegen Kompatibilitat und Scanresultat verifiziert werden.

## Erwartete Änderungsorte

Voraussichtlich zu ändern sind nur die Paketversionen in `Rezepte.Web/Rezepte.Web.csproj` und `Rezepte.Tests/Rezepte.Tests.csproj`, eventuell weitere direkt betroffene Testprojektdateien nach erneuter Paketauflosung. `obj/project.assets.json` und Buildartefakte sind generiert und nicht zu committen.
