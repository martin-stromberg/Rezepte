using Rezepte.Services.Database;
using Rezepte.Services.Database.Models;
using System;
using System.Linq;

namespace Rezepte.Tests.Helper
{
    internal class DummyDatabase: ICockingDatabase
    {

        private Dictionary<Type, List<object>> _tables = new Dictionary<Type, List<object>>();
        private Dictionary<Type, int> _primaryKeys = new Dictionary<Type, int>();

        private void InitTable(Type recordType)
        {
            if (_tables.ContainsKey(recordType))
                return;
            _primaryKeys.Add(recordType, 0);
            _tables.Add(recordType, new List<object>());
        }

        public T Add<T>(T record) where T: BaseDataModel, new()
        {
            Type recordType = typeof(T);
            if (recordType == typeof(BaseDataModel))
                recordType = record.GetType();
            InitTable(recordType);
            if (record.Id != 0)
                throw new ArgumentException("record.Id must be 0");
            _tables[recordType].Add(record);
            record.Id = ++_primaryKeys[recordType];
            SaveAssosiatedRecords(record);
            return record;
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

        public T AddOrUpdate<T>(T record) where T: BaseDataModel, new()
        {
            if (record.Id == 0)
                return Add<T>(record);
            else
                return Update<T>(record);
        }

        public T Get<T>(long id) where T: BaseDataModel, new()
        {
            InitTable(typeof(T));
            return GetAll<T>().FirstOrDefault(x => x.Id == id);
        }

        public IEnumerable<T> GetAll<T>() where T: BaseDataModel, new()
        {
            InitTable(typeof(T));
            return _tables[typeof(T)].Cast<T>();
        }

        public T Update<T>(T record) where T: BaseDataModel, new()
        {
            Type recordType = typeof(T);
            if (recordType == typeof(BaseDataModel))
                recordType = record.GetType();
            InitTable(recordType);
            if (record.Id == 0)
                throw new ArgumentException("record.Id must be 0");
            var existing = _tables[recordType].Cast<T>().First(rec => rec.Id == record.Id);
            existing.Update(record);
            SaveAssosiatedRecords(record);
            return existing;
        }

        public T Remove<T>(T record) where T : BaseDataModel, new()
        {
            throw new NotImplementedException();
        }
    }
}
