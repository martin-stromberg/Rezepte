# Projektplan-Gegenprüfung

## Ergebnis

**Status:** Projektplan vollständig

## Abgleich Anforderung ↔ Entwicklungsschritte

| Anforderungspunkt | Abgedeckt durch Schritt(e) | Status |
|--------------------|-----------------------------|--------|
| Transparenz des Prüfprinzips: Hinweistext in der Pluginverwaltung (automatisch einmalig beim Anwendungsstart + manuell je Quelle) | 4 | Abgedeckt |
| Sichtbarkeit/Aktualisierung der Quellen-Statusfelder `LastCheckedAt`, `LastSuccessfulReleaseTag`, `LastError` (samt `LastErrorAt`) | 2, 4 | Abgedeckt |
| Neuer manueller Einzelquellen-Check (Quelle per Id laden, Vorbedingungen `Enabled` + `TrustConfirmed`, ggf. hinterlegtes PAT, bewährter Ablauf Release abrufen → Historie → Download → Paketprüfung → Installation → Status speichern) | 2 | Abgedeckt |
| Serialisierung gegen parallele Checks (manuell und Start-Lauf) | 2 | Abgedeckt |
| Admin-Berechtigung für manuellen Check (Delegation über Einstellungs-Service mit `EnsureAdmin`) | 2 | Abgedeckt |
| Ergebnis des manuellen Checks als direkte Rückmeldung an die UI („kein Update", „Release installiert", Fehler) — zusätzlich zu den Statusfeldern | 2, 4 | Abgedeckt |
| Skip-Verhalten für bereits installierte Releases (`Installed`) und Wiederholung fehlgeschlagener Releases | 2 | Abgedeckt |
| Neustartfreies Wirksamwerden: Installation in versionsbezogenen Pluginverzeichnissen (Namensschema aus Release-Tag, Umsetzungsentscheidung) statt In-place-Ersetzung | 1 | Abgedeckt |
| Austausch des Pluginbestands über den koordinierten Reload; Löschen der Altverzeichnisse beim Austausch | 1 | Abgedeckt |
| Nicht löschbare Altverzeichnisse: dauerhafte Löschmarkierung, Ausschluss aus der Plugin-Erkennung, Bereinigung beim nächsten Anwendungsstart bzw. nächsten koordinierten Austausch (inkl. Erweiterung des Start-Laufs) | 1 | Abgedeckt |
| Persistenz-Mechanismus der Löschmarkierung (Markerdatei/Pending-Delete; Umsetzungsentscheidung) | 1 | Abgedeckt |
| Plugins, die im neuen Release nicht mehr enthalten sind, erhalten den Status `Missing` | 1 | Abgedeckt |
| Bestehendes Fehlerverhalten (Rücksicherung bei fehlgeschlagener Installation/Austausch) bleibt erhalten und wird auf das neue Verzeichnismodell angepasst | 1 | Abgedeckt |
| Kein echtes Entladen erforderlich; alte Assembly bleibt im Prozess ohne neue Aufgaben (Entscheidung festgehalten) | 1 (sowie Grobe Vorgehensentscheidungen) | Abgedeckt |
| Mitgelieferte Plugins aus dem Publish sind von der Update-/Austauschbehandlung ausgenommen | 1 | Abgedeckt |
| Entfernen von `Missing`-Plugins: neue Löschmethode, nur Status `Missing`, Admin-Prüfung, nur Verwaltungseintrag (`PluginSetting`) | 3 | Abgedeckt |
| Wiederentdeckung behandelt Plugin wie neu (frischer `DiscoveredAt`, ans Ende der Reihenfolge); Hinweis Systemexport/Backup-Rückkehr | 3, 5 | Abgedeckt |
| UI: Aktion „Auf Updates prüfen" je Quelle, deaktiviert bei nicht erfüllten Vorbedingungen, gesperrt/Busy während eines Laufs | 4 | Abgedeckt |
| UI: Umstellung der Quellen-Aktionen (Bearbeiten, Löschen, Prüfen) auf Symbolbuttons mit Bootstrap-Icons, lokalisierten Tooltips und Accessible-Labels | 4 | Abgedeckt |
| UI: Entfernen-Symbolbutton für `Missing`-Plugins in der Pluginliste, nur bei Vermisst-Status | 5 | Abgedeckt |
| Lokalisierung aller neuen Texte (`UiStrings.resx`) | 4, 5 | Abgedeckt |
| Dokumentation: Prüfprinzip, versionsbezogene Verzeichnisse, Löschmarkierung und Entfernen-Aktion (`Docs/help/import-plugins.md`, `Docs/features.md`) | 6 | Abgedeckt |
| Scope-Ausschluss: keine automatische periodische Hintergrundprüfung | Grobe Vorgehensentscheidungen (Umfang) | Abgedeckt |
| Scope-Ausschluss: kein Downgrade-/Rollback-Angebot auf ältere Versionen über die UI | Grobe Vorgehensentscheidungen (Umfang) | Abgedeckt |
| Scope-Ausschluss: keine Änderung an Paketvalidierung, Vertragsprüfung, GitHub-Anbindung, PAT-Verwaltung | Grobe Vorgehensentscheidungen (Umfang) | Abgedeckt |
| Konfiguration: keine neue Konfiguration zwingend; optionales Prüfintervall nur bei geklärter periodischer Prüfung (entfällt durch Ausschluss) | Grobe Vorgehensentscheidungen (Umfang) | Abgedeckt |

## Abhängigkeitsprüfung

| Prüfpunkt | Befund |
|-----------|--------|
| Zyklen in Abhängigkeiten | Keine gefunden. Alle Abhängigkeiten verweisen auf kleinere Schrittnummern: Schritt 2 → 1, Schritt 4 → 2, Schritt 5 → 3, Schritt 6 → 1, 4, 5. Schritte 1 und 3 sind abhängigkeitsfrei. |
| Reihenfolge konsistent zu inhaltlichen Abhängigkeiten | Ja. Schritt 2 (manueller Check mit neustartfreier Installation) setzt inhaltlich Schritt 1 (versionsbezogene Verzeichnisse) voraus — als Abhängigkeit vermerkt. Schritt 4 baut auf dem Service aus Schritt 2 auf; Schritt 5 auf dem Service aus Schritt 3. Schritt 6 dokumentiert die Ergebnisse der Schritte 1, 4 und 5 (Schritte 2 und 3 sind darüber transitiv erreicht). Kein Schritt steht vor einem Schritt, von dem er inhaltlich abhängt. |

## Hinweise

- Die im Projektplan offen gelassenen Umsetzungsentscheidungen (Namensschema der versionsbezogenen Verzeichnisse, Form der Löschmarkierung, ggf. `PluginSourceSettingsItem`-Erweiterung für den Busy-Zustand) sind in der Anforderung ausdrücklich als Umsetzungsentscheidungen bzw. offene Fragen mit Annahmen markiert und müssen innerhalb der jeweiligen Lifecycle-Durchläufe festgelegt werden.
- Die in der Anforderung genannten Testbereiche (Einzelquellen-Check, Verzeichnismodell/Löschmarkierung, Discovery-Ausschluss, Entfernen von `Missing`-Plugins, UI-Tests) sind den Schritten 1–5 fachlich zugeordnet und im Rahmen der jeweiligen Abarbeitung zu prüfen.
