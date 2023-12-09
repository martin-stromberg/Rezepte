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
            return record;
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
            return existing;
        }

    }
}
