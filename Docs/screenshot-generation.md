# Screenshot-Erzeugung

Diese Anleitung dokumentiert, wie die Screenshots und das animierte GIF in der README erzeugt wurden, damit der Vorgang bei neuen Features wiederholt werden kann.

## Voraussetzungen

- [.NET SDK 10](https://dotnet.microsoft.com)
- [Node.js](https://nodejs.org/) (mind. v18)
- Chromium-Browser fuer Playwright

## Verwendete Tools

| Tool | Zweck |
|---|---|
| `dotnet publish` | Erzeugt die Release-Ausgabe von `Rezepte.Web` mit korrekt ausgelieferten statischen Assets. |
| `playwright` (Node.js) | Startet Chromium headless, fuehrt Registrierung/Demo-Daten-Seeding durch und erstellt die Screenshots. |
| `pngjs` | Dekodiert die PNG-Dateien fuer die GIF-Erzeugung. |
| `gifenc` | Erzeugt das animierte GIF aus den einzelnen Screenshots. |

Die Skripte liegen unter [`scripts/screenshots/`](../scripts/screenshots/):

- [`generate-screenshots.js`](../scripts/screenshots/generate-screenshots.js) – erstellt die PNGs.
- [`make-gif.js`](../scripts/screenshots/make-gif.js) – erzeugt `Docs/screenshots/demo.gif`.

## Ablauf

1. **Abhaengigkeiten installieren**

   ```powershell
   cd scripts/screenshots
   npm install
   # Einmalig Chromium fuer Playwright bereitstellen:
   npx playwright install chromium
   ```

2. **Anwendung veroeffentlichen**

   ```powershell
   dotnet publish Rezepte.Web -c Release
   ```

3. **Screenshots erstellen**

   ```powershell
   cd scripts/screenshots
   node generate-screenshots.js
   ```

   Das Skript startet die veroeffentlichte Anwendung auf einem zufaelligen Port, registriert den ersten Benutzer mit aktivierter Demodaten-Option, wartet auf das Background-Job-Seeding und nimmt nacheinander Screenshots der gewuenschten Seiten auf. Alle Bilder werden in `Docs/screenshots/` gespeichert.

4. **GIF erzeugen**

   ```powershell
   node make-gif.js
   ```

   Das Skript liest die PNGs aus `Docs/screenshots/`, schneidet sie auf 1280×900 Pixel zu und schreibt `Docs/screenshots/demo.gif` mit einer Bild-Anzeigezeit von 2 Sekunden pro Frame.

## Wichtige Details

- **Viewport:** Die Aufnahmen verwenden einen Viewport von 1280×900 Pixel. Soll ein Screenshot die ganze Seite zeigen, kann in `generate-screenshots.js` `fullPage: false` auf `fullPage: true` geaendert werden. Fuer das GIF werden alle Bilder anschliessend in `make-gif.js` auf 1280×900 zugeschnitten.
- **Wartebedingungen:** Die Skripte warten nicht einfach auf `networkidle`, weil Blazor Server eine offene WebSocket-Verbindung haelt. Stattdessen werden seiten-spezifische Selektoren und JavaScript-Polling genutzt (z. B. bis fuenf Kochbuecher, 43 Rezepte, fuenf Kalendereintraege und mindestens ein Einkaufslisten-Item sichtbar sind).
- **Demodaten:** Der erste registrierte Benutzer wird automatisch Administrator. Die Checkbox `createDemoData` loest den Hintergrundjob aus, der fuenf Kochbuecher, 43 Rezepte, fuenf Kalendereintraege und Einkaufslisteneintraege anlegt.
- **Temporaere Datenbank:** Das Skript legt fuer jeden Lauf eine neue SQLite-Datenbank im System-Temp-Verzeichnis an und entfernt sie danach.

## Erweiterung fuer neue Features

1. In `generate-screenshots.js` eine neue `page.goto`/`page.screenshot`-Sequenz ergaenzen.
2. Die neue PNG-Datei in das `frames`-Array in `make-gif.js` eintragen, damit sie auch im GIF erscheint.
3. Die README ggf. aktualisieren.
