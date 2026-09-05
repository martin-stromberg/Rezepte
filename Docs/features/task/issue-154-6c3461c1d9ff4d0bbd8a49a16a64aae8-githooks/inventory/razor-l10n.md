# Detail: razor-l10n-check --all (Exit 1)

35 `.razor`-Dateien mit hartkodierten UI-Strings. Der Check empfiehlt Ersetzung durch `@L["SchlüsselName"]` — **es existiert jedoch kein `L`-Localizer, kein `IStringLocalizer` und keine `.resx` im Repository** (verifiziert). Der Check hat keinen Opt-out-/Suppressionsmechanismus; jede Fundstelle ist ein Exit-1-Fehler.

## `Rezepte.Web/Components/Layout/MainLayout.razor` (11)
Z. 9 `alt="Rezepte"`; Z. 12 `aria-label="Toggle navigation"`; Z. 18 `title="Kochbücher"`; Z. 24 `title="Kalender"`; Z. 30 `title="Einkaufsliste"`; Z. 39 `placeholder="Rezepte suchen..."`, `aria-label="Suche Rezepte"`; Z. 40 `aria-label="Suche starten"`; Z. 52 `title`/`aria-label="Einrichtung"`; Z. 58 `aria-label="Benutzermenü öffnen"`

## `Rezepte.Web/Components/Pages/Calendar.razor` (13)
Z. 11 „Mein Kalender"; Z. 20 „Termin erstellen"; Z. 30 „Der Kalender konnte nicht geladen werden."; Z. 65 „Keine Rezepte eingeplant"; Z. 87 `aria-label="Rezept ansehen"`; Z. 106/109/123/126 `title`+`aria-label` „Bearbeiten"/„Löschen"

## `Rezepte.Web/Components/Pages/CookbookDetails.razor` (4)
Z. 11 „Kochbuch bearbeiten"; Z. 15 `title="Speichern"`; Z. 16 `title="Löschen"`; Z. 27 „Kochbuch nicht gefunden."

## `Rezepte.Web/Components/Pages/CookbookPage.razor` (10)
Z. 17 `title="Bearbeiten"`; Z. 19 `title="Rezept hinzufügen"`; Z. 24 `title="Mehrfachauswahl"`; Z. 46 „Kochbuch nicht gefunden."; Z. 63 „Noch keine Rezepte vorhanden."; Z. 77 `title="Auswählen"`; Z. 89 `alt="Bild zu @recipe.Title"`; Z. 117 „Zu Kochbüchern zuweisen"; Z. 142 „Rezept von URL laden"; Z. 143 URL-Hilfetext

## `Rezepte.Web/Components/Pages/Cookbooks.razor` (4)
Z. 34 „+ Anlegen"; Z. 40 „Noch keine Kochbücher vorhanden."; Z. 60 `alt="Letztes Rezept in @item.Name"`; Z. 77 `title="Verschieben"`

## `Rezepte.Web/Components/Pages/Error.razor` (4)
Z. 7 „An error occurred while processing your request."; Z. 12 „Request ID:"; Z. 16 „Development Mode"; Z. 21 „The Development environment shouldn't be enabled..."

## `Rezepte.Web/Components/Pages/Home.razor` (2)
Z. 13 `title="Neues Rezept anlegen"`; Z. 14 „+ Neu"

## `Rezepte.Web/Components/Pages/Login.razor` (3)
Z. 4 „Rezepte - Anmeldung"; Z. 25 „Angemeldet bleiben"; Z. 34 „Benutzername oder Passwort falsch."

## `Rezepte.Web/Components/Pages/RecipeEdit.razor` (14)
Z. 17–22 `title` „Speichern"/„Abbrechen"/„Bilder verwalten"/„Löschen"; Z. 32 „Rezept konnte nicht geladen werden."; Z. 57 „Quelle (URL)"; Z. 68 `placeholder="Rezept, Rezept-ID oder Rezept-URL als Beilage suchen..."`; Z. 91 „Keine Beilagen hinterlegt."; Z. 122 „Titel (optional)"; Z. 131 „Zubereitungsdauer (Minuten)"; Z. 137 „Ruhezeit über Nacht"; Z. 163 „Zutat hinzufügen"; Z. 176 „Neuen Zubereitungsschritt hinzufügen"

## `Rezepte.Web/Components/Pages/RecipePage.razor` (10)
Z. 25 „Termin planen"; Z. 31 `title="Zutaten zur Einkaufsliste"`; Z. 34 `title="Bearbeiten"`; Z. 37 `title="Zu Kochbüchern zuordnen"`; Z. 52 `title="Quelle aufrufen"`; Z. 55 `aria-label="Quelle aufrufen (öffnet neues Fenster)"`; Z. 69 „Rezept nicht gefunden."; Z. 78 `alt="Rezeptfoto"`; Z. 91 `title="Foto hinzufügen"`; Z. 97 `title="Alle Bilder anzeigen"`

