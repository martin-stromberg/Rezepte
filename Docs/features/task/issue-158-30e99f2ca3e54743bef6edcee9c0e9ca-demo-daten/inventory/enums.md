# Enums – Bestandsaufnahme „Demo-Daten bei Registrierung"

## `BackgroundJobStatus`
Datei: `Rezepte.Web/Services/BackgroundJobs/BackgroundJob.cs` (Zeilen 9–31)

| Wert | Bedeutung |
|------|-----------|
| `Pending = 0` | In Warteschlange |
| `Running = 1` | Wird ausgeführt |
| `Succeeded = 2` | Erfolgreich abgeschlossen |
| `Failed = 3` | Fehlgeschlagen |
| `Cancelled = 4` | Abgebrochen |

## `RecurrenceType`
Datei: `Rezepte.Web/Entities/CalendarEvent.cs` (Zeilen 9–16)

| Wert | Bedeutung |
|------|-----------|
| `None = 0` | Keine Wiederholung |
| `Weekly = 1` | Wöchentliche Wiederholung |

## `WeekDays` (`[Flags]`)
Datei: `Rezepte.Web/Entities/CalendarEvent.cs` (Zeilen 22–47)

| Wert | Bedeutung |
|------|-----------|
| `None = 0` | Keine Tage |
| `Monday = 1` … `Sunday = 64` | Bitmaske der Wochentage für `Weekly`-Wiederholung |

Es existieren keine Enums für Demo-Daten oder Job-Typen; Job-Typen werden als Strings vergeben (`export:user`, `export:all`).
