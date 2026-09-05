← [Zurück zur Übersicht](../index.md)

# Git-Hooks — Installation und Konfiguration

## Voraussetzungen

- Git-Repository mit lokalem Checkout.
- Python 3.
- .NET SDK.
- `powershell.exe` (für den Encoding-Check).
- Windows: `install-hooks.cmd`; Linux/macOS: `install-hooks.sh`.

## Installationsschritte

1. Repository klonen und in den Ordner wechseln.
2. Das passende Skript ausführen:
   - Windows: `install-hooks.cmd`
   - Linux/macOS: `install-hooks.sh`
3. Das Skript richtet `core.hooksPath` auf `.githooks` ein:

   ```
   git config --local core.hooksPath .githooks
   ```

4. Mit `git config --local core.hooksPath` prüfen, dass der Pfad auf `.githooks` zeigt.

## Manuelle Aktivierung

Falls das Skript nicht verwendet werden soll:

```
git config --local core.hooksPath .githooks
```

## Überprüfung

- `git config --local core.hooksPath` zeigt `.githooks`.
- Beim nächsten `git commit` erscheinen Meldungen der einzelnen Checks.
