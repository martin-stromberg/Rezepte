# Offene Aufgaben

Erstellt am: 2026-07-18
Abbruchgrund: Kein Fortschritt zwischen den letzten zwei Iterationen

Die folgenden Aufgaben konnten im automatisierten Zyklus nicht abgeschlossen werden
und muessen manuell oder in einem erneuten Lauf bearbeitet werden.

## Offene Planelemente

- [ ] Reload ist nicht als eigener, nachvollziehbarer Zustand persistiert: `PluginSourceRelease` braucht Reloadstatus, Reloadzeitpunkt und Reloadfehler, damit ein Fehler nach Dateiaustausch vom eigentlichen Kopier-/Installationsfehler unterscheidbar ist.
- [ ] Plugin-Lebenszyklus beim produktiven Reload bleibt unkoordiniert: laufende Import-Handler und produktive nicht sammelnde `AssemblyLoadContext`s werden beim Austausch noch nicht kontrolliert abgeloest oder gesperrt.
- [ ] GitHub-Rate-Limits und kontrollierte Wiederholung fehlen: `429`, Rate-Limit-`403`, `Retry-After`, Backoff und dedizierte Status-/Fehlerbehandlung sind nicht umgesetzt.
- [ ] Rollback ist nicht in allen Austauschfehlern robust: teilweise kopierte neue Pluginziele koennen liegen bleiben, wenn das Kopieren vor Aufnahme in die installierte Zielliste scheitert.
- [ ] Abnahmetests decken die offenen Vertraege nicht vollstaendig ab: insbesondere GitHub-Rate-Limits/Auth, Reloadstatus, produktive Reloadkoordination, Rollbackfehler, Hosted-Service-Ausfuehrung und Geheimnisausschluss.

## Code-Review-Befunde

- [ ] Hoch - Produktiver Reload ersetzt den Plugin-Lebenszyklus nicht kontrolliert: `PluginPackageInstaller` ruft nach Dateiaustausch direkt `PluginManager.InitializeAsync` auf, ohne laufende Import-Handler zu sperren oder alte produktive LoadContexts kontrolliert abzuloesen.
- [ ] Hoch - Reloadfehler werden nicht als eigener persistenter Zustand erfasst: Reload wird nur als generisches `InstallFailed` sichtbar, nicht mit eigenem Status, Zeitpunkt und Fehlertext.
- [ ] Mittel - Teilweise kopierte neue Pluginziele koennen nach einem Austauschfehler liegen bleiben: neue Ziele werden erst nach erfolgreichem Kopieren in die Rollbackliste aufgenommen.
- [ ] Mittel - GitHub-Rate-Limits und kontrollierte Wiederholung sind weiterhin nicht implementiert: `429`, Rate-Limit-`403`, `Retry-After`, Backoff und dedizierte Persistenz fehlen.

## Fehlgeschlagene Tests

- [ ] `Rezepte.Tests.Deployment.DeploymentDocumentationTests.FrameworkDependentLinuxPublish_ShouldProduceDocumentedEntrypointAndRuntimeFrameworks` - Bekannter bestehender Fehler beim Linux-Publish: externe AI-Pluginprojekte koennen die Referenz auf `Rezepte.Web` nicht aufloesen.
