# Offene Aufgaben

Erstellt am: 2026-07-21
Abbruchgrund: Kein Fortschritt zwischen den letzten zwei Iterationen

Die folgenden Aufgaben konnten im automatisierten Zyklus nicht abgeschlossen werden
und müssen manuell oder in einem erneuten Lauf bearbeitet werden.

## Offene Planelemente

Keine.

## Code-Review-Befunde

- [ ] `IGoogleCredentialsProvider.cs` — Ungenutzte `using`-Direktiven `using System;` (Zeile 1) und `using System.IO;` (Zeile 2) entfernen; keine Typen aus diesen Namespaces werden verwendet.
- [ ] `IGoogleCredentialsProvider.cs` — Leeres `<returns></returns>`-Element im XML-Kommentar von `GetGeminiApiKey()` (Zeile 25–29) ausfüllen, z. B. „The resolved Gemini API key, or an empty string if none is configured.", analog zu den übrigen Methodenkommentaren.
- [ ] `GoogleCredentialsProviderTests.cs` — Direkten Positivtest für `ServiceAccountFileExists()` ergänzen (Pfad ist gesetzt und Datei existiert real, z. B. via `Path.GetTempFileName()`, → `true`; temporäre Datei danach löschen). Bisher wird der Positivfall nur indirekt über `SettingsCredentialAvailabilityTests` abgedeckt.
- [ ] `EnvironmentVariableScope.cs` — Duplizierte Umgebungsvariablen-Namen `"GOOGLE_APPLICATION_CREDENTIALS"` / `"GOOGLE_GEMINI_API_KEY"` (Zeile 5–6) sind identisch zu den privaten Konstanten in `GoogleCredentialsProvider.cs` (Zeile 8–9) und laufen Gefahr, bei einer Umbenennung stillschweigend zu divergieren (niedrige Priorität). Entscheiden: entweder Namen als `internal const` in der Produktionsklasse zentralisieren und im Test referenzieren, oder die Duplizierung bewusst als Test-Isolation dokumentieren.

## Fehlgeschlagene Tests

Keine (195/195 Tests bestanden; der zuvor in Iteration 1 gemeldete Fehlschlag in `PluginManagerTests.DiscoverFromDirectory_ShouldNotKeepAssembliesLoadedFromTemporaryDirectory` ist ein unabhängiger, vorbestehender GC-/AssemblyLoadContext-Timing-Flake ohne Bezug zu dieser Anforderung und trat in Iteration 2 nicht mehr auf).
