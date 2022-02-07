using Microsoft.EntityFrameworkCore;
using NeuroSpeech;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace NeuroSpeech.EntityAccessControl
{

    public static class ClrEntityExtensions
    {

        public static void SaveJsonOrValue(this PropertyInfo property, object target, JsonElement value)
        {
            var type = property.PropertyType;
            type = Nullable.GetUnderlyingType(type) ?? type;
            property.SetValue(target, value.DeserializeJsonElement(type));
        }

        public static object? DeserializeJsonElement(this JsonElement target, Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;
            if (target.ValueKind == JsonValueKind.Null)
            {
                if(type.IsEnum)
                {
                    return Enum.GetValues(type).GetValue(0)!;
                }
                return null;
            }
            if (type.IsEnum)
            {
                if (target.ValueKind == JsonValueKind.Number)
                    return Enum.ToObject(type, target.GetInt32());
                return Enum.Parse(type, target.GetString()!, true);
            }
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Boolean:
                    if(target.ValueKind == JsonValueKind.Number)
                    {
                        return target.GetDouble() != 0;
                    }
                    if (target.ValueKind == JsonValueKind.True)
                        return true;
                    if (target.ValueKind == JsonValueKind.False)
                        return false;
                    break;
                case TypeCode.Int16:
                    if(target.ValueKind == JsonValueKind.Number)
                        return target.GetInt16();
                    break;
                case TypeCode.Int32:
                    if (target.ValueKind == JsonValueKind.Number)
                        return target.GetInt32();
                    break;
                case TypeCode.Int64:
                    if (target.ValueKind == JsonValueKind.Number)
                        return target.GetInt64();
                    break;
                case TypeCode.UInt16:
                    if (target.ValueKind == JsonValueKind.Number)
                        return target.GetUInt16();
                    break;
                case TypeCode.UInt32:
                    if (target.ValueKind == JsonValueKind.Number)
                        return target.GetUInt32();
                    break;
                case TypeCode.UInt64:
                    if (target.ValueKind == JsonValueKind.Number)
                        return target.GetUInt64();
                    break;
                case TypeCode.SByte:
                    if (target.ValueKind == JsonValueKind.Number)
                        return target.GetSByte();
                    break;
                case TypeCode.Byte:
                    if (target.ValueKind == JsonValueKind.Number)
                        return target.GetByte();
                    break;
                case TypeCode.Char:
                    if (target.ValueKind == JsonValueKind.Number)
                        return target.GetInt16();
                    break;
                case TypeCode.String:
                    if (target.ValueKind == JsonValueKind.String)
                        return target.GetString()!;
                    return target.GetRawText();
                case TypeCode.Single:
                    if (target.ValueKind == JsonValueKind.Number)
                        return target.GetSingle();
                    break;
                case TypeCode.Double:
                    if (target.ValueKind == JsonValueKind.Number)
                        return target.GetDouble();
                    break;
                case TypeCode.DateTime:
                    return DateTime.Parse(target.GetString()!);

            }
            string text = target.ValueKind == JsonValueKind.String
                ? target.GetString()!
                : target.GetRawText();
            if(type == typeof(DateTimeOffset))
            {
                return DateTimeOffset.Parse(text);
            }
            
            return Convert.ChangeType(text, type);
        }

        //public static Task<object> FindByKeysAsync(
        //    this DbContext db,
        //    ClrEntity entity)
        //{
        //    var t = db.Model.GetEntityTypes().FirstOrDefault(x => x.Name.EqualsIgnoreCase(entity.Type));
        //    var e = Activator.CreateInstance(t.ClrType);
        //    foreach(var key in t.GetKeys())
        //    {
        //        foreach(var p in key.Properties)
        //        {
        //            if(entity.Keys.TryGetValue(p.Name, out var v))
        //            {
        //                var pr = p.PropertyInfo;
        //                pr.SaveJsonOrValue(e, v);
        //            }
        //        }
        //    }
        //    return db.FindByKeysAsync(t, e);
        //}

        //public static async Task<object> SerializeAsync(this ISecureRepository db, object item, bool loadNav = true)
        //{
        //    var d = new Dictionary<string, object>();
        //    var entry = db.Entry(item);
        //    foreach (var p in entry.Properties)
        //    {
        //        var m = p.Metadata;
        //        if (m.IsKey())
        //        {
        //            d[m.Name] = p.CurrentValue;
        //            continue;
        //        }
        //        if (m.PropertyInfo.PropertyType.IsEnum)
        //        {
        //            d[m.Name] = m.PropertyInfo.PropertyType.GetEnumName(p.CurrentValue)!;
        //            continue;
        //        }
        //        d[m.Name] = p.CurrentValue;
        //    }
        //    if (loadNav)
        //    {
        //        foreach (var np in entry.Navigations)
        //        {
        //            if (np.Metadata.IsCollection)
        //                continue;
        //            if (np.CurrentValue == null)
        //                continue;
        //            d[np.Metadata.Name.ToCamelCase()] = await db.SerializeAsync(np.CurrentValue, false);
        //        }
        //    }
        //    d["$type"] = entry.Metadata.Name;
        //    return d;
        //}
    }
}
