# Offene Aufgaben

Erstellt am: 2026-07-21
Abbruchgrund: Kein Fortschritt zwischen den letzten zwei Iterationen

Die folgenden Aufgaben konnten im automatisierten Zyklus nicht abgeschlossen werden
und müssen manuell oder in einem erneuten Lauf bearbeitet werden.

## Offene Planelemente

Keine.

## Code-Review-Befunde

- [ ] `GoogleCredentialsProvider.cs` — `GetServiceAccountFilePath()` und `GetGeminiApiKey()` haben eine strukturell identische Auflösungslogik (Umgebungsvariable lesen, bei `!IsNullOrWhiteSpace` zurückgeben, sonst Options-Wert prüfen, sonst `string.Empty`), die nur im Namen der Umgebungsvariablen und der Options-Property abweicht. Empfehlung: gemeinsame Logik in eine private Hilfsmethode auslagern, z. B. `private static string ResolveValue(string environmentVariableName, string? configuredValue)`, die von beiden öffentlichen Methoden aufgerufen wird.
- [ ] `GoogleCredentialsProviderTests.cs` — Für die Whitespace-Behandlung existiert nur der Regressionstest `GetGeminiApiKey_ReturnsEmpty_WhenOptionsValueIsWhitespace`; ein analoger Test für `GetServiceAccountFilePath()` (z. B. `GetServiceAccountFilePath_ReturnsEmpty_WhenOptionsValueIsWhitespace`, mit `ServiceAccountFilePath = "   "` und nicht gesetzter Env-Variable, Erwartung: leeres Ergebnis) fehlt und sollte ergänzt werden.

## Fehlgeschlagene Tests

Keine (197/197 Tests bestanden).
