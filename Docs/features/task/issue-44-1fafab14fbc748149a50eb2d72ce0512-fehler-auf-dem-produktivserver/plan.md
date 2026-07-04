# Umsetzungsplan

## Ziel

Die Profil-Einstellungen muessen auf dem produktiven Server nach erfolgreichem `GET /api/users/me` ohne `System.IO.FileNotFoundException` fuer `System.Runtime.Serialization.Primitives, Version=10.0.0.0` gerendert werden.

Der Fehler wird primaer als Runtime-/Deployment-Inkonsistenz behandelt. `UserProfile` und `UserProfileViewModel` bleiben fachlich unveraendert, solange die Verifikation keinen lokalen Renderfehler nachweist.

## Leitentscheidung

`System.Runtime.Serialization.Primitives` ist bei `net10.0` Bestandteil des .NET-Shared-Frameworks. Bei framework-abhaengigem Publish wird die Assembly deshalb nicht als Datei in das Publish-Verzeichnis kopiert, sondern von `Microsoft.NETCore.App` auf dem Zielsystem geladen.

Der Fix soll deshalb nicht zuerst eine explizite NuGet-Referenz fuer diese Runtime-Assembly einfuehren. Stattdessen werden Publish- und Betriebsanweisungen so korrigiert, dass der Produktionsserver entweder passende .NET-10-Shared-Frameworks bereitstellt oder ein self-contained Publish verwendet wird.

## Arbeitsschritte

1. Runtime-Anforderungen im Deployment eindeutig machen
   - `Docs/install.md` ueberarbeiten.
   - Framework-abhaengiges Deployment klar an installierte Shared Frameworks binden:
     - `Microsoft.NETCore.App` fuer .NET 10
     - `Microsoft.AspNetCore.App` fuer .NET 10
   - Pruefbefehl fuer den Server dokumentieren:
     - `dotnet --info`
     - Kontrolle, dass beide Shared Frameworks in passender .NET-10-Version vorhanden sind.
   - Erklaeren, dass Framework-Assemblies wie `System.Runtime.Serialization.Primitives.dll` bei `--self-contained false` nicht im Publish-Ordner liegen muessen.

2. Verlaessliche Publish-Option dokumentieren
   - In `Docs/install.md` zusaetzlich eine self-contained Variante fuer unsichere oder nicht kontrollierte Server-Runtimes aufnehmen:
     - `dotnet publish Rezepte.Web -c Release -f net10.0 -r linux-x64 --self-contained true`
   - Entscheidungskriterium dokumentieren:
     - Framework-abhaengig, wenn der Server die passende .NET-10-Runtime hat.
     - Self-contained, wenn die Server-Runtime nicht verlaesslich installiert oder aktualisierbar ist.

3. systemd-Beispiel korrigieren
   - In `Docs/install.md` den falschen Assembly-Namen `Rezepte.dll` ersetzen.
   - Fuer framework-abhaengiges Deployment den erzeugten Namen verwenden:
     - `ExecStart=/usr/bin/dotnet /var/www/rezepte/Rezepte.Web.dll`
   - Optional den nativen Host als Alternative dokumentieren:
     - `ExecStart=/var/www/rezepte/Rezepte.Web`
   - Sicherstellen, dass `WorkingDirectory=/var/www/rezepte` erhalten bleibt.

4. README mit Deployment-Hinweis synchronisieren
   - Den vorhandenen Deployment-Abschnitt in `README.md` knapp anpassen.
   - Auf `Docs/install.md` als verbindliche Schritt-fuer-Schritt-Anleitung verweisen.
   - Erwaehnen, dass bei framework-abhaengigem Publish passende .NET-10-Shared-Frameworks auf dem Server erforderlich sind.

