using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace NeuroSpeech.EntityAccessControl
{
    public class EntitySerializationSettings
    {
        public JsonNamingPolicy? NamingPolicy;

        public Func<object, string>? GetTypeName;
    }

    public static class EntityJsonSerializer
    {


        static readonly string DateFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'FFFFFFFZ";


        public static object? SerializeToJson(this DbContext context, object entity, JsonNamingPolicy? namingPolicy = null)
        {
            var settings = new EntitySerializationSettings
            {
                NamingPolicy = namingPolicy,
                GetTypeName = x => context.Entry(x).Metadata.Name
            };
            return SerializeToJson(entity, new Dictionary<object, int>(), settings);
        }

        public static object? SerializeToJson<T>(this DbContext context, List<T> items, JsonNamingPolicy? namingPolicy = null)
        {
            var settings = new EntitySerializationSettings { 
                NamingPolicy = namingPolicy,
                GetTypeName = x => context.Entry(x).Metadata.Name
            };
            var all = new Dictionary<object, int>();
            var result = new List<object>();
            foreach(var item in items)
            {
                var ji = SerializeToJson(item, all, settings);
                if (ji == null)
                    continue;
                result.Add(ji);
            }
            return result;
        }

        public static object? SerializeToJson(object? e, Dictionary<object,int> added, EntitySerializationSettings settings)
        {
            if (e == null)
                return null;
            if(added.TryGetValue(e, out  var existingIndex))
            {
                return new Dictionary<string, object> {
                    { "$id", existingIndex }
                };
            }
            var index = added.Count;
            added[e] = index;
            var r = new Dictionary<string, object>() {
                { "$id", index }
            };
            var d = settings.GetTypeName?.Invoke(e) ?? e.GetType().FullName;
            var namingPolicy = settings.NamingPolicy ?? JsonNamingPolicy.CamelCase;
            r["$type"] = d;
            foreach (var p in e.GetType().GetProperties())
            {
                if (p.GetCustomAttribute<System.Text.Json.Serialization.JsonIgnoreAttribute>() != null)
                    continue;
                var name = namingPolicy.ConvertName(p.Name);
                var v = p.GetValue(e);
                if (v == null)
                    continue;
                if (v is not string && v is System.Collections.IEnumerable coll)
                {
                    var list = new List<object>();
                    foreach (var c in coll)
                    {
                        var jc = SerializeToJson(c, added, settings);
                        if (jc != null)
                        {
                            list.Add(jc);
                        }
                    }
                    r[name] = list;
                    continue;
                }
                var t = p.PropertyType;
                t = Nullable.GetUnderlyingType(t) ?? t;
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
                    r[name] = v;
                    continue;
                }
                var sv = SerializeToJson(v, added, settings);
                if (sv != null)
                {
                    r[name] = sv;
                }
            }
            return r;
        }

    }
}
