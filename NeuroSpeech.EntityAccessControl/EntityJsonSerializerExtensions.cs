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

    //public class JsonIgnoreProperty
    //{
    //    public readonly PropertyInfo Property;
    //    public readonly JsonIgnoreCondition Condition;

    //    public JsonIgnoreProperty(PropertyInfo property, JsonIgnoreCondition condition)
    //    {
    //        this.Property = property;
    //        this.Condition = condition;
    //    }
    //}

    public class EntityJsonSerializer
    {
        public static string Serialize<T>(DbContext db, T item)
        {
            return JsonSerializer.Serialize(item, Options(db));
        }

        public static JsonSerializerOptions Options(DbContext db)
        {
            if (db is ISecureQueryProvider sp)
            {
                return new EntitySerializationSettings(sp).Options;
            }
            return new EntitySerializationSettings(db).Options;
        }
    }

    public class EntitySerializationSettings: JsonConverterFactory
    {
        public string TypeCacheKey;

        public Func<Type, string> GetTypeName;

        public Func<Type, List<PropertyInfo>> GetIgnoredProperties = GetDefaultIgnoreAttribute;

        public readonly JsonConverter Converter;

        public readonly JsonSerializerOptions Options;


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

        public EntitySerializationSettings()
        {
            GetTypeName = (x) => (x.IsAnonymous() ? x.Name : x.FullName)!;
            TypeCacheKey = "Global";
            Converter = new EntityJsonConverter(this);
            Options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                MaxDepth = 1024,
                Converters =
                {
                    this
                }
            };
        }

        public EntitySerializationSettings(ISecureQueryProvider db)
        {
            GetTypeName = (x) => db.Model.FindEntityType(x)?.Name ?? (x.IsAnonymous() ? x.Name : x.FullName!);
            GetIgnoredProperties = db.GetIgnoredProperties;
            TypeCacheKey = db.TypeCacheKey;
            Converter = new EntityJsonConverter(this);
            Options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                MaxDepth = 1024,
                Converters =
                {
                    this
                }
            };
        }


        public EntitySerializationSettings(DbContext db)
        {
            GetTypeName = (x) => db.Model.FindEntityType(x)?.Name ?? (x.IsAnonymous() ? x.Name : x.FullName!);
            TypeCacheKey = "Global";
            Converter = new EntityJsonConverter(this);
            Options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                MaxDepth = 1024,
                Converters =
                {
                    this
                }
            };
        }

        internal EntityJsonTypeInfo GetTypeInfo(Type et, JsonNamingPolicy? policy)
        {
            var key = (TypeCacheKey, type: et);
            return typeCache.GetOrCreate(key, (x) => new EntityJsonTypeInfo(this, x.type, policy));
        }

        public override bool CanConvert(Type typeToConvert)
        {
            if (typeToConvert.IsValueType)
                return false;
            if (typeToConvert == typeof(string))
                return false;
            if (typeof(System.Collections.IEnumerable).IsAssignableFrom(typeToConvert))
                return false;
            return true;
        }

        public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            return Converter;
        }

        class EntityJsonConverter : JsonConverter<object>
        {

            static readonly string DateFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'FFFFFFFZ";

            private EntitySerializationSettings settings;


            public EntityJsonConverter(EntitySerializationSettings settings)
            {
                this.settings = settings;
            }

            public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                throw new NotImplementedException();
            }

            public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
            {
                if (value == null)
                {
                    writer.WriteNullValue();
                    return;
                }

                if (settings.TryGetReferenceIdOrAdd(value, out var id))
                {
                    writer.WriteStartObject();
                    writer.WriteNumber("$ref", id);
                    writer.WriteEndObject();
                    return;
                }
                writer.WriteStartObject();

                // write id
                writer.WriteNumber("$id", id);



                var et = value.GetType();

                var typeInfo = settings.GetTypeInfo(et, options.PropertyNamingPolicy);

                writer.WriteString("$type", typeInfo.Name);


                foreach (var p in typeInfo.Properties)
                {
                    var ignoreCondition = p.IgnoreCondition;

                    if (ignoreCondition == JsonIgnoreCondition.Always)
                        continue;
                    var name = p.Name;
                    var propertyType = p.PropertyType;
                    var v = p.PropertyInfo.GetValue(value);
                    if (v == null)
                    {
                        if (ignoreCondition == JsonIgnoreCondition.WhenWritingNull)
                        {
                            continue;
                        }
                        // r[name] = null;
                        writer.WriteNull(name);
                        continue;
                    }
                    switch (p.TypeCode)
                    {
                        case TypeCode.Boolean:
                            // r[name] = JsonValue.Create((bool)v);
                            writer.WriteBoolean(name, (bool)v);
                            continue;
                        case TypeCode.Char:
                            writer.WriteString(name, v.ToString());
                            continue;
                        case TypeCode.SByte:
                            writer.WriteNumber(name, (sbyte)v);
                            continue;
                        case TypeCode.Byte:
                            writer.WriteNumber(name, (byte)v);
                            continue;
                        case TypeCode.Int16:
                            writer.WriteNumber(name, (Int16)v);
                            continue;
                        case TypeCode.UInt16:
                            writer.WriteNumber(name, (UInt16)v);
                            continue;
                        case TypeCode.Int32:
                            if (propertyType.IsEnum)
                            {
                                // r[name] = propertyType.GetEnumName(v)!;
                                writer.WriteString(name, propertyType.GetEnumName(v));
                                continue;
                            }
                            writer.WriteNumber(name, (Int32)v);
                            continue;
                        case TypeCode.UInt32:
                            writer.WriteNumber(name, (UInt32)v);
                            continue;
                        case TypeCode.Int64:
                            writer.WriteNumber(name, (Int64)v);
                            continue;
                        case TypeCode.UInt64:
                            writer.WriteNumber(name, (UInt64)v);
                            continue;
                        case TypeCode.Single:
                            writer.WriteNumber(name, (Single)v);
                            continue;
                        case TypeCode.Double:
                            writer.WriteNumber(name, (Double)v);
                            continue;
                        case TypeCode.Decimal:
                            writer.WriteNumber(name, (decimal)v);
                            continue;
                        case TypeCode.DateTime:
                            // r[name] = JsonValue.Create(((DateTime)v).ToString(DateFormat));
                            writer.WriteString(name, ((DateTime)v).ToString(DateFormat));
                            continue;
                        case TypeCode.String:
                            // r[name] = JsonValue.Create((string)v);
                            writer.WriteString(name, (string)v);
                            continue;
                    }
                    if (propertyType == typeof(DateTimeOffset))
                    {
                        // r[name] = ((DateTimeOffset)v).UtcDateTime.ToString(DateFormat);
                        writer.WriteString(name, ((DateTimeOffset)v).UtcDateTime.ToString(DateFormat));
                        continue;
                    }
                    if (propertyType == typeof(Guid))
                    {
                        // r[name] = ((Guid)v).ToString();
                        writer.WriteString(name, ((Guid)v).ToString());
                        continue;
                    }
                    if (v is JsonNode jn)
                    {
                        // r[name] = jn;
                        writer.WritePropertyName(name);
                        writer.WriteRawValue(jn.ToString(), true);
                        continue;
                    }
                    if (v is Geometry g)
                    {
                        // r[name] = g.ToString();
                        // writer.WriteString(name, g.ToString());
                        writer.WritePropertyName(name);
                        if (g is Point point)
                        {
                            writer.WriteStartObject();
                            writer.WriteNumber("latitude", point.Y);
                            writer.WriteNumber("longitude", point.X);
                            writer.WriteString("wktString", point.AsText());
                            writer.WriteNumber("srid", point.SRID);
                            writer.WriteEndObject();
                            continue;
                        }
                        writer.WriteStartObject();
                        point = g.Centroid;
                        writer.WriteNumber("latitude", point.Y);
                        writer.WriteNumber("longitude", point.X);
                        writer.WriteString("wktString", g.AsText());
                        writer.WriteNumber("srid", point.SRID);
                        writer.WriteEndObject();
                        continue;
                    }
                    if (v is System.Collections.IDictionary vd)
                    {
                        writer.WritePropertyName(name);
                        writer.WriteStartObject();
                        var ve = vd.GetEnumerator();
                        var dictionaryNamingPolicy = options.DictionaryKeyPolicy;
                        while (ve.MoveNext())
                        {
                            if (ve.Key == null)
                                continue;
                            var keyName = ve.Key.ToString()!;
                            if (dictionaryNamingPolicy != null)
                            {
                                keyName = dictionaryNamingPolicy.ConvertName(keyName);
                            }
                            if (ve.Value == null)
                            {
                                writer.WriteNull(keyName);
                                continue;
                            }
                            writer.WritePropertyName(keyName);
                            Write(writer, ve.Value, options);
                            //JsonSerializer.Serialize(writer, ve.Value, options);
                        }
                        writer.WriteEndObject();
                        continue;
                    }
                    if (v is System.Collections.IEnumerable coll)
                    {
                        writer.WritePropertyName(name);
                        writer.WriteStartArray();
                        foreach (var c in coll)
                        {
                            if (c == null)
                                continue;
                            Write(writer, c, options);
                            // JsonSerializer.Serialize(writer, c, options);
                        }
                        writer.WriteEndArray();
                        //var list = new JsonArray();
                        //r[name] = list;
                        //pending.Enqueue(() =>
                        //{
                        //    foreach (var c in coll)
                        //    {
                        //        if (c != null)
                        //        {
                        //            var jc = SerializeToJson(c);
                        //            list.Add(jc);
                        //        }
                        //    }
                        //});
                        continue;
                    }
                    //  r[name] = SerializeToJson(v);
                    writer.WritePropertyName(name);
                    Write(writer, v, options);
                    // JsonSerializer.Serialize(writer, v, options);
                }

                writer.WriteEndObject();
            }
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
