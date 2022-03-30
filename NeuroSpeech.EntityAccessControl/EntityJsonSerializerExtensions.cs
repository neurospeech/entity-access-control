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
        public Func<Type, string>? GetTypeName;

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

        }

        public EntitySerializationSettings(DbContext db)
        {
            GetTypeName = (x) => db.Model.FindEntityType(x)?.Name ?? x.FullName!;
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

    //public class EntityJsonSerializer
    //{
    //    static readonly string DateFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'FFFFFFFZ";

    //    private readonly Dictionary<object, int> added = new(ReferenceEqualityComparer.Instance);
    //    private readonly EntitySerializationSettings settings;
    //    private readonly Queue<Action> pending = new();

    //    public EntityJsonSerializer(EntitySerializationSettings? settings)
    //    {
    //        this.settings = settings ?? new EntitySerializationSettings { 
                
    //        };
    //    }

    //    public EntityJsonSerializer(DbContext db)
    //    {
    //        this.settings = new EntitySerializationSettings {
    //            GetTypeName = (x) => db.Model.FindEntityType(x)?.Name ?? x.FullName!,
    //            NamingPolicy = JsonNamingPolicy.CamelCase,
    //        };
    //    }

    //    public JsonObject? Serialize(object? entity)
    //    {
    //        if (entity == null)
    //        {
    //            return null;
    //        }
    //        var r = SerializeToJson(entity);
    //        while(pending.TryDequeue(out var a))
    //        {
    //            a();
    //        }
    //        return r;
    //    }

    //    public JsonNode? SerializeList<T>(List<T>? items)
    //    {
    //        if (items == null)
    //        {
    //            return null;
    //        }
    //        var r = new JsonArray(items.Count);
    //        foreach (var item in items)
    //        {
    //            if (item == null)
    //            {
    //                r.Add(null);
    //                continue;
    //            }
    //            r.Add(SerializeToJson(item));
    //        }
    //        while (pending.TryDequeue(out var a))
    //        {
    //            a();
    //        }
    //        return r;
    //    }

    //    private JsonObject? SerializeToJson(object e)
    //    {
    //        if (added.TryGetValue(e, out var existingIndex))
    //        {
    //            return new JsonObject() {
    //                { "$id", existingIndex }
    //            };
    //        }
    //        var index = added.Count;
    //        added[e] = index;
    //        var r = new JsonObject() {
    //            { "$id", index }
    //        };
    //        var et = e.GetType();
    //        var d = et.StaticCacheGetOrCreate((et) => settings.GetTypeName?.Invoke(et) ?? et.FullName);
    //        var namingPolicy = settings.NamingPolicy ?? JsonNamingPolicy.CamelCase;
    //        r["$type"] = d;
    //        var properties = et.StaticCacheGetOrCreate((et) =>
    //            et.GetProperties()
    //            .Where(p => 
    //                !(p.GetIndexParameters()?.Length > 0))
    //            .Select(p =>
    //            {
    //                var propertyType = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;
    //                return new
    //                {
    //                    PropertyType = propertyType,
    //                    p.Name,
    //                    GetValue = (Func<object, object>)p.GetValue!,
    //                    PropertyInfo = p,
    //                    TypeCode = Type.GetTypeCode(propertyType)
    //                };
    //            })
    //            .ToList());
    //        foreach (var p in properties)
    //        {
    //            var ignoreCondition = settings.GetIgnoreCondition(p.PropertyInfo);
                
    //            if (ignoreCondition == JsonIgnoreCondition.Always)
    //                continue;
    //            var name = namingPolicy.ConvertName(p.Name);
    //            var propertyType = p.PropertyType;
    //            var v = p.GetValue(e);
    //            if (v == null)
    //            {
    //                if (ignoreCondition == JsonIgnoreCondition.WhenWritingNull)
    //                {
    //                    continue;
    //                }                    
    //                r[name] = null;
    //                continue;
    //            }
    //            switch (p.TypeCode)
    //            {
    //                case TypeCode.Boolean:
    //                    r[name] = JsonValue.Create((bool)v);
    //                    continue;
    //                case TypeCode.Char:
    //                    r[name] = JsonValue.Create((char)v);
    //                    continue;
    //                case TypeCode.SByte:
    //                    r[name] = JsonValue.Create((sbyte)v);
    //                    continue;
    //                case TypeCode.Byte:
    //                    r[name] = JsonValue.Create((byte)v);
    //                    continue;
    //                case TypeCode.Int16:
    //                    r[name] = JsonValue.Create((Int16)v);
    //                    continue;
    //                case TypeCode.UInt16:
    //                    r[name] = JsonValue.Create((UInt16)v);
    //                    continue;
    //                case TypeCode.Int32:
    //                    if (propertyType.IsEnum)
    //                    {
    //                        r[name] = propertyType.GetEnumName(v)!;
    //                        continue;
    //                    }
    //                    r[name] = JsonValue.Create((Int32)v);
    //                    continue;
    //                case TypeCode.UInt32:
    //                    r[name] = JsonValue.Create((UInt32)v);
    //                    continue;
    //                case TypeCode.Int64:
    //                    r[name] = JsonValue.Create((Int64)v);
    //                    continue;
    //                case TypeCode.UInt64:
    //                    r[name] = JsonValue.Create((UInt64)v);
    //                    continue;
    //                case TypeCode.Single:
    //                    r[name] = JsonValue.Create((Single)v);
    //                    continue;
    //                case TypeCode.Double:
    //                    r[name] = JsonValue.Create((Double)v);
    //                    continue;
    //                case TypeCode.Decimal:
    //                    r[name] = JsonValue.Create((Decimal)v);
    //                    continue;
    //                case TypeCode.DateTime:
    //                    r[name] = JsonValue.Create(((DateTime)v).ToString(DateFormat));
    //                    continue;
    //                case TypeCode.String:
    //                    r[name] = JsonValue.Create((string)v);
    //                    continue;
    //            }
    //            if (propertyType == typeof(DateTimeOffset))
    //            {
    //                r[name] = ((DateTimeOffset)v).UtcDateTime.ToString(DateFormat);
    //                continue;
    //            }
    //            if (propertyType == typeof(Guid))
    //            {
    //                r[name] = ((Guid)v).ToString();
    //                continue;
    //            }
    //            if (v is JsonNode jn)
    //            {
    //                r[name] = jn;
    //                continue;
    //            }
    //            if (v is Geometry g)
    //            {
    //                r[name] = g.ToString();
    //                continue;
    //            }
    //            if (v is System.Collections.IDictionary vd)
    //            {
    //                var jd = new JsonObject();
    //                r[name] = jd;
    //                pending.Enqueue(() => {
    //                    var ve = vd.GetEnumerator();
    //                    while(ve.MoveNext())
    //                    {
    //                        if (ve.Value == null) {
    //                            jd[ve.Key.ToString()!] = null;
    //                            continue;
    //                        }
    //                        jd[ve.Key.ToString()!] = SerializeToJson(ve.Value);
    //                    }
    //                });
    //                continue;
    //            }
    //            if (v is System.Collections.IEnumerable coll)
    //            {
    //                var list = new JsonArray();
    //                r[name] = list;
    //                pending.Enqueue(() =>
    //                {
    //                    foreach (var c in coll)
    //                    {
    //                        if (c != null)
    //                        {
    //                            var jc = SerializeToJson(c);
    //                            list.Add(jc);
    //                        }
    //                    }
    //                });
    //                continue;
    //            }
    //            r[name] = SerializeToJson(v);
    //        }
    //        return r;
    //    }
    //}

}
