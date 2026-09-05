# Detail: no-notimplemented-check --all --strict (Exit 1)

13 Fundstellen in 5 Dateien — **alle in Testprojekten**, alle absichtliche Fehlersimulations-Fakes. Es wird **keine** `NotImplementedException` verwendet; der Check meldet throw-only-Member beliebigen Exception-Typs (Methoden-Body oder Expression-Body nur aus `throw`). Kein Suppressionsmechanismus im Check.

## `Rezepte.Tests.PluginFixture/TestImportPlugin.cs`
- Z. 31: `CheckUsabilityAsync(...) => throw new InvalidOperationException("Simulated usability check failure.")` — Expression-bodied Stub in `TestImportPlugin`.

## `Rezepte.Tests/Services/ApplicationUpdatePreInstallHandlerTests.cs`
- Z. 101: `FailingBackupService.CreateBackupAsync(...) => throw new InvalidOperationException("backup failed")`
- Z. 118: `FailingPreInstallHandler.RunPreInstallBackupAsync(...) => throw new InvalidOperationException("backup failed")`

## `Rezepte.Tests/Services/UpdateBackupServiceTests.cs`
- Z. 162: `RecordingExportService.ExportUserAsync(...) => throw new NotSupportedException()`
- Z. 174: `RecordingExportService.RestoreFromZipAsync(...) => throw new NotSupportedException()`
- Z. 180: `FailingExportService.ExportUserAsync(...) => throw new NotSupportedException()`
- Z. 183: `FailingExportService.ExportAllAsync(...) => throw new InvalidOperationException("export failed")`
- Z. 186: `FailingExportService.RestoreFromZipAsync(...) => throw new NotSupportedException()`

## `Rezepte.Tests/Services/Import/ImportOrchestratorTests.cs`
- Z. 389: `ThrowingHandler.HandleAsync(...) { throw new InvalidOperationException("handler failed"); }` — Block-Body nur aus throw

## `Rezepte.Tests/Services/Import/PluginUpdateServiceTests.cs`
- Z. 160: `FailingPackageInstaller.InstallAsync(...) => throw new PluginPackageInstallException(status, "reload failed", ...)`

## Feststellung

Die Fundstellen sind Test-Doubles, die gezielt Fehlerfälle simulieren — keine Produktiv-Stubs. Da der Check keine Ausnahmemarkierung unterstützt, müssen die Test-Helfer so umgeschrieben werden, dass der Member-Body nicht mehr nur aus einem `throw` besteht (z. B. Zähler/Return-Kombination), oder die Klassen anders strukturiert werden.
