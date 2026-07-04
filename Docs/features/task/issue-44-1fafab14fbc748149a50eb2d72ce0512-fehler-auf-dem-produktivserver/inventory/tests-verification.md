# Detail: Test- und Verifikationslage

## Vorhandene Tests

`Rezepte.Tests` enthaelt Unit-Tests fuer zentrale Services, unter anderem:

- `UserServiceTests`
- `RecipeServiceTests`
- `CookbookServiceTests`
- `ShoppingListServiceTests`
- `SettingsServiceTests`
- `AiUsageServiceTests`

Ein gezielter Komponenten-/Render-Test fuer `Rezepte.Web/Components/Settings/UserProfile.razor` wurde nicht gefunden. Auch ein Publish-/Runtime-Smoke-Test ist nicht als Testziel oder Skript im Repository sichtbar.

## Relevante Verifikation fuer diese Anforderung

Fuer die weitere Planung sind zwei Absicherungen sinnvoll:

- Build/Publish-Verifikation: `dotnet publish Rezepte.Web -c Release -f net10.0 -r linux-x64 --self-contained false` muss erfolgreich laufen und die erwarteten Runtime-Anforderungen klar dokumentieren.
- Produktionsnahe Runtime-Pruefung: Auf dem Zielserver muss `dotnet --info` mindestens passende `Microsoft.NETCore.App`- und `Microsoft.AspNetCore.App`-Eintraege fuer .NET 10 zeigen. Alternativ muss das Deployment self-contained erfolgen.

## Lokaler Pruefstand

Lokal wurde `dotnet publish` fuer `linux-x64` framework-abhaengig ausgefuehrt. Der Publish war erfolgreich, zeigte aber bestehende Warnungen:

- `NU1903` fuer `SQLitePCLRaw.lib.e_sqlite3 2.1.11`
- mehrere bestehende C#-Warnungen, insbesondere Duplicate-Using-, Nullable- und Obsolete-Warnungen

Diese Warnungen blockieren den Publish nicht. Keine der sichtbaren Warnungen erklaert direkt die fehlende Assembly `System.Runtime.Serialization.Primitives`.

## Empfohlene Regression

Eine robuste Regression sollte mindestens einen dieser Wege abdecken:

- Ein Smoke-Test, der den Release-Publish erzeugt und prueft, ob `Rezepte.Web.runtimeconfig.json` die erwarteten Frameworks referenziert und die Deployment-Doku dazu passt.
- Ein produktionsnaher Starttest des Publish-Outputs in einer Linux-Umgebung mit installierter .NET-10-Runtime.
- Falls im Projekt Testinfrastruktur fuer Razor-Komponenten ergaenzt wird: ein Render-Test fuer `UserProfile`, der `InputText` und `ValidationMessage` nach erfolgreichem `LoadAsync` rendert.

## Offene Informationen

Diese Punkte bleiben fuer die Umsetzungsplanung wichtig:

- Welche exakte Ausgabe liefert `dotnet --info` auf dem Produktionsserver?
- Wird der Publish-Output vollstaendig und unveraendert nach `/var/www/rezepte` kopiert?
- Startet der systemd-Service tatsaechlich `Rezepte.Web.dll`, den nativen Host `Rezepte.Web` oder noch den dokumentierten Namen `Rezepte.dll`?
- Tritt dieselbe Exception auf anderen Seiten mit `InputText` ebenfalls auf?

