# Release Notes

## Naechste Version

- `msTools.Updater` auf Version `0.10.0` aktualisiert.
- Das lokale NuGet-Paket `lib/nuget/msTools.Updater.0.10.0.nupkg` aufgenommen.
- Den Testhost minimal an die neue `StartScript(string, string?)`-API angepasst.
- Das Security-Scan-Gate durch Aktualisierung bzw. Pinning der verwundbaren transitiven Pakete `SQLitePCLRaw.lib.e_sqlite3` und `AngleSharp` behoben.
- Betroffene Pfade: `Rezepte.Web/Rezepte.Web.csproj`, `Rezepte.Tests/Rezepte.Tests.csproj`, `Docs/dependencies.md`.
