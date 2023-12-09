using Rezepte.Services.Database.Models;
using SQLite;
using System;
using System.Linq;

namespace Rezepte.Services.Database
{
    public interface ICockingDatabase
    {

        T Add<T>(T record) where T: BaseDataModel, new();

        T Update<T>(T record) where T: BaseDataModel, new();

        T AddOrUpdate<T>(T record) where T: BaseDataModel, new();

        IEnumerable<T> GetAll<T>() where T: BaseDataModel, new();

        T Get<T>(long id) where T: BaseDataModel, new();

    }

    public class CockingDatabase: ICockingDatabase, IDisposable
    {

        private SQLiteConnection connection;
        private CockingDatabaseSettings settings;

        public CockingDatabase(CockingDatabaseSettings settings)
        {
            this.settings = settings;
        }

        protected SQLiteConnection Connection
        {
            get
            {
                if (connection == null)
                {
                    connection = new SQLiteConnection(settings.FilePath, settings.OpenFlags);
                    InitOrUpgrade();
                }
                return connection;
            }
        }

        private bool initialized = false;
        private bool initializing = false;
        private bool disposedValue;

        private void InitOrUpgrade()
        {
            if (initialized || initializing)
                return;
            initializing = true;
            try
            {
                Connection.CreateTable<Receipt>();
                initialized = true;
            }
            finally
            {
                initializing = false;
            }
        }

        public void Open()
        {
            InitOrUpgrade();
        }

        public void Close()
        {
            if (connection != null)
            {
                connection.Dispose();
                connection = null;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: Verwalteten Zustand (verwaltete Objekte) bereinigen
                    Close();
                }

                // TODO: Nicht verwaltete Ressourcen (nicht verwaltete Objekte) freigeben und Finalizer überschreiben
                // TODO: Große Felder auf NULL setzen
                connection = null;
                settings = null;
                disposedValue = true;
            }
        }

        // TODO: Finalizer nur überschreiben, wenn "Dispose(bool disposing)" Code für die Freigabe nicht verwalteter Ressourcen enthält
        ~CockingDatabase()
        {
            // Ändern Sie diesen Code nicht. Fügen Sie Bereinigungscode in der Methode "Dispose(bool disposing)" ein.
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Ändern Sie diesen Code nicht. Fügen Sie Bereinigungscode in der Methode "Dispose(bool disposing)" ein.
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public T Add<T>(T record) where T: BaseDataModel, new()
        {
            if (record.Id != 0)
                throw new ArgumentException($"record with defined primary key cannot be added.");
            Connection.Insert(record);
            return record;
        }

        public T Update<T>(T record) where T: BaseDataModel, new()
        {
            if (record.Id == 0)
                throw new ArgumentException($"record with undefined primary key cannot be updated.");
            Connection.Update(record);
            return record;
        }

        public T AddOrUpdate<T>(T record) where T: BaseDataModel, new()
        {
            var existing = Connection.Table<T>().FirstOrDefault(rec => (rec as BaseDataModel).Id == record.Id) as BaseDataModel;
            if (existing == null)
                return Add(record);
            else
            {
                existing.Update(record);
                return Update((T)existing);
            }
        }

        public IEnumerable<T> GetAll<T>() where T: BaseDataModel, new()
        {
            return Connection.Table<T>();
        }

        public T Get<T>(long id) where T: BaseDataModel, new()
        {
            return Connection.Table<T>().FirstOrDefault(rec => rec.Id == id);
        }

    }
}