## `Rezepte.Web/Components/Pages/RecipeSearch.razor` (9)
Z. 7 „Rezepte – Suche"; Z. 11 `placeholder`+`aria-label` Suche; Z. 21 „Suche läuft…"; Z. 26 „Die Suche ist fehlgeschlagen. Bitte versuche es erneut."; Z. 30 „Keine Treffer."; Z. 59 `aria-label="Seitennavigation"`; Z. 62/68 `aria-label` „Vorherige"/„Nächste"

## `Rezepte.Web/Components/Pages/Register.razor` (2)
Z. 5 „Rezepte - Ersteinrichtung"; Z. 21 „E-Mail (optional)"

## `Rezepte.Web/Components/Pages/ScheduledRecipes.razor` (5)
Z. 13 „🗓️ Geplante Rezepte"; Z. 16 „Kalender öffnen"; Z. 39 „Keine geplanten Rezepte in den nächsten 3 Tagen."; Z. 49 „Rezept nicht gefunden."; Z. 56 `alt="Bild zu @it.Recipe.Title"`

## `Rezepte.Web/Components/Pages/Settings.razor` (2)
Z. 8 „Rezepte - Einstellungen"; Z. 46 „Bitte anmelden."

## `Rezepte.Web/Components/Pages/ShoppingList.razor` (24)
Z. 20/24 `aria-label` „Anzeigemodus"/„Ansicht"; Z. 25 `title="Ansicht"`; Z. 32/33 `aria-label`/`title` „Bearbeiten"; Z. 40 `title="Gruppe hinzufügen"`; Z. 70 `aria-label="Gruppenname"`; Z. 73/74 `title`/`aria-label` „Gruppe löschen"; Z. 87 „Aus Rezept übernommen"; Z. 92 „Noch keine Zutaten in dieser Gruppe."; Z. 104 `aria-label="Menge und Einheit"`; Z. 108 `aria-label="Zutat"`; Z. 112/113 `title`/`aria-label` „Zutat löschen"; Z. 140/141 `placeholder`/`aria-label` „Menge"; Z. 144/145 „Einheit"; Z. 148/149 „Zutat hinzufügen"; Z. 151 `title`/`aria-label` „Zutat hinzufügen"

## `Rezepte.Web/Components/Settings/AiSettings.razor` (25)
Z. 8 „Nutzung von KI"; Z. 21 „KI global aktiv (Admin)"; Z. 27 „Google Vision global aktiv (Admin)"; Z. 31 „Gemini global aktiv (Admin)"; Z. 36 „Limit Einstellungen"; Z. 39 „Max Anfragen pro Stunde"; Z. 44 „Max Anfragen pro Tag"; Z. 50 „KI global deaktivieren, wenn Limit erreicht"; Z. 53 „Setzen Sie 0 für unbegrenzte Anfragen."; Z. 65 „KI für mein Konto aktivieren"; Z. 77 „Google Vision für mein Konto aktivieren"; Z. 79 `title`/`aria-label` Info; Z. 96 „Gemini für mein Konto aktivieren"; Z. 98 Info; Z. 116 „Bestätigung vor KI‑Nutzung anfordern"; Z. 118 Info; Z. 130/134/138 Sperr-Hinweistexte; Z. 155 „Google Vision"; Z. 157 „Bestätigung vor KI‑Nutzung"; Z. 164/168/172 Erklärtexte zu Google Vision/Gemini/Bestätigung

## `Rezepte.Web/Components/Settings/ApplicationUpdates.razor` (4)
Z. 29 „Zuletzt geprüft"; Z. 42 „Jetzt prüfen"; Z. 58 „RC-Versionen akzeptieren"; Z. 65 „Letzte Ergebnisse"

## `Rezepte.Web/Components/Settings/BackupRestore.razor` (8)
Z. 7 „Sicherung (Admin)"; Z. 8 „Gesamtexport und Wiederherstellung der Anwendung (Admin)."; Z. 17 „Wiederherstellen (ZIP-Datei)"; Z. 38 „Vorgang läuft..."; Z. 50 „Bereinigung alter Sicherungen"; Z. 59 „Uhrzeit der Bereinigung"; Z. 66 „Jetzt bereinigen"; Z. 76 „Bisher wurde noch keine Bereinigung ausgeführt."

## `Rezepte.Web/Components/Settings/ExportData.razor` (4)
Z. 7 „Exportiere deine Rezepte..."; Z. 11 „Bilder einbeziehen"; Z. 16 „PDF pro Rezept"; Z. 30 „Export läuft..."

## `Rezepte.Web/Components/Settings/ExportFilesList.razor` (2)
Z. 6 „Gespeicherte Exporte"; Z. 14 „Keine Exportdateien vorhanden."

## `Rezepte.Web/Components/Settings/PluginSettings.razor` (10)
Z. 35 „PAT aktualisieren"; Z. 41 „Ich vertraue dieser Quelle."; Z. 56 „Keine GitHub-Pluginquellen konfiguriert."; Z. 117 „Keine Import-Plugins gefunden."; Z. 121 „Lokale Import-Plugins"; Z. 164 „nicht nutzbar"; Z. 182 `aria-label="Plugin-Reihenfolge"`; Z. 183/184 `title` „Nach oben"/„Nach unten"

