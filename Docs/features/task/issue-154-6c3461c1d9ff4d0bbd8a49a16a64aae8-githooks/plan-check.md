# Plan-Gegenprüfung

## Ergebnis

**Status:** Plan vollständig

## Abgleich Akzeptanzkriterien

Aus `requirement.md` abgeleitete prüfpflichtige Punkte (die Anforderung formuliert keine nummerierten Akzeptanzkriterien; die Kriterien ergeben sich aus der fachlichen Zusammenfassung und dem Implementierungsansatz):

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| Hooks aus Pattern-Collection sind übernommen und aktiv (`core.hooksPath=.githooks`), keine Prüfung entfällt | Bereits erfolgt (Ist-Zustand, Commit-Basis); AP 1 committed die `.githooks`-Baseline; Designentscheidung „Hooks werden nicht entschärft" | AP-1-Kriterien: `pre-commit` durchläuft; AP 10: `git commit`/`git push` ohne Blocker | Abgedeckt |
| `csproj-xmldoc-check`: 24 `.cs`-Dateien mit unvollständiger XML-Doku beheben | „Änderungen an bestehenden Klassen" → 24 gemeldete `.cs`-Dateien erhalten fehlende `<param>`/`<returns>`-Tags; AP 2, 7–9 | AP 2/7/8/9: `csproj-xmldoc-check.py --all` = 0; AP 10 Endverifikation | Abgedeckt |
| `csproj-xmldoc-check`: 10 `.csproj` mit `GenerateDocumentationFile` + CS1591-als-Fehler konfigurieren | Designentscheidung + Änderungsliste nennen alle 10 Projekte explizit; AP 2 (Rezepte.Web), AP 7 (Tests, PluginFixture), AP 8 (Tests.Browser), AP 9 (5 Import + Updater.TestHost) = 10 | Pro AP: `dotnet build` = 0 + `csproj-xmldoc-check.py --all` = 0 | Abgedeckt |
| `razor-l10n-check`: 35 `.razor`-Dateien lokalisieren (Anwenderentscheidung: alle 35, neutrale `UiStrings.resx` deutsch, Localizer-Name `Localizer`) | Designentscheidungen exakt wie entschieden; AP 3 (9 Dateien inkl. MainLayout/Settings) + AP 4 (6 Pages) + AP 5 (10 Settings-Komponenten) + AP 6 (10 Shared) = 35 — vollständige Mengenabdeckung gegen `inventory/razor-l10n.md` verifiziert | Pro AP: `razor-l10n-check` = 0; AP 6 Kriterium: `--all` = 0 (gesamtes Repo); `translation-check` validiert `Localizer`-Schlüssel gegen `.resx` | Abgedeckt |
| `no-notimplemented-check --all --strict`: 13 throw-only-Stubs in 5 Testdateien beheben | Programmablauf + AP 7: Bodies umbauen, Fehlersimulation bleibt erhalten; alle 5 Dateien mit Zeilen in „Betroffene bestehende Tests" gelistet | AP 7: `no-notimplemented-check.py --all --strict` = 0; `dotnet test Rezepte.Tests` in AP 10 | Abgedeckt |
| `razor-usage-check --all --strict`: 18 falsch-positive „verwaiste" Komponenten auflösen (ohne Check-Änderung) | Drei Ursachen adressiert: BOM-Entfernung (12 Dateien), `typeof(MainLayout)` unqualifiziert in `Routes.razor`, `typeof(...)`-Referenzen in `Settings.razor`-`@code` + `SettingsViewModel`-Anpassung; AP 3 | AP 3: `razor-usage-check.py --all --strict` = 0 | Abgedeckt |
| `enum-coverage-check --all --strict`: 3 Enums vollständig in Tests abdecken | Neue Testklasse `EnumCoverageTests` (bzw. Erweiterung); AP 7; fehlende Werte einzeln benannt | „Neue Tests"-Tabelle: `ImportCollectionItemState.Pending/.Importing`, `WeekDays.Tuesday/.Saturday`, `BackgroundJobStatus.Running/.Failed/.Cancelled`; AP 7: Check = 0 | Abgedeckt |
| Bestehende Prüfungen bleiben erfüllt: `dotnet format --verify-no-changes` und `check-encoding.ps1` | In jedem AP-Akzeptanzkriterium; Risiken Abschnitt nennt Format- und Encoding-Fallen (inkl. ASCII-Transliterierungen in resx/`///`-Kommentaren); `geloescht`-Fix in `RecipeEdit.razor` eingeplant (AP 1 Unstaging, AP 4 Commit) | AP 1–10 jeweils `dotnet format` = 0 und `check-encoding` = 0 | Abgedeckt |
| Checks/Hooks nicht abschwächen, umgehen oder konfigurierbar machen | Explizite Designentscheidung; falsch-positive Befunde werden anwendungsseitig aufgelöst; keine `NoWarn`/Pragmas für XML-Doc | Implizit durch alle Check-Akzeptanzkriterien mit unveränderten Skriptaufrufen | Abgedeckt |
| Absicherung: Solution bauen und Testsuite (`Rezepte.Tests`, `Rezepte.Tests.Browser`) ausführen | AP 10: `dotnet build Rezepte.sln`, `dotnet test Rezepte.Tests`, Browser-Suite als Regressionstest | AP 10 Akzeptanzkriterien | Abgedeckt |
| Verifikation: `git commit` und pre-push ohne blockierende Fehler | AP 10: finaler `git commit` und `git push` durchlaufen beide Hooks | AP 10 Akzeptanzkriterien | Abgedeckt |
| Jeder geplante Commit besteht den aktiven pre-commit (staged-Datei-Logik beachten) | „Programmabläufe → Commit-Gating" erklärt die staged-Datei-Prüfung; AP-Reihenfolge stellt sicher, dass `.cs` erst nach `.csproj`-Konfiguration und `.razor` erst nach Lokalisierung committed werden; AP 1 nimmt `RecipeEdit.razor` aus dem Index | AP-spezifische staged-Check-Kriterien je Arbeitspaket | Abgedeckt |
| Fehlende Artefakte aus dem Quell-Repo (`SecretScan.csproj`/`MarkdownLinkCheck.csproj`) | Inventur verifiziert: existieren im Quell-Repo nicht; pre-commit überspringt sie dynamisch — kein Handlungsbedarf | Nicht erforderlich (Inventur-Feststellung) | Abgedeckt |

