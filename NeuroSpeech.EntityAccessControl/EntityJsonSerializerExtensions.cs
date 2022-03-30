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

    public class JsonIgnoreProperty
    {
        public readonly PropertyInfo Property;
        public readonly JsonIgnoreCondition Condition;

        public JsonIgnoreProperty(PropertyInfo property, JsonIgnoreCondition condition)
        {
            this.Property = property;
            this.Condition = condition;
        }
    }

    public class EntitySerializationSettings
    {
        public Func<Type, string> GetTypeName;

        public Func<Type, List<JsonIgnoreProperty>> GetIgnoreConditions = GetDefaultIgnoreAttribute;

        private readonly Dictionary<object, int> added = new(ReferenceEqualityComparer.Instance);

        private readonly Dictionary<Type, EntityJsonTypeInfo> typeCache
            = new Dictionary<Type, EntityJsonTypeInfo>();


        public bool TryGetReferenceIdOrAdd(object key, out int id)
        {
            if (added.TryGetValue(key, out id))
                return true;
            id = added.Count;
            added[key] = id;
            return false;
        }

        private static List<JsonIgnoreProperty> GetDefaultIgnoreAttribute(Type type)
        {
            return type.GetProperties()
                .Select(x => (x, x.GetCustomAttribute<JsonIgnoreAttribute>()))
                .Where(x => x.Item2 != null)
                .Select(x => new JsonIgnoreProperty(x.x, x.Item2!.Condition))
                .ToList();
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
            GetIgnoreConditions = db.GetIgnoreConditions;
        }


        public EntitySerializationSettings(DbContext db)
        {
            GetTypeName = (x) => db.Model.FindEntityType(x)?.Name ?? (x.IsAnonymous() ? x.Name : x.FullName!);
        }

        internal EntityJsonTypeInfo GetTypeInfo(Type et, JsonNamingPolicy? policy)
        {
            return typeCache.GetOrCreate(et, (x) => new EntityJsonTypeInfo(this, x, policy));
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
