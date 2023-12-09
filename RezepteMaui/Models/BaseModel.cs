using System;
using System.Linq;

namespace Rezepte.Models
{
    public class BaseModel
    {

        public override bool Equals(object obj)
        {
            if (obj == null)
                return this == null;
            if (obj.GetType() != GetType())
                return false;
            foreach (var prop in GetType().GetProperties().Where(p => p.CanRead && p.CanWrite))
            {
                var ownValue = prop.GetValue(this);
                var compareValue = prop.GetValue(obj);
                if ((ownValue != null) && (compareValue == null))
                    return false;
                if ((ownValue == null) && (compareValue != null))
                    return false;
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
                foreach (var prop in GetType().GetProperties().Where(p => p.CanRead))
                {
                    var value = prop.GetValue(this);
                    hashcode = hashcode * 7302013 ^ value.GetHashCode();
                }
                return hashcode;
            }
        }

    }
}
