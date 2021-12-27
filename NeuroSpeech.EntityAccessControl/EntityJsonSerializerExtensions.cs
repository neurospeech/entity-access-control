using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace NeuroSpeech.EntityAccessControl
{
    public class EntitySerializationSettings
    {
        public JsonNamingPolicy? NamingPolicy;

        public Func<object, string>? GetTypeName;

        public Func<object, PropertyInfo, bool>? IsForeignKey;

        public Func<object, object>? Map;
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
                GetTypeName = (x) => db.Entry(x).Metadata.Name,
                NamingPolicy = JsonNamingPolicy.CamelCase,
                IsForeignKey = (x, p) => db.Entry(x).Property(p.Name)?.Metadata?.IsForeignKey() ?? false
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
            if (settings.Map != null)
            {
                e = settings.Map(e);
            }
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
            var d = settings.GetTypeName?.Invoke(e) ?? e.GetType().FullName;
            var namingPolicy = settings.NamingPolicy ?? JsonNamingPolicy.CamelCase;
            r["$type"] = d;
            foreach (var p in e.GetType().GetProperties())
            {
                var att = p.GetCustomAttribute<System.Text.Json.Serialization.JsonIgnoreAttribute>();
                if (att?.Condition == System.Text.Json.Serialization.JsonIgnoreCondition.Always)
                    continue;
                var name = namingPolicy.ConvertName(p.Name);
                var propertyType = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;
                var v = p.GetValue(e);
                if (v == null)
                {
                    if (att?.Condition == System.Text.Json.Serialization.JsonIgnoreCondition.Always)
                    {
                        r[name] = null!;
                        continue;
                    }
                    
                    if ((propertyType == typeof(string) ||
                        propertyType.IsValueType) &&
                        (settings.IsForeignKey?.Invoke(e, p) ?? false))
                    {
                        r[name] = null!;
                    }
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

    //public static class EntityJsonSerializerExtensions
    //{


    //    static readonly string DateFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'FFFFFFFZ";


    //    public static object? SerializeToJson(this DbContext context, object entity, JsonNamingPolicy? namingPolicy = null)
    //    {
    //        var settings = new EntitySerializationSettings
    //        {
    //            NamingPolicy = namingPolicy,
    //            GetTypeName = x => context.Entry(x).Metadata.Name,
    //            IsForeignKey = (x, p) => context.Entry(x).Property(p.Name).Metadata.IsForeignKey()
    //        };
    //        return SerializeToJson(entity, new Dictionary<object, int>(), settings);
    //    }

    //    public static object? SerializeToJson<T>(this DbContext context, List<T> items, JsonNamingPolicy? namingPolicy = null)
    //    {
    //        var settings = new EntitySerializationSettings { 
    //            NamingPolicy = namingPolicy,
    //            GetTypeName = x => context.Entry(x).Metadata.Name,
    //            IsForeignKey = (x, p) => context.Entry(x).Property(p.Name).Metadata.IsForeignKey()
    //        };
    //        var all = new Dictionary<object, int>();
    //        var result = new List<object>();
    //        foreach(var item in items)
    //        {
    //            var ji = SerializeToJson(item, all, settings);
    //            if (ji == null)
    //                continue;
    //            result.Add(ji);
    //        }
    //        return result;
    //    }

    //    public static object? SerializeToJson(object? e, Dictionary<object,int> added, EntitySerializationSettings settings)
    //    {
    //        if (e == null)
    //            return null;
    //        if(added.TryGetValue(e, out  var existingIndex))
    //        {
    //            return new Dictionary<string, object> {
    //                { "$id", existingIndex }
    //            };
    //        }
    //        var index = added.Count;
    //        added[e] = index;
    //        var r = new Dictionary<string, object>() {
    //            { "$id", index }
    //        };
    //        var d = settings.GetTypeName?.Invoke(e) ?? e.GetType().FullName;
    //        var namingPolicy = settings.NamingPolicy ?? JsonNamingPolicy.CamelCase;
    //        r["$type"] = d;
    //        foreach (var p in e.GetType().GetProperties())
    //        {
    //            var att = p.GetCustomAttribute<System.Text.Json.Serialization.JsonIgnoreAttribute>();
    //            if (att?.Condition == System.Text.Json.Serialization.JsonIgnoreCondition.Always)
    //                continue;
    //            var name = namingPolicy.ConvertName(p.Name);
    //            var v = p.GetValue(e);
    //            if (v == null)
    //            {
    //                if(settings.IsForeignKey?.Invoke(e, p) ?? false)
    //                {
    //                    r[name] = null!;
    //                    continue;
    //                }
                    

    //                if(att?.Condition == System.Text.Json.Serialization.JsonIgnoreCondition.Always)
    //                {
    //                    r[name] = null!;
    //                }
    //                continue;
    //            }
    //            if (v is not string && v is System.Collections.IEnumerable coll)
    //            {
    //                var list = new List<object>();
    //                foreach (var c in coll)
    //                {
    //                    var jc = SerializeToJson(c, added, settings);
    //                    if (jc != null)
    //                    {
    //                        list.Add(jc);
    //                    }
    //                }
    //                r[name] = list;
    //                continue;
    //            }
    //            var t = p.PropertyType;
    //            t = Nullable.GetUnderlyingType(t) ?? t;
    //            if (t.IsEnum)
    //            {
    //                r[name] = t.GetEnumName(v)!;
    //                continue;
    //            }
    //            if (t.IsValueType || t == typeof(string))
    //            {
    //                switch (v)
    //                {
    //                    case DateTime dt:
    //                        r[name] = dt.ToString(DateFormat);
    //                        continue;
    //                    case DateTimeOffset dto:
    //                        r[name] = dto.UtcDateTime.ToString(DateFormat);
    //                        continue;
    //                }
    //                r[name] = v;
    //                continue;
    //            }
    //            var sv = SerializeToJson(v, added, settings);
    //            if (sv != null)
    //            {
    //                r[name] = sv;
    //            }
    //        }
    //        return r;
    //    }

    //}
}
