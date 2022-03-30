using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using NeuroSpeech.EntityAccessControl.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace NeuroSpeech.EntityAccessControl
{

    public class EntitySerializationSettings
    {
        public Func<Type, string> GetTypeName;

        public Func<PropertyInfo, JsonIgnoreCondition> GetIgnoreCondition = GetDefaultIgnoreAttribute;

        private static JsonIgnoreCondition GetDefaultIgnoreAttribute(PropertyInfo property)
        {
            return property.GetCustomAttribute<JsonIgnoreAttribute>()?.Condition
                ?? System.Text.Json.Serialization.JsonIgnoreCondition.Never;
        }

        public JsonSerializerOptions Options => new JsonSerializerOptions { 
            Converters =
            {
                new EntityJsonConverterFactory(this)
            }
        };

        public EntitySerializationSettings()
        {
            GetTypeName = (x) => (x.IsAnonymous() ? x.Name : x.FullName)!;
        }

        public EntitySerializationSettings(ISecureQueryProvider db)
        {
            GetTypeName = (x) => db.Model.FindEntityType(x)?.Name ?? (x.IsAnonymous() ? x.Name : x.FullName!);
            GetIgnoreCondition = db.GetIgnoreCondition;
        }


        public EntitySerializationSettings(DbContext db)
        {
            GetTypeName = (x) => db.Model.FindEntityType(x)?.Name ?? (x.IsAnonymous() ? x.Name : x.FullName!);
        }
    }

    public class ReferenceEqualityComparer : IEqualityComparer<object>
    {
        public static ReferenceEqualityComparer Instance = new();

        public new bool Equals([AllowNull] object x, [AllowNull] object y)
        {
            return x == y;
        }

        public int GetHashCode([DisallowNull] object obj)
        {
            return RuntimeHelpers.GetHashCode(obj);
        }
    }

}
