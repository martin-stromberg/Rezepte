using Rezepte.Services.Database.Models;
using SQLite;
using System;
using System.Linq;
using System.Reflection;

namespace Rezepte.Services.Database
{
    public interface ICockingDatabase
    {
        T Remove<T>(T record) where T : BaseDataModel, new();
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
        private Type baseType = typeof(BaseDataModel);

        private void InitOrUpgrade()
        {
            if (initialized || initializing)
                return;
            initializing = true;
            try
            {
                Connection.CreateTable<Receipt>();
                Connection.CreateTable<ReceiptIngredient>();
                Connection.CreateTable<ReceiptPicture>();
                Connection.CreateTable<ReceiptCollection>();
                Connection.CreateTable<ReceiptCollectionEntry>();
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

        private void SaveAssosiatedRecords<T>(T record) where T: BaseDataModel, new()
        {
            foreach (var value in record.GetType()
                                        .GetProperties()
                                        .Where(prop => prop.PropertyType.IsAssignableTo(typeof(BaseDataModel)))
                                        .Select(prop => prop.GetValue(record) as BaseDataModel)
                                        .Where(value => value is not null))
            {
                AddOrUpdate<BaseDataModel>(value);
            }

            foreach (var value in record.GetType()
                                        .GetProperties()
                                        .Where(prop => prop.PropertyType.IsArray)
                                        .Select(prop => prop.GetValue(record) as Array)
                                        .Where(value => value is not null)
                                        .Where(value => value.Length > 0)
                                        .SelectMany(value => value.OfType<BaseDataModel>()))
            {
                AddOrUpdate<BaseDataModel>(value);
            }
        }

        public T Add<T>(T record) where T: BaseDataModel, new()
        {
            if (record.Id != 0)
                throw new ArgumentException($"record with defined primary key cannot be added.");
            Connection.Insert(record);
            SaveAssosiatedRecords(record);
            return record;
        }

        public T Update<T>(T record) where T: BaseDataModel, new()
        {
            if (record.Id == 0)
                throw new ArgumentException($"record with undefined primary key cannot be updated.");
            Connection.Update(record);
            SaveAssosiatedRecords(record);
            return record;
        }

        public T AddOrUpdate<T>(T record) where T: BaseDataModel, new()
        {
            var existing = (typeof(T) == baseType) ? Get(record.GetType(), record.Id) : Get<T>(record.Id);
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
            return CompleteElements(Connection.Table<T>());
        }

        protected IEnumerable<BaseDataModel> GetAll(Type modelType, BaseDataModel parentRecord = null)
        {
            var tableMapping = Connection.GetMapping(modelType);
            var query = $"select * from {tableMapping.TableName}";
            if (parentRecord != null)
            {
                var parentType = parentRecord.GetType();
                var foreignProp = modelType.GetProperties()
                                           .FirstOrDefault(prop =>
                                           {
                                               var attr = prop.GetCustomAttribute(typeof(ForeignKeyAttribute)) as ForeignKeyAttribute;
                                               if (attr == null)
                                                   return false;
                                               return attr.ParentType == parentType;
                                           });
                var foreignField = tableMapping.FindColumn(foreignProp.Name);
                query += $" where {foreignField.Name} = {parentRecord.Id}";
            }
            var recordSet = Connection.Query(tableMapping, query);
            return recordSet.Cast<BaseDataModel>();
        }

        private IEnumerable<T> CompleteElements<T>(TableQuery<T> ts) where T: BaseDataModel, new()
        {
            Type type = null;
            PropertyInfo[] properties = null;
            foreach (var record in ts)
            {
                var completedRecord = CompleteElement(record);
                
                yield return completedRecord;
            }
        }

        private T CompleteElement<T>(T record) where T : BaseDataModel, new()
        {
            if (record == null)
                return null;
            Type type = null; PropertyInfo[] properties = null;
            if (type == null)
            {
                type = record.GetType();
                properties = type.GetProperties()
                                 .Where(prop =>
                                        prop.PropertyType.IsAssignableTo(typeof(BaseDataModel))
                                     || (prop.PropertyType.IsArray
                                         && prop.PropertyType.GetElementType().IsAssignableTo(typeof(BaseDataModel))))
                                 .ToArray();
            }
            foreach (var prop in properties.Where(p => !p.PropertyType.IsArray))
            {
                var records = GetAll(prop.PropertyType, record);
            }
            foreach (var prop in properties.Where(p => p.PropertyType.IsArray))
            {
                var records = GetAll(prop.PropertyType.GetElementType(), record).ToArray();
                var destRecords = Activator.CreateInstance(prop.PropertyType, records.Length) as Array;
                for (int idx = 0; idx < records.Length; idx++)
                    destRecords.SetValue(records[idx], idx);
                prop.SetValue(record, destRecords);
            }
            return record;
        }

        public T Get<T>(long id) where T: BaseDataModel, new()
        {
            return CompleteElement<T>(Connection.Table<T>().FirstOrDefault(rec => rec.Id == id));
        }

        

        public BaseDataModel Get(Type modelType, long id)
        {
            var mapping = Connection.GetMapping(modelType);
            var record = Connection.Query(mapping, $"select * from {mapping.TableName} where id = {id};")
                                   .FirstOrDefault();
            return CompleteElement(record as BaseDataModel);
        }

        public T Remove<T>(T record) where T : BaseDataModel, new()
        {
            Connection.Delete(record);
            return null;
        }
    }
}
