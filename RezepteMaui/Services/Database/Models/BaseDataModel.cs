using SQLite;
using System;
using System.Linq;

namespace Rezepte.Services.Database.Models
{
    public class BaseDataModel
    {

        private long _Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdate { get; set; }

        [PrimaryKey]
        [AutoIncrement]
        public long Id
        {
            get
            {
                return _Id;
            }
            set
            {
                var _oldValue = _Id;
                _Id = value;
                OnRename(_oldValue, value);
            }
        }

        protected virtual void OnRename(long oldId, long newId) { }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return this == null;
            if (obj.GetType() != GetType())
                return false;
            foreach (var prop in GetType().GetProperties())
            {
                var ignore = prop.GetCustomAttributes(typeof(IgnoreAttribute), true).Any();
                var isDataModel = prop.PropertyType.IsAssignableTo(typeof(BaseDataModel));
                var isDataModelArray = prop.PropertyType.IsArray
                    && prop.PropertyType.GetElementType().IsAssignableTo(typeof(BaseDataModel));
                if (ignore && !isDataModel && !isDataModelArray)
                    continue;

                var ownValue = prop.GetValue(this);
                var compareValue = prop.GetValue(obj);
                if ((ownValue == null) && (compareValue == null))
                    continue;
                if ((ownValue != null) && (compareValue == null))
                    return false;
                if ((ownValue == null) && (compareValue != null))
                    return false;

                if (prop.PropertyType.IsArray)
                {
                    var ownArray = ownValue as Array;
                    var compareArray = compareValue as Array;
                    if (ownArray.Length != compareArray.Length)
                        return false;
                    for (int idx = 0; idx < ownArray.Length; idx++)
                    {
                        ownValue = ownArray.GetValue(idx);
                        compareValue = compareArray.GetValue(idx);
                        if ((ownValue == null) && (compareValue == null))
                            continue;
                        if ((ownValue != null) && (compareValue == null))
                            return false;
                        if ((ownValue == null) && (compareValue != null))
                            return false;
                        if (!ownValue.Equals(compareValue))
                            return false;
                    }
                    continue;
                }
                if (!ownValue.Equals(compareValue))
                    return false;
            }
            return true;
        }

        public override int GetHashCode()
        {
            unchecked // Allow arithmetic overflow, numbers will just "wrap around"
            {
                int hashcode = 1430287;
                foreach (var prop in GetType()
                                     .GetProperties()
                                     .Where(p => p.CanRead)
                                     .Where(p => p.GetCustomAttributes(typeof(AffectsEqualAttribute), true).Any()))
                {
                    var value = prop.GetValue(this);
                    hashcode = hashcode * 7302013 ^ value.GetHashCode();
                }
                return hashcode;
            }
        }

        public void Update(BaseDataModel source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (source.GetType() != GetType())
                throw new ArgumentException(nameof(source));
            foreach (var prop in GetType()
                                 .GetProperties()
                                 .Where(p => !p.GetCustomAttributes(typeof(AffectsEqualAttribute), true).Any())
                                 .Where(p => !p.GetCustomAttributes(typeof(PrimaryKeyAttribute), true).Any())
                                 .Where(p => p.CanRead && p.CanWrite))
            {
                var sourceValue = prop.GetValue(source);
                prop.SetValue(this, sourceValue);
            }
        }

    }
}
