# Release Notes

## Important Notes Before Update

- The application now runs on .NET 11. The recommended self-contained release packages already include the runtime — no action required. For framework-dependent deployments, the ASP.NET Core Runtime 11 must be installed on the server before updating.
- Import plugins built for .NET 10 remain compatible and continue to load under .NET 11; rebuilding plugins is not required.

## What's New

- Platform upgrade to .NET 11 for the entire application, including build and release pipeline.
- More robust PDF export: bundled fonts (Lato with Noto Emoji fallback) make generated PDFs independent of the fonts installed on the server; emojis and special characters in recipes are rendered, and rare unsupported characters no longer cause a missing PDF.

## Wichtige Hinweise vor dem Update

- Die Anwendung läuft nun auf .NET 11. Die empfohlenen self-contained Release-Pakete enthalten die Laufzeitumgebung bereits — keine Aktion erforderlich. Bei framework-abhängigen Installationen muss vor dem Update die ASP.NET Core Runtime 11 auf dem Server installiert werden.
- Für .NET 10 gebaute Import-Plugins bleiben kompatibel und werden unter .NET 11 weiterhin geladen; ein Neuübersetzen der Plugins ist nicht erforderlich.

## Neuerungen

- Plattform-Upgrade auf .NET 11 für die gesamte Anwendung inklusive Build- und Release-Pipeline.
- Robusterer PDF-Export: Mitgelieferte Schriftarten (Lato mit Noto-Emoji-Fallback) machen erzeugte PDFs unabhängig von den auf dem Server installierten Schriften; Emojis und Sonderzeichen in Rezepten werden dargestellt, und seltene nicht unterstützte Zeichen führen nicht mehr zu einem fehlenden PDF.
