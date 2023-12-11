using Rezepte.Extensions;
using Rezepte.Services.Database.Models;
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace Rezepte.Models
{
    public class BaseModel: ICloneable
    {

        public long Id { get; set; }

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

                if (prop.PropertyType.IsArray)
                {
                    var ownArray = ownValue as Array;
                    var compareArray = compareValue as Array;
                    if ((ownArray == null) && (compareArray == null))
                        return true;
                    if ((ownArray == null) && (compareArray != null))
                        return false;
                    if ((ownArray != null) && (compareArray == null))
                        return false;
                    if (ownArray.Length != compareArray.Length)
                        return false;
                    for (int idx = 0; idx < ownArray.Length; idx++)
                    {
                        ownValue = ownArray.GetValue(idx);
                        compareValue = compareArray.GetValue(idx);

                        if ((ownValue != null) && (compareValue == null))
                            return false;
                        if ((ownValue == null) && (compareValue != null))
                            return false;
                        if (!ownValue.Equals(compareValue))
                            return false;
                    }
                    return true;
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
                foreach (var prop in GetType().GetProperties().Where(p => p.CanRead))
                {
                    var value = prop.GetValue(this);
                    hashcode = hashcode * 7302013 ^ value.GetHashCode();
                }
                return hashcode;
            }
        }

        public virtual BaseDataModel ToDataModel()
        {
            var ownType = GetType();
            var dataModelType = (ownType.GetCustomAttribute(typeof(DataModelReferenceAttribute)) as DataModelReferenceAttribute).DataModelType;
            var dataModel = Activator.CreateInstance(dataModelType) as BaseDataModel;
            foreach (var prop in ownType.GetProperties()
                                        .Where(p => p.CanRead)
                                        .Where(p => !p.GetCustomAttributes().Any(a => a is IgnoreDataMemberAttribute)))
            {
                var refFieldName = string.Empty;
                var dataModelProp = dataModelType.GetProperty(prop.Name);
                if (dataModelProp == null)
                {
                    var fieldRefAttr = prop.GetCustomAttribute(typeof(FieldModelReferenceAttribute)) as FieldModelReferenceAttribute;
                    refFieldName = fieldRefAttr?.FieldName ?? string.Empty;
                    var sourceRefFieldName = fieldRefAttr?.ReferenceFieldName ?? string.Empty;
                    dataModelProp = dataModelType.GetProperty(sourceRefFieldName);
                }
                if (dataModelProp == null)
                    continue;
                if (!dataModelProp.CanWrite)
                    continue;
                var ownValue = prop.GetValue(this);
                if ((prop.PropertyType == typeof(StreamImageSource)) && (ownValue != null))
                {
                    StreamImageSource img = (StreamImageSource)ownValue;
                    using (var memoryStream = new MemoryStream())
                    {
                        img.Stream(CancellationToken.None).Wait<Stream>().CopyTo(memoryStream);
                        byte[] bytes = memoryStream.ToArray();
                        ownValue = Convert.ToBase64String(bytes);
                    }
                }
                if (prop.PropertyType.IsEnum)
                {
                    ownValue = (int)ownValue;
                }
                if (!string.IsNullOrWhiteSpace(refFieldName))
                {
                    var propField = prop.PropertyType.GetProperty(refFieldName);
                    if (ownValue is not null)
                    {
                        ownValue = propField.GetValue(ownValue);
                    }
                    else
                        ownValue = null;
                }
                dataModelProp.SetValue(dataModel, ownValue);
            }
            return dataModel;
        }

        protected virtual void Update(BaseModel record)
        {
            var ownType = GetType();
            var dataModelType = record.GetType();
            foreach (var prop in ownType.GetProperties()
                                        .Where(p => p.CanWrite)
                                        .Where(p => p.GetCustomAttribute(typeof(IgnoreDataMemberAttribute)) == null))
            {
                string refFieldName = string.Empty;
                var dataModelProp = dataModelType.GetProperty(prop.Name);
                if (dataModelProp == null)
                {
                    var fieldRefAttr = prop.GetCustomAttribute(typeof(FieldModelReferenceAttribute)) as FieldModelReferenceAttribute;
                    refFieldName = fieldRefAttr?.FieldName ?? string.Empty;
                    var sourceRefFieldName = fieldRefAttr?.ReferenceFieldName ?? string.Empty;
                    dataModelProp = dataModelType.GetProperty(sourceRefFieldName);
                }
                if (dataModelProp == null)
                    continue;
                if (!dataModelProp.CanRead)
                    continue;
                var sourceValue = dataModelProp.GetValue(record);
                if (sourceValue != null)
                {
                    if (prop.PropertyType.IsAssignableTo(typeof(BaseModel)))
                        sourceValue = ((BaseModel) sourceValue).Clone() as BaseModel;
                    else if (prop.PropertyType.IsArray
                        && prop.PropertyType.GetElementType().IsAssignableTo(typeof(BaseModel)))
                    {
                        var sourceArray = sourceValue as Array;
                        var newArray = Activator.CreateInstance(prop.PropertyType, sourceArray.Length) as Array;
                        for (int idx = 0; idx < sourceArray.Length; idx++)
                        {
                            sourceValue = sourceArray.GetValue(idx);
                            sourceValue = ((BaseModel)sourceValue).Clone();
                            newArray.SetValue(sourceValue, idx);
                        }
                        sourceValue = newArray;
                    }
                    if ((prop.PropertyType == typeof(StreamImageSource)) && (sourceValue != null))
                    {
                        byte[] bytes = Convert.FromBase64String((string)sourceValue);
                        MemoryStream stream = new MemoryStream(bytes);
                        sourceValue = StreamImageSource.FromStream(() => stream);
                    }
                    if (!string.IsNullOrWhiteSpace(refFieldName))
                    {
                        var propField = prop.PropertyType.GetProperty(refFieldName);
                        if (!sourceValue.Equals(Activator.CreateInstance(propField.PropertyType)))
                        {
                            var propFieldObj = Activator.CreateInstance(prop.PropertyType);
                            propField.SetValue(propFieldObj, sourceValue);
                            sourceValue = propFieldObj;
                        }
                        else
                            sourceValue = null;
                    }
                }
                prop.SetValue(this, sourceValue);
            }
        }

        protected virtual void Update(BaseDataModel record)
        {
            var ownType = GetType();
            var dataModelType = record.GetType();
            foreach (var prop in ownType.GetProperties()
                                        .Where(p => p.CanWrite)
                                        .Where(p => p.GetCustomAttribute(typeof(IgnoreDataMemberAttribute)) == null))
            {
                string refFieldName = string.Empty;
                var dataModelProp = dataModelType.GetProperty(prop.Name);
                if (dataModelProp == null)
                {
                    var fieldRefAttr = prop.GetCustomAttribute(typeof(FieldModelReferenceAttribute)) as FieldModelReferenceAttribute;
                    refFieldName = fieldRefAttr?.FieldName ?? string.Empty;
                    var sourceRefFieldName = fieldRefAttr?.ReferenceFieldName ?? string.Empty;
                    dataModelProp = dataModelType.GetProperty(sourceRefFieldName);
                }
                if (dataModelProp == null)
                    continue;
                if (!dataModelProp.CanRead)
                    continue;
                var sourceValue = dataModelProp.GetValue(record);
                if (sourceValue != null)
                {
                    if ((prop.PropertyType == typeof(StreamImageSource)) && (sourceValue != null))
                    {
                        byte[] bytes = Convert.FromBase64String((string)sourceValue);
                        MemoryStream stream = new MemoryStream(bytes);
                        sourceValue = StreamImageSource.FromStream(() => stream);
                    }
                    if (!string.IsNullOrWhiteSpace(refFieldName))
                    {
                        var propField = prop.PropertyType.GetProperty(refFieldName);
                        if (!sourceValue.Equals(Activator.CreateInstance(propField.PropertyType)))
                        {
                            var propFieldObj = Activator.CreateInstance(prop.PropertyType);
                            propField.SetValue(propFieldObj, sourceValue);
                            sourceValue = propFieldObj;
                        }
                        else
                            sourceValue = null;
                    }
                }
                prop.SetValue(this, sourceValue);
            }
        }

        public static BaseModel CreateFromDataModel(BaseDataModel record)
        {
            var dataModelType = record.GetType();
            var baseType = typeof(BaseModel);
            var modelType = baseType.Assembly
                                    .GetTypes()
                                    .Where(t => t.IsAssignableTo(baseType))
                                    .FirstOrDefault(t =>
                                    {
                                        var attr = t.GetCustomAttribute(typeof(DataModelReferenceAttribute)) as DataModelReferenceAttribute;
                                        return (attr != null) && (attr.DataModelType == dataModelType);
                                    });
            if (modelType == null)
                return null;
            var modelObject = Activator.CreateInstance(modelType) as BaseModel;
            modelObject.Update(record);
            return modelObject;
        }

        public object Clone()
        {
            var destObj = Activator.CreateInstance(GetType()) as BaseModel;
            destObj.Update(this);
            return destObj;
        }

    }
}
