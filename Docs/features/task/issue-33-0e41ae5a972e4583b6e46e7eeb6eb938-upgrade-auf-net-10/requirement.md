# Anforderung: Upgrade auf .NET 10

## Zusammenfassung
Das gesamte Projekt soll auf .NET 10 aktualisiert werden. Anschließend sollen alle NuGet-Pakete auf den neuesten kompatiblen Stand gebracht werden. Ziel ist ein vollständig build- und testfähiger Projektstand auf Basis von .NET 10 mit aktualisierten Abhängigkeiten.

## Auslöser und Akteure
- **Auslöser:** Technisches Upgrade der Projektplattform und der Paketabhängigkeiten.
- **Akteure:** Entwicklerinnen und Entwickler des Projekts, Build-/CI-Systeme sowie alle Anwendungen, Bibliotheken und Tests innerhalb des Repositorys.

## Beschreibung
Alle relevanten Projektdateien, Build-Konfigurationen, Testprojekte, Dokumentationsverweise und Hilfsdateien im Repository sollen geprüft und, sofern erforderlich, auf .NET 10 umgestellt werden. Dazu gehören insbesondere Target Frameworks, SDK-/Runtime-Angaben, globale Konfigurationsdateien, CI- oder Build-Skripte sowie Docker- oder Deployment-Artefakte, falls sie im Projekt vorhanden sind.

Nach der Umstellung auf .NET 10 sollen alle NuGet-Pakete auf die jeweils neuesten Versionen aktualisiert werden, soweit diese mit .NET 10 und dem bestehenden Projekt kompatibel sind. Erforderliche Anpassungen an Code, Konfiguration oder Tests sind vorzunehmen, wenn sie durch geänderte APIs, Analyzer, Compilerwarnungen oder Paketverhalten notwendig werden.

Das Ergebnis soll ein konsistenter Projektstand sein, bei dem alle Projektbestandteile auf .NET 10 ausgerichtet sind und keine veralteten Paketversionen verbleiben, sofern deren Aktualisierung technisch möglich und sinnvoll ist.

## Eingaben und Ausgaben
- **Eingaben:** Bestehender Repository-Stand, vorhandene Projektdateien, Paketreferenzen, Build- und Testkonfigurationen, CI-/Deployment-Konfigurationen.
- **Ausgaben/Ergebnisse:** Aktualisierte Projekt-, Konfigurations- und Paketdateien; gegebenenfalls angepasster Quellcode und Tests; dokumentierte Testergebnisse.

## Fehlerbehandlung
Falls einzelne NuGet-Pakete keine mit .NET 10 kompatible neuere Version anbieten oder ein Paket-Upgrade inkompatible Änderungen verursacht, soll dies im Plan oder in den Folgeartefakten dokumentiert werden. Falls ein Paket ersetzt oder auf einer älteren Version belassen werden muss, ist die technische Begründung festzuhalten. Build- oder Testfehler, die durch das Upgrade entstehen, sind im Rahmen der Umsetzung zu beheben oder als offene Folgeaufgabe zu dokumentieren, wenn sie nicht automatisch abschließend lösbar sind.

## Abgrenzung
Nicht Teil der Anforderung sind fachliche Funktionsänderungen, neue Features oder größere Architekturumbauten, sofern sie nicht zwingend erforderlich sind, um das Projekt auf .NET 10 und aktuelle Paketversionen zu bringen. Ebenfalls nicht Teil der Anforderung ist eine bewusste Änderung von Laufzeitverhalten, Benutzeroberflächen oder öffentlichen Schnittstellen über notwendige Kompatibilitätsanpassungen hinaus.

## Akzeptanzkriterien
- [ ] Alle relevanten Projekte im Repository verwenden .NET 10 als Zielframework oder sind nachvollziehbar davon ausgenommen.
- [ ] Build-, Test-, CI-, Docker- und sonstige Konfigurationsdateien sind auf .NET 10 geprüft und bei Bedarf aktualisiert.
- [ ] Alle NuGet-Pakete sind auf die neuesten kompatiblen Versionen aktualisiert oder begründet dokumentiert, falls ein Update nicht möglich ist.
- [ ] Der Quellcode kompiliert nach dem Upgrade ohne upgradebedingte Fehler.
- [ ] Vorhandene automatisierte Tests wurden ausgeführt oder nicht ausführbare Tests wurden mit Begründung dokumentiert.
- [ ] Notwendige Code- oder Konfigurationsanpassungen aufgrund geänderter APIs, Analyzer oder Paketverhalten sind umgesetzt.
- [ ] Es bleiben keine offensichtlichen Verweise auf ältere .NET-Zielversionen zurück, sofern diese nicht begründet erforderlich sind.

## Offene Punkte
- Es ist noch zu prüfen, welche konkreten Projektbestandteile, Build-Artefakte und NuGet-Pakete im Repository vorhanden sind.
- Es ist noch zu prüfen, ob alle verwendeten Pakete bereits .NET 10-kompatible Versionen bereitstellen.
