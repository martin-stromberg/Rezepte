# Datenmodell und Dependency Injection

## User-Entity und Datenbankmodell

`Rezepte.Web/Data/RezepteDbContext.cs` konfiguriert `User.Username` aktuell mit:

- `IsRequired()`
- `HasMaxLength(64)`
- eindeutigem Index `HasIndex(u => u.Username).IsUnique()`

Die Anforderung verlangt fachlich 3 bis 20 Zeichen. Eine Service-Validierung kann dies sofort durchsetzen; eine Anpassung der EF-Konfiguration auf `HasMaxLength(20)` waere konsistent, erfordert aber eine Migration und muss mit bestehenden Daten bedacht werden. Die Abgrenzung sagt, dass bestehende Benutzer mit kuenftig gesperrten Namen nicht automatisch umbenannt/deaktiviert werden muessen.

## Eindeutigkeit

Die Eindeutigkeit wird aktuell ueber Service-Abfragen und DB-Index abgesichert. Diese Pruefung darf nicht ersetzt werden. Die neue Validierung sollte vor der Eindeutigkeitspruefung laufen, damit offensichtlich ungueltige Namen schnell mit passender Meldung abgelehnt werden.

Case-insensitive Sperrlistenvergleiche sind gefordert. Case-insensitive Eindeutigkeit fuer normale Namen ist nicht explizit gefordert und waere eine fachliche Erweiterung. Der Bestand verwendet exakte Vergleiche.

## Dependency Injection

`Rezepte.Web/Extensions/ServiceCollectionExtensions.cs` registriert `IUserService` als scoped:

`services.AddScoped<IUserService, UserService>();`

Ein neuer Validator kann dort passend registriert werden, z. B.:

`services.AddSingleton<IUsernameValidator, UsernameValidator>();`

oder scoped, falls er spaeter konfigurations-/datenbankabhaengig wird. Fuer statische Sperrlisten und reine Validierungslogik ist singleton ausreichend.

## Moegliche Dateistruktur

Eine zum Bestand passende Struktur waere:

- `Rezepte.Web/Services/Validation/IUsernameValidator.cs`
- `Rezepte.Web/Services/Validation/UsernameValidator.cs`
- optional `Rezepte.Web/Services/Validation/UsernameValidationResult.cs`

Alternativ kann die Schnittstelle in derselben Datei liegen, wie es bei einigen Services im Projekt bereits vorkommt. Fuer Testbarkeit und Lesbarkeit ist eine separate kleine Validierungskomponente vorzuziehen.

## Konfiguration der Sperrlisten

Die Anforderung verlangt Erweiterbarkeit, aber keine Admin-Oberflaeche. Fuer den ersten Schritt reicht eine zentrale, wartbare Liste im Validator oder in einer dedizierten Optionsklasse. Eine Optionsklasse waere zukunftsfaehiger, wuerde aber mehr Verkabelung erfordern.

Pragmatische Variante fuer die Umsetzung:

- statische `HashSet<string>` fuer exakt reservierte Namen mit `StringComparer.OrdinalIgnoreCase`
- statische Pattern-/Tokenlisten fuer Support-/Security-Namen
- kleine Liste missbraeuchlicher Begriffe als initiale technische Grundlage
- alle Listen an einer Stelle, mit Tests abgesichert

