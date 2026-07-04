# Fachliche Zusammenfassung

Auf dem produktiven Server bricht das serverseitige Rendering der Profil-Einstellungen mit einer unbehandelten `System.IO.FileNotFoundException` ab. Der Fehler tritt nach erfolgreichem Abruf von `GET /api/users/me` beim Rendern von Blazor-Formularfeldern in `UserProfile` auf, konkret in `Microsoft.AspNetCore.Components.Forms.InputText` ueber `ExpressionFormatter.FormatLambda`. Die Anwendung muss so angepasst werden, dass die Profilseite auf dem produktiven Server ohne fehlende Assembly `System.Runtime.Serialization.Primitives, Version=10.0.0.0` gerendert werden kann.

# Betroffene Klassen und Komponenten

- Projektdateien und Deployment-Artefakte:
  - `Rezepte.Web/Rezepte.Web.csproj`
  - `Rezepte.Tests/Rezepte.Tests.csproj`
  - ggf. zentrale Build-/Publish-Konfigurationen wie `Directory.Build.props`, `Directory.Packages.props`, `global.json` oder Deployment-Skripte, falls vorhanden
- UI-Komponenten:
  - `Rezepte.Web.Components.Settings.UserProfile`
  - Blazor-Formularfelder auf Basis von `InputText`
- ViewModels:
  - `Rezepte.Web.ViewModels.UserProfileViewModel`
- API/Contracts als Kontext:
  - `Rezepte.Web.Controllers.UsersController`
  - `Rezepte.Web.Contracts.UserProfileDto`
- Tests:
  - Build-/Smoke-Test fuer `Rezepte.Web`
  - Regressionstest oder Komponenten-/Render-Test fuer `UserProfile`, sofern im Projekt testbar
  - Publish-/Runtime-Verifikation, dass alle fuer Blazor Server benoetigten .NET-10-Assemblies vorhanden sind

# Implementierungsansatz

Zuerst ist die Ursache fuer die fehlende Assembly in der Produktionsumgebung zu ermitteln. Aus dem Stacktrace ist ableitbar, dass der Fehler nicht beim API-Aufruf selbst entsteht, sondern beim Rendern der `InputText`-Komponenten, nachdem `UserProfileViewModel.LoadAsync` die Profildaten geladen und `StateHasChanged` ausgeloest hat.

Der Fix soll die Runtime- und Paketkonsistenz zwischen Anwendung, Publish-Output und produktivem Server herstellen. Dabei sind insbesondere das Ziel-Framework `net10.0`, die verwendeten `Microsoft.AspNetCore.*`-/`Microsoft.Extensions.*`-/`Microsoft.EntityFrameworkCore.*`-Versionen und die Bereitstellung von `System.Runtime.Serialization.Primitives` im Publish-Ergebnis zu pruefen. Falls eine explizite Paket- oder Framework-Referenz erforderlich ist, soll sie in der passenden Projekt- oder zentralen Paketkonfiguration ergaenzt werden; falls das Problem durch einen unvollstaendigen Publish oder eine falsche Server-Runtime entsteht, soll die Build-/Deployment-Konfiguration entsprechend korrigiert oder dokumentiert werden.

Die Komponente `UserProfile` soll fachlich unveraendert bleiben, sofern keine technische Anpassung an den Blazor-Formularfeldern notwendig ist. Aenderungen an Profil-API, Authentifizierung oder Nutzerdatenmodell sind nur vorzunehmen, wenn sie fuer die Fehlerbehebung zwingend erforderlich sind.

# Konfiguration

Eine fachliche Anwendungskonfiguration ist nicht erforderlich. Relevant ist die technische Laufzeit- und Deployment-Konfiguration: .NET-Runtime-Version, Publish-Modus, Paketversionen und ggf. selbststaendige vs. framework-abhaengige Bereitstellung muessen zur produktiven Umgebung passen.

# Offene Fragen

- Welche .NET-10-Runtime-Version ist auf dem produktiven Server installiert?
- Wird die Anwendung framework-abhaengig oder self-contained veroeffentlicht?
- Wie wird der produktive Publish-Output erzeugt und auf den Server kopiert?
- Tritt der Fehler nur in `UserProfile` auf oder auch auf anderen Seiten mit `InputText`/`EditForm`?
