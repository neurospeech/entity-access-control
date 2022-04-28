using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using NeuroSpeech.EntityAccessControl.Internal;
using System;
using System.Collections.Concurrent;
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
        public string TypeCacheKey;

        public Func<Type, string> GetTypeName;

        public Func<Type, List<PropertyInfo>> GetIgnoredProperties = GetDefaultIgnoreAttribute;

        private readonly Dictionary<object, int> added = new(ReferenceEqualityComparer.Instance);

        private readonly ConcurrentDictionary<(string key,Type type), EntityJsonTypeInfo> typeCache
            = new ();


        public bool TryGetReferenceIdOrAdd(object key, out int id)
        {
            if (added.TryGetValue(key, out id))
                return true;
            id = added.Count;
            added[key] = id;
            return false;
        }

        private static List<PropertyInfo> GetDefaultIgnoreAttribute(Type type)
        {
            return type.GetProperties()
                .Select(x => (property: x, condition: x.GetCustomAttribute<JsonIgnoreAttribute>()))
                .Where(x => x.condition?.Condition == JsonIgnoreCondition.Always)
                .Select(x => x.property)
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
            TypeCacheKey = "Global";
        }

        public EntitySerializationSettings(ISecureQueryProvider db)
        {
            GetTypeName = (x) => db.Model.FindEntityType(x)?.Name ?? (x.IsAnonymous() ? x.Name : x.FullName!);
            GetIgnoredProperties = db.GetIgnoredProperties;
            TypeCacheKey = db.TypeCacheKey;
        }


        public EntitySerializationSettings(DbContext db)
        {
            GetTypeName = (x) => db.Model.FindEntityType(x)?.Name ?? (x.IsAnonymous() ? x.Name : x.FullName!);
            TypeCacheKey = "Global";
        }

        internal EntityJsonTypeInfo GetTypeInfo(Type et, JsonNamingPolicy? policy)
        {
            var key = (TypeCacheKey, type: et);
            return typeCache.GetOrCreate(key, (x) => new EntityJsonTypeInfo(this, x.type, policy));
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
