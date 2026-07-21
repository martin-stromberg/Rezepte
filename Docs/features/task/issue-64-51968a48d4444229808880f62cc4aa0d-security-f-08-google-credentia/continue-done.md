# Offene Aufgaben

Erstellt am: 2026-07-21
Abbruchgrund: Kein Fortschritt zwischen den letzten zwei Iterationen

Die folgenden Aufgaben konnten im automatisierten Zyklus nicht abgeschlossen werden
und müssen manuell oder in einem erneuten Lauf bearbeitet werden.

## Offene Planelemente

Keine.

## Code-Review-Befunde

- [x] `IGoogleCredentialsProvider.cs` — Ungenutzte `using`-Direktiven `using System;` (Zeile 1) und `using System.IO;` (Zeile 2) entfernt.
- [x] `IGoogleCredentialsProvider.cs` — `<returns>`-Element im XML-Kommentar von `GetGeminiApiKey()` mit „The resolved Gemini API key, or an empty string if none is configured." befüllt.
- [x] `GoogleCredentialsProviderTests.cs` — Direkten Positivtest `ServiceAccountFileExists_ReturnsTrue_WhenPathIsSetAndFileExists` ergänzt (nutzt `Path.GetTempFileName()`, löscht die Datei danach in einem `finally`-Block).
- [x] `EnvironmentVariableScope.cs` — Duplizierung bewusst als Test-Isolation dokumentiert (Kommentar über den Konstanten); keine `internal const`/`InternalsVisibleTo`-Kopplung eingeführt, da im Projekt bisher keine solche Infrastruktur existiert.

## Fehlgeschlagene Tests

Keine (195/195 Tests bestanden; der zuvor in Iteration 1 gemeldete Fehlschlag in `PluginManagerTests.DiscoverFromDirectory_ShouldNotKeepAssembliesLoadedFromTemporaryDirectory` ist ein unabhängiger, vorbestehender GC-/AssemblyLoadContext-Timing-Flake ohne Bezug zu dieser Anforderung und trat in Iteration 2 nicht mehr auf).
