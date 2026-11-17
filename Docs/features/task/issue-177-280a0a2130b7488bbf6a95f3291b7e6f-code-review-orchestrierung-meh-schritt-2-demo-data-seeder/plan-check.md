# Plan-Gegenprüfung

## Ergebnis

**Status:** Plan vollständig

## Abgleich Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| AK1: Service-Einstiegspunkt im Service-Layer; Handler nur noch Auslöser | Neue Klassen `IDemoDataSeeder`/`DemoDataSeeder`; `HandleAsync` auf Payload + Benutzerprüfung + Aufruf reduziert (Abschnitte „Neue Klassen", „Änderungen", Programmablauf) | `DemoDataSeedingJobHandlerTests` (Delegation mit korrekter UserId) | Abgedeckt |
| AK2: Gesamte Sequenz + Nebenbedingungen (Fehlerabbruch, tolerante Bilder, keine Retries) im Service; `demo-images.zip`-Logik Teil der Sequenz | Umzug aller Sequenzmethoden inkl. `OpenDemoImages`/`AttachImageAsync`/`GetImageContentType` und Konstanten 1:1 (Designentscheidungen, Programmablauf Schritt 5) | `DemoDataSeederTests_Sequence` + `DemoDataSeederTests_Errors` inkl. toleranter Bild-Fehlerfall | Abgedeckt |
| AK3: Demo-Daten fachlich unverändert; Fehlerverhalten unverändert | 1:1-Übernahme der Sequenz, Scope-Ausschluss 1 (kein Transaktions-Umbau) | `DemoDataSeederTests_*` (gleiche Assertions wie bisher) + E2E `Register_WithDemoData_CreatesDemoData` | Abgedeckt |
| AK4: `JobTypeName`, Job-Registrierung, Auslöser unverändert | Plan ändert Handler nur in `HandleAsync`; `UserService.RegisterAsync`, `IBackgroundJobQueue`, DI der Handler unverändert | Unveränderte `UserServiceTests`-Enqueue-Tests + E2E | Abgedeckt |
| AK5: Testverlagerung (Sequenz → Service-Tests; Handler → Auslöser); Registrierungs-/Browser-Tests unverändert | Abschnitt „Tests" benennt neue Testklassen, betroffene und unveränderte Tests | `DemoDataSeederTests_Sequence`, `DemoDataSeederTests_Errors`, reduzierte `DemoDataSeedingJobHandlerTests` | Abgedeckt |
| AK6: Build/Tests grün; Doku-Abschnitt angepasst | Umsetzungsschritte 5–6 | Build-/Testlauf + `Docs/help/user-accounts.md` | Abgedeckt |

## Fehlende oder unvollständige Testanforderungen

Keine — alle Akzeptanzkriterien haben Testnachweis. Der „ZIP fehlt"-Pfad ist nicht separat unit-testbar, ohne die 1:1-Übernahme zu verändern (`DemoImagesPath` ist statisch an `AppContext.BaseDirectory` gebunden und das Archiv liegt im Test-Output vor); der tolerante Einzelbild-Fehlerfall ist durch `SeedForUserAsync_WhenImageAttachFails_Continues` abgedeckt, die Archiv-Logik wird unverändert übernommen.

## E2E-Abdeckung

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| Registrierung mit Demo-Daten → unveränderte sichtbare Inhalte (AK2–4) | `RegisterTests.Register_WithDemoData_CreatesDemoData` (bestehend, unverändert) | Abgedeckt |
| Neue Benutzerflüsse | — | Nicht erforderlich mit Begründung: rein interne Sequenz-Verlagerung, kein neuer/geänderter Nutzer-Auslöser; bestehender E2E-Test deckt den vollständigen Fluss ab |

## Fehlende oder unvollständige Planbestandteile

Keine — Scope-Klärung vollständig, beide Scope-Ausschlüsse bestätigt, Happy-Path-Akzeptanzszenario vom Nutzer-Auslöser bis zur sichtbaren Wirkung vorhanden, Fehler-/Ausgangszustände (fehlender Benutzer, fehlendes ZIP, Domänenfehler) benannt.

## Hinweise

- Der Plan weist die statische `DemoImagesPath`-Bindung korrekt aus: Sie verhindert einen Unit-Test des „Archiv fehlt"-Pfads ohne produktive Änderung; der Verzicht ist begründet und im Plan dokumentiert.
- `SeededRecipe`-Record und private Konstanten ziehen mit um — im Code-Review auf versehentliche Verhaltensabweichungen achten.