## E2E-Abdeckung

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| Kein neuer oder geänderter Benutzerfluss — ausschließlich Qualitätsprüfungen und Befundbehebung | Keine neuen E2E-Tests; bestehende `Rezepte.Tests.Browser`-Suite als Regressionstest in AP 10 eingeplant | Nicht erforderlich mit Begründung (im Plan explizit begründet, Abschnitt „E2E-Tests") |

Die Begründung ist nachvollziehbar: Die Anforderung ändert kein UI-Verhalten — Texte werden 1:1 in `UiStrings.resx` ausgelagert. Einzige bewusste Textänderung ist `Error.razor` (englisch → resx/deutsch); das Risiko für Browser-Tests, die auf die englischen Strings prüfen, ist im Risikoabschnitt benannt und durch den Suite-Lauf in AP 10 abgesichert.

## Hinweise

- **`dotnet format Rezepte.sln --verify-no-changes` wurde in der Inventur nie ausgeführt.** Falls der Bestand Formatierungsverstöße enthält, blockiert bereits AP 1. Empfehlung: vor AP 1 einmal `dotnet format Rezepte.sln` ausführen und das Ergebnis ggf. als eigenen Formatierungs-Commit einplanen (nur `.cs`-Dateien wären dabei staged-Check-pflichtig — reine Whitespace-Änderungen an `.cs` lösen den XML-Doc-Check nur aus, wenn die Datei gestaged ist; das ist in AP 1 unkritisch, da dann noch kein `.csproj` konfiguriert ist — der pre-commit-XML-Doc-Check verlangt bei gestagten `.cs` aber bereits jetzt vollständige `///`-Blöcke und ein konfiguriertes `.csproj`. Ein format-bedingter `.cs`-Commit vor AP 2 wäre daher blockierend. Alternativ `dotnet format` erst ab AP 2 bzw. projektweise einrollen.)
- **`check-encoding.ps1` prüft nur `.cs/.razor/.cshtml/.html/.js/.css/.json/.resx/.xaml/.xml`** (verifiziert, Skriptzeilen 12/21/25) — die Docs-Dateien unter `Docs/features/...` mit Transliterierungswörtern wie „geloescht"/„fuer" sind kein Blocker. In `UiStrings.resx` und `///`-Kommentaren hingegen auf Umlaute statt ASCII-Umschreibungen achten (im Plan als Risiko bereits benannt).
- **Offene Frage „legitime `NotImplementedException`/Ausnahmemarkierung"** aus `requirement.md` ist durch die Inventur faktisch beantwortet (alle 13 Funde sind Test-Fakes; der Check kennt keine Ausnahmemarkierung → Umbau laut Plan).
- **Branch-Blocker `main`/`staging`:** Plan enthält keine explizite Feststellung; Inventur-Annahme (Branch-Namen aus Quell-Repo übernommen, Verhalten korrekt) steht unverändert — nicht planrelevant, da auf dem Task-Branch gearbeitet wird.
- **`Routes.razor`-`@using`-Namenskonflikt** und **`SettingsViewModel`-Refaktor** sind als Risiken benannt; betroffene Unit-Tests sind in „Betroffene bestehende Tests" aufgeführt.
