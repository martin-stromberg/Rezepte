# Code-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### GoogleCredentialsProvider.cs (GoogleCredentialsProvider)

- **Doppelter Code / Fehlende Kapselung** — `GetServiceAccountFilePath()` (Zeilen 18–29) und `GetGeminiApiKey()` (Zeilen 39–50) haben eine strukturell identische Auflösungslogik: Umgebungsvariable lesen, bei `!IsNullOrWhiteSpace` zurückgeben, sonst Options-Wert prüfen, sonst `string.Empty`. Die beiden Methoden unterscheiden sich nur im Namen der Umgebungsvariablen und der herangezogenen Options-Property. Dieselbe Whitespace-Regel wird damit an zwei Stellen dupliziert, was künftige Änderungen (wie der gerade behobene Whitespace-Befund) erneut fehleranfällig für Inkonsistenzen macht.

  Empfehlung: Die gemeinsame Logik in eine private Hilfsmethode auslagern, z. B. `private static string ResolveValue(string environmentVariableName, string? configuredValue)`, die beide öffentlichen Methoden aufrufen. Dadurch existiert die Whitespace-/Fallback-Regel nur noch an einer Stelle.

### GoogleCredentialsProviderTests.cs (GoogleCredentialsProviderTests)

- **Testqualität (unzureichende Testabdeckung / Asymmetrie)** — Für die Whitespace-Behandlung existiert nur der Regressionstest `GetGeminiApiKey_ReturnsEmpty_WhenOptionsValueIsWhitespace` (Zeilen 149–159). Ein analoger Test für `GetServiceAccountFilePath()` (z. B. `GetServiceAccountFilePath_ReturnsEmpty_WhenOptionsValueIsWhitespace`) fehlt. Da der behobene Befund gerade die Gleichbehandlung von Whitespace über beide Methoden hinweg betraf, ist das identische Verhalten von `GetServiceAccountFilePath()` bei reinem Whitespace-Options-Wert aktuell nicht durch einen Test abgesichert und könnte bei einer künftigen Änderung unbemerkt divergieren.

  Empfehlung: Einen spiegelbildlichen Regressionstest für `GetServiceAccountFilePath()` mit einem Whitespace-Options-Wert (`ServiceAccountFilePath = "   "`, Env nicht gesetzt) ergänzen, der `result.Should().BeEmpty()` prüft.

## Geprüfte Dateien

- `Rezepte.Web/Services/GoogleCredentialsProvider.cs`
- `Rezepte.Web/Services/IGoogleCredentialsProvider.cs`
- `Rezepte.Web/Configuration/GoogleCredentialsOptions.cs`
- `Rezepte.Web/Extensions/ServiceCollectionExtensions.cs`
- `Rezepte.Web/Rezepte.Web.csproj`
- `Rezepte.Web/appsettings.json`
- `Rezepte.Tests/Services/GoogleCredentialsProviderTests.cs`
- `Rezepte.Tests/TestHelpers/EnvironmentVariableScope.cs`
- `Rezepte.Tests/TestHelpers/GoogleCredentialsEnvironmentCollection.cs`
- `Rezepte.Tests/Controllers/SettingsCredentialAvailabilityTests.cs`
- `Rezepte.Tests/Deployment/CsprojCredentialCopyTests.cs`