5. Publish-/Runtime-Smoke-Test als automatisierte Verifikation ergaenzen
   - Einen fokussierten Test im Testprojekt ergaenzen, der die dokumentierte Deployment-Konfiguration prueft.
   - Der Test soll ohne echten Linux-Server auskommen und mindestens sicherstellen:
     - `Docs/install.md` verweist nicht mehr auf `Rezepte.dll`.
     - `Docs/install.md` nennt `Rezepte.Web.dll` oder den nativen Host `Rezepte.Web`.
     - `Docs/install.md` dokumentiert `dotnet --info`.
     - `Docs/install.md` dokumentiert die erforderlichen Shared Frameworks `Microsoft.NETCore.App` und `Microsoft.AspNetCore.App`.
     - `README.md` enthaelt den Hinweis auf passende .NET-10-Shared-Frameworks oder die self-contained Alternative.
   - Geeigneter Ort: neuer Test z. B. `Rezepte.Tests/Deployment/DeploymentDocumentationTests.cs`.

6. Lokale Build- und Publish-Verifikation ausfuehren
   - `dotnet test`
   - `dotnet publish Rezepte.Web -c Release -f net10.0 -r linux-x64 --self-contained false`
   - Optional, falls lokal zeitlich vertretbar:
     - `dotnet publish Rezepte.Web -c Release -f net10.0 -r linux-x64 --self-contained true`
   - Erwartung fuer framework-abhaengigen Publish:
     - Publish erfolgreich.
     - `Rezepte.Web.runtimeconfig.json` referenziert `Microsoft.NETCore.App` und `Microsoft.AspNetCore.App`.
     - `System.Runtime.Serialization.Primitives.dll` muss nicht im Publish-Ordner liegen.

7. UI-Code nur bei Gegenbeweis anfassen
   - `Rezepte.Web/Components/Settings/UserProfile.razor` und `Rezepte.Web/ViewModels/UserProfileViewModel.cs` nicht refaktorieren, solange Tests und Publish-Befund die Runtime-Ursache bestaetigen.
   - Kein Austausch von `InputText`, `EditForm` oder `ValidationMessage` als Workaround, weil der Stacktrace auf fehlende Framework-Assembly und nicht auf fehlerhafte Binding-Logik zeigt.

## Betroffene Dateien

- `Docs/install.md`
- `README.md`
- `Rezepte.Tests/Deployment/DeploymentDocumentationTests.cs` (neu)

Nicht geplant:

- `Rezepte.Web/Components/Settings/UserProfile.razor`
- `Rezepte.Web/ViewModels/UserProfileViewModel.cs`
- `Rezepte.Web/Rezepte.Web.csproj`

Diese Dateien werden nur geaendert, wenn die Verifikation eine konkrete lokale Ursache ausserhalb des Deployments zeigt.

## Akzeptanzkriterien

- Die Installationsdokumentation beschreibt korrekt, wie ein framework-abhaengiges .NET-10-Deployment auf dem Produktionsserver betrieben wird.
- Die Installationsdokumentation nennt eine self-contained Alternative fuer Server ohne passende Runtime.
- Das systemd-Beispiel startet den tatsaechlich erzeugten Einstiegspunkt `Rezepte.Web.dll` oder den nativen Host `Rezepte.Web`.
- Es gibt einen automatisierten Test, der die kritischen Deployment-Hinweise gegen Regressionen absichert.
- `dotnet test` ist erfolgreich.
- Der framework-abhaengige Release-Publish fuer `linux-x64` ist erfolgreich.

## Risiken

- Die eigentliche Produktionsmaschine kann durch Repository-Aenderungen nicht direkt repariert werden. Nach der Umsetzung muss der Betreiber entweder die passende .NET-10-Runtime installieren oder den self-contained Publish ausrollen.
- Wenn der Server bereits passende Shared Frameworks hat, kann die Ursache ein unvollstaendiger Kopiervorgang, ein falscher Dienstpfad oder eine manuell veraenderte Runtime-Installation sein. Die aktualisierte Doku soll diese Pruefung explizit machen.
- Bestehende Publish-Warnungen wie `NU1903` und C#-Compilerwarnungen sind nicht Teil dieses Fixes, solange sie den Publish nicht blockieren und nicht mit der fehlenden Runtime-Assembly zusammenhaengen.

## Offene Punkte

Keine.
