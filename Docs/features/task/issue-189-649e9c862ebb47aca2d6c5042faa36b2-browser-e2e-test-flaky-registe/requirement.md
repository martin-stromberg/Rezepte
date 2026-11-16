# Anforderung

Issue #189: Browser-E2E-Test flaky: `Register_WithDemoData_CreatesDemoData` läuft in Demo-Daten-Timeout.

Der Test wartet 120 s auf das Seeding der Demo-Daten und findet dann `calendar=0`, obwohl der
Seed-Job-Status `Succeeded` meldet. Fehlschlag zuerst bei Issue #185 beobachtet, unabhängig davon
auch auf unverändertem `staging`.

Erwartet: Nach erfolgreichem Seed-Job sind die Demo-Daten (inkl. Kalendereinträge) sichtbar;
der Test läuft stabil grün.

Reproduktion: `dotnet test Rezepte.Tests.Browser -c Release --filter "FullyQualifiedName~RegisterTests"`
