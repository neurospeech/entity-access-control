using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace NeuroSpeech.EntityAccessControl
{
    internal class EntityJsonTypeInfo
    {
        public readonly string? Name;

        public readonly List<EntityJsonPropertyInfo> Properties;

        public EntityJsonTypeInfo(EntitySerializationSettings settings, Type type, JsonNamingPolicy? policy)
        {
            this.Name = settings.GetTypeName?.Invoke(type) ?? type.FullName;
            var namingPolicy = policy ?? JsonNamingPolicy.CamelCase;
            Properties = type.GetProperties()
                    .Where(p =>
                        !(p.GetIndexParameters()?.Length > 0))
                    .Select(p =>
                    {
                        var propertyType = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;
                        return new EntityJsonPropertyInfo(
                            PropertyType: propertyType,
                            Name: namingPolicy.ConvertName(p.Name),
                            PropertyInfo: p,
                            TypeCode: Type.GetTypeCode(propertyType)
                        );
                    })
                    .ToList();
        }
    }

    internal class EntityJsonPropertyInfo
    {
        public readonly Type PropertyType;
        public readonly string Name;
        public readonly PropertyInfo PropertyInfo;
        public readonly TypeCode TypeCode;

        public EntityJsonPropertyInfo(
            Type PropertyType,
            string Name,
            PropertyInfo PropertyInfo,
            TypeCode TypeCode)
        {
            this.PropertyType = PropertyType;
            this.Name = Name;
            this.PropertyInfo = PropertyInfo;
            this.TypeCode = TypeCode;
        }
    }
}