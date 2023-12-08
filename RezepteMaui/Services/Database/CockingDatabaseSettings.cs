using SQLite;
using System;
using System.Linq;

namespace Rezepte.Services.Database
{
    public class CockingDatabaseSettings
    {

        public string FilePath { get; set; }

        public SQLiteOpenFlags OpenFlags => SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache | SQLiteOpenFlags.FullMutex;

    }
}
