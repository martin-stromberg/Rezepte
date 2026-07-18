# Offene Aufgaben

Erstellt am: 2026-07-18
Abbruchgrund: Laufzeitfehler nach abgeschlossenem Lifecycle

Die folgenden Aufgaben muessen korrigiert und verifiziert werden.

## Laufzeitfehler

- [x] Beim Pruefen einer Pluginquelle wirft EF/SQLite `SQLite Error 1: 'no such column: p.ReloadError'` in `Rezepte.Web.Services.Import.Plugins.PluginUpdateService.ProcessSourceAsync` bei `PluginUpdateService.cs:63`. Das Modell enthaelt `PluginSourceRelease.ReloadError`, aber die laufende SQLite-Datenbank hat die Spalte nicht. Pruefe insbesondere, ob die Migration `20260718131500_AddPluginSourceReleaseReloadState` korrekt von EF Core entdeckt und angewendet wird; falls noetig Designer-/Migration-Attribut oder eine neue Migration ergaenzen.

## Stacktrace

```text
Microsoft.Data.Sqlite.SqliteException
  HResult=0x80004005
  Nachricht = SQLite Error 1: 'no such column: p.ReloadError'.
  Quelle = Microsoft.Data.Sqlite
  Stapelueberwachung:
   bei Microsoft.Data.Sqlite.SqliteException.ThrowExceptionForRC(Int32 rc, sqlite3 db)
   bei Microsoft.Data.Sqlite.SqliteCommand.<PrepareAndEnumerateStatements>d__64.MoveNext()
   bei Microsoft.Data.Sqlite.SqliteCommand.<GetStatements>d__54.MoveNext()
   bei Microsoft.Data.Sqlite.SqliteDataReader.NextResult()
   bei Microsoft.Data.Sqlite.SqliteCommand.ExecuteReader(CommandBehavior behavior)
   bei Microsoft.Data.Sqlite.SqliteCommand.ExecuteReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
   bei Microsoft.Data.Sqlite.SqliteCommand.<ExecuteDbDataReaderAsync>d__60.MoveNext()
   bei Microsoft.EntityFrameworkCore.Storage.RelationalCommand.<ExecuteReaderAsync>d__22.MoveNext()
   bei Microsoft.EntityFrameworkCore.Query.Internal.SingleQueryingEnumerable`1.AsyncEnumerator.<InitializeReaderAsync>d__21.MoveNext()
   bei Microsoft.EntityFrameworkCore.Query.Internal.SingleQueryingEnumerable`1.AsyncEnumerator.<MoveNextAsync>d__20.MoveNext()
   bei Microsoft.EntityFrameworkCore.Query.ShapedQueryCompilingExpressionVisitor.<SingleOrDefaultAsync>d__16`1.MoveNext()
   bei Rezepte.Web.Services.Import.Plugins.PluginUpdateService.<ProcessSourceAsync>d__8.MoveNext()
       in D:\Repositories\softwareschmiede\539d7473-2d3f-474e-a423-7efeca5b203e\Rezepte.Web\Services\Import\Plugins\PluginUpdateService.cs: Zeile63
```
