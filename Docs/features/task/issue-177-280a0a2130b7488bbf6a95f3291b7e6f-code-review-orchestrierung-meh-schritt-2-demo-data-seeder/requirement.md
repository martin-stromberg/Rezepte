# Anforderungsübersetzung

## Metadaten

- Aufgaben-ID: `280a0a21-30b7-488b-bf6a-95f3291b7e6f` (Projekt Issue #177, Schritt 2 von 2)
- Branch: `task/issue-177-280a0a2130b7488bbf6a95f3291b7e6f-code-review-orchestrierung-meh-schritt-2-demo-data-seeder`
- Basisbranch: `task/issue-177-280a0a2130b7488bbf6a95f3291b7e6f-code-review-orchestrierung-meh`
- Kundenanforderung: „Service-Einstiegspunkt für ‚Demo-Daten für einen Benutzer anlegen'" (Entwicklungsschritt 2 des Projektplans zu Issue #177, Korrektur von Review-Befund 2)

## Fachliche Zusammenfassung

Der Geschäftsvorfall „Demo-Daten für einen neuen Benutzer anlegen" wird heute fachlich sequenziert im Hintergrund-Job-Handler `DemoDataSeedingJobHandler` komponiert (Komposition in `HandleAsync` plus private Sequenzmethoden `SeedCookbooksAsync`, `SeedRecipesAsync`, `SeedCalendarAsync`, `SeedShoppingListAsync`). Der Vorfall wird als Einstiegspunkt im Service-Layer gekapselt — Richtung `IDemoDataSeeder.SeedForUserAsync(userId, ct)` nach dem Muster der übrigen fachlichen Service-Einstiegspunkte. Die gesamte fachliche Sequenz samt Nebenbedingungen (Fehlerabbruch je Schritt, tolerante Behandlung fehlender Demo-Bilder aus `DemoData/demo-images.zip`, keine Retries) wandert in den Service. Der Job-Handler löst den Geschäftsvorfall danach nur noch aus: Nutzlast lesen, Benutzer prüfen, Service-Einstiegspunkt aufrufen. Auslöser (Registrierung stellt denselben `seed-demo-data`-Job ein), Job-Registrierung und Demo-Daten-Inhalte bleiben unverändert.

## Fachlicher Scope

- **Verarbeitungseinheit:** Eine Ausführung des Geschäftsvorfalls „Demo-Daten für einen Benutzer anlegen" — die vollständige fachliche Sequenz für genau einen Benutzer: Demo-Kochbücher → Demo-Rezepte inkl. Bilder → fünf Kalenderevents (Folgetage, 18:00 Uhr) → Standardgruppe sicherstellen + Einkaufsliste mit Zutaten des ersten geplanten Rezepts befüllen.
- **Auslösende Ereignisse:** Abarbeitung des Hintergrund-Jobs `seed-demo-data` durch `BackgroundJobHostedService` → `DemoDataSeedingJobHandler.HandleAsync`. Der Job wird unverändert bei der Benutzerregistrierung (`UserService.RegisterAsync`, `createDemoData == true`) eingestellt.
- **Erwartete Wirkung:** Nach der Registrierung mit Demo-Daten sieht der Benutzer dieselben Demo-Inhalte wie bisher: 5 Demo-Kochbücher, 43 Demo-Rezepte inkl. Bildern (soweit im ZIP vorhanden), 5 Kalenderevents um 18:00 Uhr auf den Folgetagen sowie eine befüllte Einkaufsliste in der Standardgruppe. Fehlerverhalten unverändert: Teilanlage bei Fehlern, Job auf `Failed`, kein Retry, keine Transaktion.
- **Umgang mit fehlenden Objekten:**
  - Benutzer aus der Job-Nutzlast nicht vorhanden: verwerfen — `InvalidOperationException`, Job schlägt fehl (unverändert, Prüfung verbleibt im Handler).
  - `DemoData/demo-images.zip` nicht vorhanden oder einzelnes Bild fehlt: tolerieren — Warnung protokollieren, Seeding ohne Bild(er) fortsetzen (unverändert).
  - Standardgruppe der Einkaufsliste nicht vorhanden: erzeugen — über `IShoppingListService.EnsureDefaultGroupAsync` (unverändert).
  - Fehlschlag eines Domänenaufrufs (außer `AddImageAsync`): abbrechen — `InvalidOperationException`, Teildaten bleiben bestehen (unverändert, explizit kein transaktionaler Umbau).
- **Scope-Ausschlüsse:**
  1. Kein transaktionaler Umbau des Seedings: Bei Fehlern bleiben Teildaten bestehen; es gibt weder Rollback noch Retry. — Status: bestätigt (im Entwicklungsschritt explizit als „geklärte Randbedingung"/Nicht-Ziel vorgegeben).
  2. Der neue Service-Einstiegspunkt erhält keinen eigenen externen Auslöser (kein Endpunkt, kein UI-Einstieg); er wird ausschließlich vom Job-Handler aufgerufen. — Status: bestätigt (Auslöseweg laut Entwicklungsschritt unverändert).

## Betroffene Klassen und Komponenten

- Neu: `IDemoDataSeeder` + `DemoDataSeeder` im Service-Layer unter `Rezepte.Web/Services/` (Platzierung nach Konvention der bestehenden fachlichen Service-Einstiegspunkte; ggf. eigener Unterordner, z. B. `DemoData/`). Nimmt die gesamte Sequenz inkl. `demo-images.zip`-Logik und `DemoDataSource`-Iteration auf.
- `Rezepte.Web/Services/BackgroundJobs/Handlers/DemoDataSeedingJobHandler.cs` — Reduktion auf: `DemoDataSeedPayload` parsen, `IUserService.GetByIdAsync` prüfen, `IDemoDataSeeder.SeedForUserAsync` aufrufen. `JobTypeName`, `JobType` und Handler-Registrierung unverändert.
- `Rezepte.Web/Extensions/ServiceCollectionExtensions.cs` — DI-Registrierung des neuen Services (scoped).
- Unverändert: `UserService.RegisterAsync` (Auslöser), `DemoDataSeedPayload`, `DemoDataSource`, `IBackgroundJobQueue`/`BackgroundJobHostedService`, `Rezepte.Web.csproj` (Copy der `demo-images.zip`), Domänen-Service-Signaturen (`ICookbookService`, `IRecipeService`, `ICalendarService`, `IShoppingListService`).
- Tests: `Rezepte.Tests` — bisherige `DemoDataSeedingJobHandler*Tests` bauen die komplette Sequenz-Mock-Landschaft nach; Sequenz-Assertions verlagern sich in Tests des neuen Services (`DemoDataSeederTests`), der Handler-Test verifiziert nur noch Payload-Parsing, Benutzerprüfung, Delegation, Fehlerdurchreichung. Registrierungs-/Job-Enqueue-Tests und `Rezepte.Tests.Browser/Auth/RegisterTests.cs` bleiben unverändert (E2E-Nachweis).
- Feature-Doku: `Docs/help/user-accounts.md` (Abschnitt „Technischer Hintergrund", ca. Zeilen 40–44) an die Service-Kapselung anpassen.

## Implementierungsansatz

- Neuer fachlicher Service-Einstiegspunkt `IDemoDataSeeder` mit Operation `SeedForUserAsync(string userId, CancellationToken ct)`; Implementierung `DemoDataSeeder` als scoped Service mit injizierten Domänen-Services (`ICookbookService`, `IRecipeService`, `ICalendarService`, `IShoppingListService`) und `ILogger`.
- Die privaten Sequenzmethoden und Hilfsfunktionen (`OpenDemoImages`, `AttachImageAsync`, `GetImageContentType`, `SeededRecipe`-Record) ziehen inhaltlich unverändert vom Handler in den Service um.
- `DemoDataSeedingJobHandler` wird auf die reine Auslöserolle reduziert; die bisher über `scopeServices` aufgelösten Domänen-Services entfallen dort, `IUserService` und `IDemoDataSeeder` werden weiterhin über `scopeServices` aufgelöst (Handler-Infrastruktur).
- Kein Datenmodell-, Migrations- oder Konfigurationsumbau.

## Konfiguration

Keine Konfigurationsänderungen erforderlich.

## Akzeptanzkriterien

1. Für den Geschäftsvorfall existiert ein Service-Einstiegspunkt im Service-Layer (`IDemoDataSeeder.SeedForUserAsync`); der Job-Handler enthält keine fachliche Sequenzkomposition mehr und löst nur noch aus (Nutzlast parsen, Benutzer prüfen, Service aufrufen).
2. Die gesamte fachliche Sequenz inkl. Nebenbedingungen (Fehlerabbruch je Schritt, tolerante Behandlung fehlender Demo-Bilder, keine Retries) liegt im Service; die `demo-images.zip`-Logik ist Teil der Sequenz im Service.
3. Die per Registrierung erzeugten Demo-Daten sind fachlich unverändert: gleiche Demo-Kochbücher, gleiche Demo-Rezepte inkl. Bilder, fünf Kalenderevents um 18:00 Uhr auf Folgetagen, Einkaufsliste in der Standardgruppe; Fehlerverhalten (Teilanlage ohne Retry) unverändert.
4. `JobTypeName`, Job-Registrierung und der Auslöser (Registrierung stellt denselben `seed-demo-data`-Job ein) bleiben unverändert.
5. Handler-Tests verifizieren nur noch Payload-Parsing, Benutzerprüfung, Delegation und Fehlerdurchreichung; Sequenz-Assertions liegen in Tests des neuen Services; Registrierungs-/Job-Enqueue-Tests und `Rezepte.Tests.Browser/Auth/RegisterTests.cs` bleiben unverändert und dienen als E2E-Nachweis.
6. Die Solution baut, `dotnet test Rezepte.Tests` läuft grün; der Doku-Abschnitt zum technischen Hintergrund der Demo-Daten (`Docs/help/user-accounts.md`) wird an die Service-Kapselung angepasst.

## Explizite Nicht-Ziele

- Kein transaktionaler Umbau des Seedings (Teilanlage ohne Retry bleibt).
- Keine Änderung der Demo-Daten-Inhalte (Kochbücher, Rezepte, Bilder, Anzahlen, Uhrzeiten).
- Keine Änderung an Job-Infrastruktur, `JobTypeName`, Job-Enqueue oder Registrierungslogik.
- Kein neuer externer Auslöser (Endpunkt/UI) für den Service-Einstiegspunkt.
- Keine Änderung an Domänen-Service-Signaturen.

## Offene Fragen

- Keine. Platzierung und Benennung des neuen Services folgen den Konventionen der bestehenden fachlichen Service-Einstiegspunkte und werden in der Planung anhand der Bestandsaufnahme festgelegt.