## `Rezepte.Web/Components/Settings/SecurityTxtSettings.razor` (6)
Z. 15 „security.txt (RFC 9116)"; Z. 33 „security.txt aktivieren"; Z. 37 „Contact (...)"; Z. 41 „Expires (Pflichtfeld)"; Z. 49 „Acknowledgments (...)"; Z. 57 „Canonical (optional; ...)"

## `Rezepte.Web/Components/Settings/UsageStats.razor` (3)
Z. 18 „Zeit seit Registrierung"; Z. 19 „Anzahl Kochbücher"; Z. 20 „Eigene Rezepte"

## `Rezepte.Web/Components/Settings/UserAdmin.razor` (6)
Z. 9 „Lade Benutzer…"; Z. 21 `placeholder="Suchen…"`; Z. 25–27 `placeholder` „Benutzername"/„E-Mail (optional)"/„Passwort"; Z. 32 „+ Anlegen"

## `Rezepte.Web/Components/Settings/UserProfile.razor` (8)
Z. 11 „Lade Profil…"; Z. 61 „Passwort ändern"; Z. 69 „Aktuelles Passwort"; Z. 74 „Neues Passwort"; Z. 79 „Neues Passwort bestätigen"; Z. 85 „Passwort ändern"; Z. 95 „Bitte anmelden, um das Profil zu verwalten."

## `Rezepte.Web/Components/Shared/AddRecipeToShoppingListDialog.razor` (5)
Z. 14 „Zutaten übernehmen"; Z. 15 `aria-label="Schließen"`; Z. 25 „Lade Zutaten..."; Z. 29 „Dieses Rezept hat keine Zutaten."; Z. 71 „Zutaten der Beilagen sind nicht vorausgewählt."

## `Rezepte.Web/Components/Shared/AssignToCookbooksOverlay.razor` (3)
Z. 11 „Zuweisung zu Kochbüchern"; Z. 12 „Wähle aus, in welchen Kochbüchern dieses Rezept erscheinen soll."; Z. 23 „Keine Kochbücher vorhanden."

## `Rezepte.Web/Components/Shared/CalendarEventDialog.razor` (9)
Z. 17 „Rezept (optional)"; Z. 50/51 „Kein Rezept ausgewählt"/„Klicken, um ein Rezept auszuwählen"; Z. 57 `placeholder="Rezept‑Id oder per Auswahl wählen"`; Z. 118 „Wöchentlich an"; Z. 147 „Rezept (optional)"; Z. 154 „Kein Rezept ausgewählt"; Z. 201 `placeholder="Rezept suchen..."`; Z. 238 „Keine Treffer."

## `Rezepte.Web/Components/Shared/CreateRecipeDialog.razor` (8)
Z. 14 „Neues Rezept anlegen"; Z. 15 „Wähle, wie das neue Rezept angelegt werden soll:"; Z. 25 „Neues Rezept anlegen"; Z. 91 `aria-label="Rezeptauswahl"`; Z. 92/93 „Alle auswählen"/„Alle abwählen"; Z. 100 „Zielkochbuch wählen"; Z. 137 „Kochbuch auswählen"

## `Rezepte.Web/Components/Shared/ImageCropper.razor` (3)
Z. 22 „Lade Vorschau…"; Z. 30 „Speichere Bild…"; Z. 37 „Zuschneiden &amp; Speichern"

## `Rezepte.Web/Components/Shared/LatestRecipes.razor` (4)
Z. 9 und Z. 26 „📥 Zuletzt hinzugefügte Rezepte"; Z. 22 „Noch keine Rezepte vorhanden."; Z. 33 `alt="Bild zu @recipe.Title"`

## `Rezepte.Web/Components/Shared/MultiAssignToCookbooksOverlay.razor` (3)
Z. 13 „Zuweisung zu Kochbüchern (Mehrfachauswahl)"; Z. 14 „Wähle die Kochbücher..."; Z. 25 „Keine Kochbücher vorhanden."

## `Rezepte.Web/Components/Shared/PhotoOverlay.razor` (5)
Z. 10 „Lade Bilder…"; Z. 14 „Keine Bilder vorhanden."; Z. 24 `alt="Foto"`; Z. 29 `title="Bild löschen"`; Z. 49 `alt="Großansicht"`

## `Rezepte.Web/Components/Shared/RandomFromCookbooks.razor` (3)
Z. 11 „🎲 Zufallsrezepte aus Ihren Kochbüchern"; Z. 51 „Kein Rezept in diesem Kochbuch."; Z. 58 `alt="Bild zu @it.Recipe.Title"`

## `Rezepte.Web/Components/Shared/RecipeSelectDialog.razor` (7)
Z. 10 „Vorhandene Rezepte hinzufügen"; Z. 11 `aria-label="Close"`; Z. 15 `placeholder="Suchen…"`; Z. 23 „Rezepte konnten nicht geladen werden."; Z. 27 „Keine weiteren Rezepte verfügbar."; Z. 48 „Mehr laden…"; Z. 53 „Lade weitere…"
