# Release Notes

## Important Notes Before Update

- There are no special notices.

## What's New

- Registration form offers a new "Create demo data" checkbox (`createDemoData` in `POST /api/auth/register`).
- When selected, a background job (`seed-demo-data`, `DemoDataSeedingJobHandler`) seeds the new account with 5 cookbooks containing 43 recipes in total.
- One dinner recipe is scheduled in the calendar for each of the next 5 days (6:00 PM).
- The ingredients of the recipe planned for the next day are added to the shopping list.

## Wichtige Hinweise vor dem Update

- Es gibt keine besonderen Hinweise.

## Neuerungen

- Registrierungsformular mit neuer Checkbox „Demo-Daten anlegen" (`createDemoData` in `POST /api/auth/register`).
- Bei Auswahl legt ein Hintergrundjob (`seed-demo-data`, `DemoDataSeedingJobHandler`) für das neue Konto 5 Kochbücher mit insgesamt 43 Rezepten an.
- Für die nächsten 5 Tage wird jeweils ein Abendessen-Rezept im Kalender eingetragen (18:00 Uhr).
- Die Zutaten des für den nächsten Tag geplanten Rezepts werden in die Einkaufsliste übernommen.
