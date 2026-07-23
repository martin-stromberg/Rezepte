# Tests, Dokumentation und offene Risiken

## Vorhandene Verifikation

`Rezepte.Tests` ist ein xUnit-Testprojekt und referenziert Abstractions,
produktive Plugins, Testfixture und Web. Die PR-Pipeline fuehrt die Tests aus.
Die vorhandene Dokumentation beschreibt Plugin-Erkennung, externe private
Plugins und Host-Integrationstests mit einem externen Pluginpfad.

Es gibt im Checkout keine Tests fuer:

- Contract-Pfad-Whitelist und Ausschluss von `bin/`/`obj/`;
- Manifest-Schema, SemVer und Commit-Zuordnung;
- SHA-256-Dateihashes und ZIP-Gesamthash;
- stabile ZIP-Reihenfolge und reproduzierbare Bytes;
- fehlende oder unerwartete Dateien als harte Exportfehler;
- isolierten Build des exportierten Workspace;
- Bau und Zuordnung der ApiCompat-Baselines;
- manuellen URL-/Hash-Import im Plugin-Repository.

## Sicherheits- und Betriebsrisiken

1. Der geforderte oeffentliche Download darf keine privaten Hostdateien oder
   Secrets enthalten. Die aktuelle Releaseausgabe hat keine Contract-Whitelist.
2. Ein gleicher `contractVersion`-Wert darf nicht auf ein anderes ZIP zeigen;
   dafuer fehlt heute der technische ZIP-Identifier.
3. Der `sourceCommit` muss aus dem unveraenderlichen Buildkontext stammen und
   darf nicht nur aus einer frei gesetzten Projektversion abgeleitet werden.
4. ApiCompat benoetigt Assemblies aus genau demselben Vertragsstand wie der
   Quellexport. Die aktuelle Buildstruktur liefert diese Baselines nicht.
5. Die in `Rezepte.Import.Plugins.AIFoto` und `AIUrl` vorhandenen
   `Rezepte.Web`-Referenzen koennen einen isolierten Export externer Plugins
   verhindern und muessen von der eigentlichen Contract-Surface getrennt
   werden.

## Nicht verifizierbare externe Grenze

Das private Plugin-Repository und sein `Rezepte.Import.PluginSdk` sind in
diesem Checkout nicht vorhanden. Aussagen ueber dessen konkrete Dateiliste,
Buildskripte oder bestehende ApiCompat-Konfiguration bleiben daher offene
Planungsannahmen und sollten vor der Implementierung mit dem Zielrepository
abgeglichen werden.

