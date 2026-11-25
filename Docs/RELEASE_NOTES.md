# Release Notes

## Important Notes Before Update

- The application now runs on .NET 11. The recommended self-contained release packages already include the runtime — no action required. For framework-dependent deployments, the ASP.NET Core Runtime 11 must be installed on the server before updating.
- Import plugins built for .NET 10 are **not** compatible with the .NET 11 application and will not load; plugins must be rebuilt and republished for .NET 11.
- Installations that cannot migrate to .NET 11 yet remain supported on the .NET 10 line via the `1.x` maintenance branch (v1.x releases); these releases are installed manually and are not offered through the in-app updater.

## What's New

- Platform upgrade to .NET 11 for the entire application, including build and release pipeline.
- Recipe search: fixed a cancellation race that could sporadically show an error instead of results when searches were started in quick succession.
- More robust PDF export: bundled fonts (Lato with Noto Emoji fallback) make generated PDFs independent of the fonts installed on the server; emojis and special characters in recipes are rendered, and rare unsupported characters no longer cause a missing PDF.

## Wichtige Hinweise vor dem Update

- Die Anwendung läuft nun auf .NET 11. Die empfohlenen self-contained Release-Pakete enthalten die Laufzeitumgebung bereits — keine Aktion erforderlich. Bei framework-abhängigen Installationen muss vor dem Update die ASP.NET Core Runtime 11 auf dem Server installiert werden.
- Für .NET 10 gebaute Import-Plugins sind mit der .NET-11-Anwendung **nicht** kompatibel und werden nicht geladen; Plugins müssen für .NET 11 neu gebaut und neu veröffentlicht werden.
- Installationen, die vorerst nicht auf .NET 11 umsteigen können, werden über den Maintenance-Branch `1.x` auf der .NET-10-Linie weiter versorgt (Releases v1.x); diese Releases werden manuell installiert und nicht über den In-App-Updater angeboten.

## Neuerungen

- Plattform-Upgrade auf .NET 11 für die gesamte Anwendung inklusive Build- und Release-Pipeline.
- Rezeptsuche: Eine Race Condition im Abbruchverhalten wurde behoben, die bei schnell aufeinanderfolgenden Suchen sporadisch einen Fehler statt der Ergebnisse anzeigen konnte.
- Robusterer PDF-Export: Mitgelieferte Schriftarten (Lato mit Noto-Emoji-Fallback) machen erzeugte PDFs unabhängig von den auf dem Server installierten Schriften; Emojis und Sonderzeichen in Rezepten werden dargestellt, und seltene nicht unterstützte Zeichen führen nicht mehr zu einem fehlenden PDF.
