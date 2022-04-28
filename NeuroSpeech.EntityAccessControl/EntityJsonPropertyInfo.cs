using NeuroSpeech.EntityAccessControl.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NeuroSpeech.EntityAccessControl
{
    internal class EntityJsonTypeInfo
    {
        public readonly string? Name;

        public readonly List<EntityJsonPropertyInfo> Properties;

        public EntityJsonTypeInfo(EntitySerializationSettings settings, Type type, JsonNamingPolicy? policy)
        {
            this.Name = settings.GetTypeName(type);
            var namingPolicy = policy ?? JsonNamingPolicy.CamelCase;
            var ignoreProperties = settings.GetIgnoredProperties(type);
            var properties = type.GetProperties();
            Properties = new List<EntityJsonPropertyInfo>(properties.Length);
            foreach(var p in properties)
            {
                if (!p.CanRead) continue;
                if (p.GetIndexParameters().Length > 0) continue;
                var propertyType = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;
                Properties.Add(new EntityJsonPropertyInfo(
                    PropertyType: propertyType,
                    Name: namingPolicy.ConvertName(p.Name),
                    PropertyInfo: p,
                    TypeCode: Type.GetTypeCode(propertyType),
                    jsonIgnoreCondition: ignoreProperties
                        .Contains(p) ? JsonIgnoreCondition.Always : JsonIgnoreCondition.Never));
            }
        }
    }

    internal class EntityJsonPropertyInfo
    {
        public readonly Type PropertyType;
        public readonly string Name;
        public readonly PropertyInfo PropertyInfo;
        public readonly TypeCode TypeCode;
        public readonly JsonIgnoreCondition IgnoreCondition;

        public EntityJsonPropertyInfo(
            Type PropertyType,
            string Name,
            PropertyInfo PropertyInfo,
            TypeCode TypeCode,
            JsonIgnoreCondition jsonIgnoreCondition)
        {
            this.PropertyType = PropertyType;
            this.Name = Name;
            this.IgnoreCondition = jsonIgnoreCondition;
            this.PropertyInfo = PropertyInfo;
            this.TypeCode = TypeCode;
        }
    }
}