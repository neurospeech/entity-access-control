using Microsoft.EntityFrameworkCore;
using NeuroSpeech.EntityAccessControl.Internal;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace NeuroSpeech.EntityAccessControl
{
    public class EntitySerializationSettings
    {
        public JsonNamingPolicy? NamingPolicy;

        public Func<Type, string>? GetTypeName;

        public Func<PropertyInfo, JsonIgnoreCondition> GetIgnoreCondition = GetDefaultIgnoreAttribute;

        private static JsonIgnoreCondition GetDefaultIgnoreAttribute(PropertyInfo property)
        {
            return property.GetCustomAttribute<JsonIgnoreAttribute>()?.Condition
                ?? System.Text.Json.Serialization.JsonIgnoreCondition.Never;
        }
    }

    public class EntityJsonSerializer
    {
        static readonly string DateFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'FFFFFFFZ";

        private Dictionary<object, int> added = new Dictionary<object, int>();
        private EntitySerializationSettings settings;
        private Queue<Action> pending = new Queue<Action>();

        public EntityJsonSerializer(EntitySerializationSettings? settings)
        {
            this.settings = settings ?? new EntitySerializationSettings { 
                
            };
        }

        public EntityJsonSerializer(DbContext db)
        {
            this.settings = new EntitySerializationSettings {
                GetTypeName = (x) => db.Model.FindEntityType(x)?.Name ?? x.FullName!,
                NamingPolicy = JsonNamingPolicy.CamelCase,
            };
        }

        public JsonObject? Serialize(object? entity)
        {
            var r = SerializeToJson(entity);
            while(pending.TryDequeue(out var a))
            {
                a();
            }
            return r;
        }

        private JsonObject? SerializeToJson(object? e)
        {
            if (e == null)
                return null;
            if (added.TryGetValue(e, out var existingIndex))
            {
                return new JsonObject() {
                    { "$id", existingIndex }
                };
            }
            var index = added.Count;
            added[e] = index;
            var r = new JsonObject() {
                { "$id", index }
            };
            var et = e.GetType();
            var d = settings.GetTypeName?.Invoke(et) ?? et.FullName;
            var namingPolicy = settings.NamingPolicy ?? JsonNamingPolicy.CamelCase;
            r["$type"] = d;
            foreach (var p in e.GetType().GetProperties())
            {
                var ignoreCondition = settings.GetIgnoreCondition(p);
                
                if (ignoreCondition == JsonIgnoreCondition.Always)
                    continue;
                var name = namingPolicy.ConvertName(p.Name);
                var propertyType = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;
                var v = p.GetValue(e);
                if (v == null)
                {
                    if (ignoreCondition == JsonIgnoreCondition.WhenWritingNull)
                    {
                        continue;
                    }                    
                    r[name] = null!;
                    continue;
                }
                if (v is not string && v is System.Collections.IEnumerable coll)
                {
                    var list = new JsonArray();
                    r[name] = list;
                    pending.Enqueue(() =>
                    {
                        foreach (var c in coll)
                        {
                            var jc = SerializeToJson(c);
                            if (jc != null)
                            {
                                list.Add(jc);
                            }
                        }
                    });
                    continue;
                }
                var t = p.PropertyType;
                t = Nullable.GetUnderlyingType(t) ?? t;
                if (t.IsEnum)
                {
                    r[name] = t.GetEnumName(v)!;
                    continue;
                }
                if (t.IsValueType || t == typeof(string))
                {
                    switch (v)
                    {
                        case DateTime dt:
                            r[name] = dt.ToString(DateFormat);
                            continue;
                        case DateTimeOffset dto:
                            r[name] = dto.UtcDateTime.ToString(DateFormat);
                            continue;
                    }
                    r[name] = JsonValue.Create(v);
                    continue;
                }
                var sv = SerializeToJson(v);
                if (sv != null)
                {
                    r[name] = sv;
                }
            }
            return r;
        }
    }

}
