# 📋 Anforderungen: Kochrezepte-Verwaltungssystem (ASP.NET Core Blazor Server)

Diese Tabelle dokumentiert alle funktionalen Anforderungen für die Anwendung zur Verwaltung von Kochrezepten. Die Implementierung erfolgt mit SQLite als Datenbank und Blazor Server als Webframework.

| Kennung       | Status     | Anforderung                                                                  | Implementierung im Code                                                                 |
|---------------|------------|------------------------------------------------------------------------------|------------------------------------------------------------------------------------------|
| AUTH-001      | 🕓 Offen   | Registrierung nur möglich, wenn keine Benutzer existieren                    | Initialer Check in der Datenbank beim Registrierungsversuch                             |
| AUTH-002      | 🕓 Offen   | Erster Benutzer wird Administrator                                           | Rollenzuweisung beim ersten erfolgreichen Benutzer-Insert                               |
| AUTH-003      | 🕓 Offen   | Administrator kann weitere Benutzer anlegen                                  | Admin-Panel mit Benutzerverwaltung                                                      |
| AUTH-004      | 🕓 Offen   | Login/Logout für Benutzer                                                    | ASP.NET Identity oder eigene Authentifizierung                                          |
| DB-001        | 🕓 Offen   | Verwendung von SQLite als Datenbank                                          | EF Core mit SQLite Provider                                                             |
| BOOK-001      | 🕓 Offen   | Benutzer kann beliebig viele Kochbücher erstellen                            | Kochbuch-Entity mit Benutzerreferenz                                                    |
| BOOK-002      | 🕓 Offen   | Rezepte können mehreren Kochbüchern zugeordnet werden                        | Many-to-Many Beziehung zwischen Rezept und Kochbuch                                     |
| RECIPE-001    | 🕓 Offen   | Rezept hat Titel                                                             | Property `Title` in der Rezept-Entity                                                   |
| RECIPE-002    | 🕓 Offen   | Rezept hat beliebig viele Bilder                                             | Bilder als separate Entity mit Foreign Key zum Rezept                                   |
| RECIPE-003    | 🕓 Offen   | Rezept hat beliebig viele Zubereitungsschritte                               | Schritte als Collection in der Rezept-Entity                                            |
| STEP-001      | 🕓 Offen   | Schritt hat optionalen Titel                                                 | Property `Title` (nullable) in Schritt-Entity                                           |
| STEP-002      | 🕓 Offen   | Schritt hat Beschreibung                                                     | Property `Description` in Schritt-Entity                                                |
| STEP-003      | 🕓 Offen   | Schritt hat Zutatenliste                                                     | Zutaten als Collection in Schritt-Entity                                                |
| STEP-004      | 🕓 Offen   | Schritt hat Zubereitungsdauer                                                | Property `DurationMinutes` in Schritt-Entity                                            |
| STEP-005      | 🕓 Offen   | Schritt kann Ruhezeit über Nacht enthalten                                   | Boolean-Flag `RequiresOvernightRest` in Schritt-Entity                                  |
| CAL-001       | 🕓 Offen   | Jeder Benutzer hat einen Kalender                                            | Kalender-View mit Benutzerbindung                                                       |
| CAL-002       | 🕓 Offen   | Rezepte können im Kalender eingeplant werden                                 | Rezept-Zuordnung zu Datum mit Vorbereitungslogik                                        |
| CAL-003       | 🕓 Offen   | Vorbereitungen an Vortagen werden automatisch erkannt                         | Algorithmus zur Rückrechnung basierend auf Dauer und Ruhezeit                          |
| PLAN-001      | 🕓 Offen   | Arbeitsplan kombiniert mehrere Rezepte                                       | Arbeitsplan-Entity mit Rezeptreferenzen                                                 |
| PLAN-002      | 🕓 Offen   | Schritte werden zeitlich optimiert (z. B. Dessert vor Hauptgericht)          | Sortierlogik nach Zubereitungszeit und Rezepttyp                                        |
| SHOP-001      | 🕓 Offen   | Zutaten aus Arbeitsplan können in Einkaufsliste übernommen werden            | Zutatenextraktion aus Arbeitsplan                                                       |
| SHOP-002      | 🕓 Offen   | Zutaten können als erledigt abgehakt werden                                  | Checkbox-Status pro Zutat in der Einkaufsliste                                          |
| KI-001        | 📌 Geplant | Rezepterfassung per KI aus Fotos/Webseiten (zukünftig)                       | Platzhalter für KI-Modul, z. B. ML.NET oder Azure Cognitive Services                    |


## 📌 Statuslegende für Anforderungen

| Symbol            | Statusbezeichnung       | Bedeutung                                                                 |
| ----------------- |-------------------------|---------------------------------------------------------------------------|
| 🕓 Offen          | Offen                  | Die Anforderung wurde noch nicht begonnen                                 |
| 🚧 In Arbeit      | In Arbeit              | Die Umsetzung der Anforderung ist im Gange                                |
| ✅ Erledigt       | Erledigt               | Die Anforderung wurde vollständig umgesetzt und getestet                  |
| 🔍 Review         | Review                 | Die Umsetzung wird aktuell überprüft oder getestet                        |
| 🛠️ Überarbeiten   | Überarbeiten           | Die Umsetzung muss überarbeitet oder korrigiert werden                    |
| ⏸️ Zurückgestellt | Zurückgestellt         | Die Umsetzung wurde pausiert oder ist aktuell nicht priorisiert           |
| ❌ Verworfen      | Verworfen              | Die Anforderung wurde gestrichen und wird nicht umgesetzt                 |
| 📌 Geplant        | Geplant                | Die Anforderung ist für eine zukünftige Version vorgesehen                |
| ⚠️ Blockiert      | Blockiert              | Die Umsetzung ist aktuell nicht möglich (z. B. technische Abhängigkeiten) |
