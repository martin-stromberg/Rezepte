← [Zurück zur Übersicht](../index.md)

# Git-Hooks — Fehlerbehebung

## Lokalisierungsfehler in `.razor`

**Symptom:** `razor-l10n-check.py` meldet hartkodierte UI-Texte.

**Ursache:** In einer Razor-Datei steht ein sichtbarer Text direkt oder in einem Attribut wie `title`, `placeholder`, `alt`, `aria-label`, `label` oder `tooltip`.

**Lösung:**

1. Sicherstellen, dass `IStringLocalizer<UiStrings> Localizer` verfügbar ist.
2. Den Text durch `@Localizer["<EnglishKey>"]` ersetzen.
3. In `Rezepte.Web/Resources/UiStrings.resx` den Schlüssel und den deutschen Wert eintragen.
4. Bei BOM-betroffenen Dateien die UTF-8-BOM entfernen.

## Fehlende XML-Dokumentation

**Symptom:** `csproj-xmldoc-check.py` meldet fehlende `/// <summary>` oder `<param>`-Elemente; Build bricht mit `CS1591` ab.

**Ursache:** Öffentliche oder `protected` Member sind nicht vollständig dokumentiert; das `.csproj` enthält nicht `<GenerateDocumentationFile>` oder `CS1591` nicht als Fehler.

**Lösung:**

1. Im zugehörigen `.csproj` folgende Eigenschaften einfügen:

   ```xml
   <PropertyGroup>
     <GenerateDocumentationFile>true</GenerateDocumentationFile>
     <WarningsAsErrors>CS1591</WarningsAsErrors>
   </PropertyGroup>
   ```

2. Für jeden öffentlichen Member einen `///`-Kommentar ergänzen.
3. Für Methoden auch `<param>`, `<returns>`, `<typeparam>` und ggf. `<response code="...">` verwenden.

## Throw-only Member

**Symptom:** `no-notimplemented-check.py` meldet Member, deren Body nur aus einem `throw` besteht.

**Ursache:** Stubs oder Fakes simulieren Fehler mit `throw new NotImplementedException(...)` oder `throw new InvalidOperationException(...)`.

**Lösung:**

1. Den Körper so umformen, dass er nicht ausschließlich aus einem `throw` besteht, z. B. durch eine lokale Variable, einen Aufrufzähler oder `Task.FromException(...)`.
2. Sicherstellen, dass die ursprüngliche Fehlersimulation unverändert bleibt, wenn sie Testzwecken dient.

## Fehlende Enum-Abdeckung in Tests

**Symptom:** `enum-coverage-check.py` meldet Enum-Werte, die in keinem Test referenziert sind.

**Ursache:** In `switch`-Ausdrücken oder Mappings werden nicht alle Werte eines Enums verwendet; die Tests nennen mindestens einen Wert nicht.

**Lösung:**

1. Im Testprojekt eine Methode ergänzen, die jeden fehlenden Enum-Wert mindestens einmal referenziert, z. B. über `Enum.GetValues<T>()` iterieren oder ihn in Assertions verwenden.

## Formatierungs- oder Kodierungsfehler

**Symptom:** `dotnet format --verify-no-changes` oder `check-encoding.ps1` schlägt fehl.

**Ursache:** C#- oder Razor-Code entspricht nicht der C#-Formatierungskonvention; Dateien enthalten UTF-8-BOM oder ASCII-Ersatzschreibungen wie `fuer` / `koennen` / `geloescht`.

**Lösung:**

1. `dotnet format Rezepte.sln` ausführen, um Formatierungsabweichungen zu korrigieren.
2. Dateien als reines UTF-8 ohne BOM speichern.
3. Umlaute in Benutzertexten, Kommentaren und Ressourcendateien korrekt verwenden; ASCII-Ersatzschreibungen vermeiden.
