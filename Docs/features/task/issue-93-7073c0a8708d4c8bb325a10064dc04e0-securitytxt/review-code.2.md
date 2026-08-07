# Code-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### SecurityTxtController.cs (SecurityTxtController)

- **Doppelter Code** — Die drei Action-Methoden `GetSecurityTxt`, `GetSecurityMd` und `GetSecurityHtml` (Zeilen 19–37) folgen exakt demselben Muster: Settings laden, auf `Enabled` prüfen, Renderer aufrufen, Content zurückgeben. Lediglich der Renderer-Aufruf und der Content-Type unterscheiden sich.

  Empfehlung: Private Hilfsmethode `RenderOrNotFound(Func<SecurityTxtSettings, string> render, string contentType, SecurityTxtSettings settings)` extrahieren und aus den drei Actions aufrufen.

### SecurityTxtRenderer.cs (SecurityTxtRenderer)

- **God-Methode / Doppelter Code** — `RenderPlainText`, `RenderMarkdown` und `RenderHtml` (Zeilen 10–55) iterieren alle denselben Satz von Feldern in derselben Reihenfolge mit je einem anderen `Append*`-Aufruf. Der Feld-Katalog ist dreifach repliziert.

  Empfehlung: Einen gemeinsamen internen Durchlauf extrahieren, der eine Liste von `(key, value, multiline)`-Tupeln liefert, und die drei öffentlichen Methoden arbeiten nur noch darüber mit dem jeweiligen Append-Delegate. Dadurch werden neue Felder an einer einzigen Stelle ergänzt.

- **Fehlende HTML-Strukturierung** — `AppendHtmlSection` (Zeile 75) gibt HTML ohne umschließendes Dokument zurück (`<!DOCTYPE html>`, `<html>`, `<body>`). `GetSecurityHtml` liefert `text/html`, aber das Ergebnis ist kein gültiges HTML-Dokument.

  Empfehlung: `RenderHtml` ein minimales HTML-Grundgerüst (`<html><body>…</body></html>`) ausgeben lassen, oder den Content-Type auf `text/html; charset=utf-8` behalten und zumindest ein Fragment-Wrapper-`<div>` ergänzen sowie in der API-Dokumentation klarstellen, dass ein Fragment geliefert wird.

- **Primitive Obsession in `AppendHtmlSection`** — Der `value`-Parameter wird mittels `WebUtility.HtmlEncode` für den `<p>`-Inhalt kodiert, aber mehrzeilige Werte (z. B. `Contact` mit mehreren URIs) werden als einzelner kodierter String in einen einzigen `<p>` geschrieben, was Zeilenumbrüche ignoriert.

  Empfehlung: Im HTML-Renderer denselben `Split('\n')`-Mechanismus wie in `AppendMultiline` verwenden, um mehrzeilige Felder als mehrere `<p>`-Elemente oder `<br>`-getrennte Zeilen auszugeben.

### SettingsController.cs (SettingsController)

- **Fehlende Validierung** — In `SetGlobalSecurityTxt` (Zeile 165) wird `settings` nicht auf `null` geprüft, bevor auf `settings.Enabled` zugegriffen wird. Bei fehlerhaftem Request-Body kann das zu einer `NullReferenceException` statt einem `400 BadRequest` führen.

  Empfehlung: `if (settings == null) return BadRequest("Request-Body fehlt.");` als erste Zeile der Methode ergänzen.

### SecurityTxtSettings.razor (SecurityTxtSettings)

- **Doppelter Code / Primitive Obsession** — Die Formularklasse `SecurityTxtForm` (privates nested record, Zeilen ca. 150 ff.) ist eine 1:1-Kopie des DTOs `SecurityTxtSettings`, abzüglich des `Enabled`-Feldes, das nochmals separat als `bool` vorhanden ist. Das DTO selbst wird direkt in `LoadAsync` und `SaveAsync` auch als Typreferenz verwendet.

  Empfehlung: `SecurityTxtForm` entfernen und direkt `SecurityTxtSettings` binden oder eine explizit benannte Klasse `SecurityTxtFormModel` erstellen und eine Konvertierungsmethode anbieten, um die Absicht klar zu machen. Dadurch entfällt das manuelle Kopieren aller Felder in `LoadAsync` und `SaveAsync`.

- **Fehlende Fehlerbehandlung in `OnEnabledChanged`** — Die Event-Handler-Methode `OnEnabledChanged` (Zeile ca. 120) parst `args.Value` manuell mit `bool.TryParse`. Ein nicht auflösbarer Wert wird stillschweigend als `false` behandelt, ohne dass der Nutzer informiert wird.

  Empfehlung: Für ein Checkbox-`<input>` ist `@bind` mit `@bind:event="onchange"` ausreichend und verlässlicher als manuelles `ChangeEventArgs`-Parsing. Die manuelle Methode kann entfernt werden.

### SecurityTxtControllerTests.cs (SecurityTxtControllerTests)

- **Doppelter Testfall** — `GetWellKnownSecurityTxt_ReturnsOk_WhenEnabled` (Zeile 51) ruft `sut.GetSecurityTxt(...)` auf — also dieselbe Methode wie `GetSecurityTxt_ReturnsOk_WhenEnabled` (Zeile 33). Der Test prüft nicht die `/.well-known/`-Route, sondern denselben Controller-Endpunkt; er ist damit inhaltlich ein Duplikat.

  Empfehlung: Den Test entweder entfernen oder durch einen echten Integrationstest auf `/.well-known/security.txt` ersetzen.

### SettingsServiceTests.cs (SettingsServiceTests)

- **Testmethode mit mehreren fachlichen Fällen** — `ShoppingListEditMode_ShouldPersistInitialValuePerUser` (Zeile ca. 95) kombiniert drei fachliche Prüfungen: Default-Wert, Persistenz für user-1 und Persistenz für user-2. Das ist kein security.txt-spezifischer Code, aber der Test verletzt das Single-Concern-Prinzip für Tests.

  Empfehlung: In separate `[Fact]`-Methoden aufteilen: `ShoppingListEditMode_ShouldReturnFalseByDefault`, `ShoppingListEditMode_ShouldPersistTrueForUser1`, `ShoppingListEditMode_ShouldReturnFalseForUser2`.

## Geprüfte Dateien

- `Rezepte.Web/Controllers/SecurityTxtController.cs`
- `Rezepte.Web/Controllers/SettingsController.cs`
- `Rezepte.Web/Services/SecurityTxtRenderer.cs`
- `Rezepte.Web/Services/ISecurityTxtRenderer.cs`
- `Rezepte.Web/Services/ISettingsService.cs`
- `Rezepte.Web/Services/SettingsService.cs`
- `Rezepte.Web/Dtos/SecurityTxtSettings.cs`
- `Rezepte.Web/Components/Settings/SecurityTxtSettings.razor`
- `Rezepte.Web/ViewModels/SettingsViewModel.cs`
- `Rezepte.Web/Extensions/ServiceCollectionExtensions.cs`
- `Rezepte.Tests/Controllers/SecurityTxtControllerTests.cs`
- `Rezepte.Tests/Controllers/SettingsControllerSecurityTxtValidationTests.cs`
- `Rezepte.Tests/Services/SecurityTxtRendererTests.cs`
- `Rezepte.Tests/Services/SettingsServiceTests.cs`
- `Rezepte.Tests.Browser/SecurityTxtBrowserTests.cs`
- `Rezepte.Tests.Browser/Infrastructure/SecurityTxtPageObject.cs`
