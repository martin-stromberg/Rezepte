# Release Notes

## Naechste Version

- `msTools.Updater` auf Version `0.10.4` aktualisiert.
- Das lokale NuGet-Paket `lib/nuget/msTools.Updater.0.10.4.nupkg` aufgenommen.
- Den Testhost minimal an die neue `StartScript(string, string?)`-API angepasst.
- Das Security-Scan-Gate durch Aktualisierung bzw. Pinning der verwundbaren transitiven Pakete `SQLitePCLRaw.lib.e_sqlite3` und `AngleSharp` behoben.
- Passwort-Hashing-Policy ergaenzt (Security F-07): `PasswordHasher` definiert Mindest- und Hoechstwerte fuer die PBKDF2-Parameter (100.000 bis 1.000.000 Iterationen, neue Hashes mit 210.000) und lehnt ungueltige oder manipulierte Hash-Strings ohne Exception ab. Beim Login werden Hashes mit veralteten Parametern automatisch neu erzeugt (Rehash-on-Login).
- Betroffene Pfade: `Rezepte.Web/Rezepte.Web.csproj`, `Rezepte.Tests/Rezepte.Tests.csproj`, `Docs/dependencies.md`.
