# Detail: enum-coverage-check --all --strict (Exit 1)

Der Check verlangt, dass jeder `public`/`internal` Enum-Wert in mindestens einer Testdatei vorkommt.

| Enum | Datei | Nicht in Tests abgedeckte Werte |
|------|-------|--------------------------------|
| `ImportCollectionItemState` | `Rezepte.Import.Abstractions/ImportCollectionModels.cs` | `Pending`, `Importing` |
| `WeekDays` | `Rezepte.Web/Entities/CalendarEvent.cs` | `Tuesday`, `Saturday` |
| `BackgroundJobStatus` | `Rezepte.Web/Services/BackgroundJobs/BackgroundJob.cs` | `Running`, `Failed`, `Cancelled` |

Hinweis: `WeekDays` wird in `Rezepte.Web/Components/Shared/CalendarEventDialog.razor` (Z. 120, 260) per `Enum.GetValues(typeof(WeekDays))` verwendet — das zählt für den Check nicht als Testabdeckung, da nur Testdateien durchsucht werden.
