using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using NeuroSpeech;
using NeuroSpeech.EntityAccessControl.Parser;
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

        public static object? SaveJsonOrValue(this PropertyInfo property, object target, JsonElement value)
        {
            var type = property.PropertyType;
            type = Nullable.GetUnderlyingType(type) ?? type;
            var isNullable = type != property.PropertyType;
            var v = value.DeserializeJsonElement(type, isNullable);
            property.SetValue(target, v);
            return v;
        }

        private static int ParseAsSByte(this string? value) => string.IsNullOrEmpty(value) ? (sbyte)0 : sbyte.Parse(value);

        private static short ParseAsByte(this string? value) => string.IsNullOrEmpty(value) ? (byte)0 : byte.Parse(value);

        private static int ParseAsInt16(this string? value) => string.IsNullOrEmpty(value) ? (short)0 : short.Parse(value);

        private static int ParseAsInt32(this string? value) => string.IsNullOrEmpty(value) ? (int)0 : int.Parse(value);

        private static long ParseAsInt64(this string? value) => string.IsNullOrEmpty(value) ? (long)0 : long.Parse(value);

        private static ushort ParseAsUInt16(this string? value) => string.IsNullOrEmpty(value) ? (ushort)0 : ushort.Parse(value);

        private static uint ParseAsUInt32(this string? value) => string.IsNullOrEmpty(value) ? (uint)0 : uint.Parse(value);

        private static ulong ParseAsUInt64(this string? value) => string.IsNullOrEmpty(value) ? (ulong)0 : ulong.Parse(value);

        private static decimal ParseAsDecimal(this string? value) => string.IsNullOrEmpty(value) ? (decimal)0 : decimal.Parse(value);

        private static double ParseAsDouble(this string? value) => string.IsNullOrEmpty(value) ? (double)0 : double.Parse(value);

        private static float ParseAsFloat(this string? value) => string.IsNullOrEmpty(value) ? (float)0 : float.Parse(value);



        private static object? DeserializeJsonElement(this JsonElement target, Type type, bool isNullable)
        {
            switch (target.ValueKind)
            {
                case JsonValueKind.Object:

                    if (type == typeof(Geometry))
                    {
                        var qp = new QueryParameter(target);
                        return (Geometry?)qp;
                    }

                    throw new ArgumentException($"Cannot convert object to {type.FullName}");
                case JsonValueKind.Array:
                    throw new ArgumentException($"Cannot convert array to {type.FullName}");
                case JsonValueKind.Null:
                case JsonValueKind.Undefined:
                    if (type.IsEnum)
                    {
                        return Enum.GetValues(type).GetValue(0)!;
                    }
                    if (type.IsValueType)
                    {
                        return Activator.CreateInstance(type);
                    }
                    return null;
                case JsonValueKind.True:
                    switch (Type.GetTypeCode(type))
                    {
                        case TypeCode.Boolean:
                            return true;
                        case TypeCode.String:
                            return "true";
                        default:
                            throw new ArgumentException($"Cannot create boolean to {type.FullName}");
                    }
                case JsonValueKind.False:
                    switch (Type.GetTypeCode(type))
                    {
                        case TypeCode.Boolean:
                            return false;
                        case TypeCode.String:
                            return "false";
                        default:
                            throw new ArgumentException($"Cannot create boolean to {type.FullName}");
                    }
                case JsonValueKind.String:
                    var stringValue = target.GetString()!;
                    if (type.IsEnum)
                    {
                        return Enum.Parse(type, stringValue, true);
                    }
                    switch (Type.GetTypeCode(type))
                    {
                        case TypeCode.Boolean:
                            return Convert.ToBoolean(target.GetString());
                        case TypeCode.Int16:
                            return stringValue.ParseAsInt16();
                        case TypeCode.Int32:
                            return stringValue.ParseAsInt32();
                        case TypeCode.Int64:
                            return stringValue.ParseAsInt64();
                        case TypeCode.UInt16:
                            return stringValue.ParseAsUInt16();
                        case TypeCode.UInt32:
                            return stringValue.ParseAsUInt32();
                        case TypeCode.UInt64:
                            return stringValue.ParseAsUInt64();
                        case TypeCode.SByte:
                            return stringValue.ParseAsSByte();
                        case TypeCode.Byte:
                            return stringValue.ParseAsByte();
                        case TypeCode.Char:
                            return stringValue.Length > 0 ? stringValue[0] : (char)0;
                        case TypeCode.String:
                            return stringValue;
                        case TypeCode.Single:
                            return stringValue.ParseAsFloat();
                        case TypeCode.Double:
                            return stringValue.ParseAsDouble();
                        case TypeCode.DateTime:
                            if (String.IsNullOrEmpty(stringValue)) {
                                if (isNullable)
                                    return null;
                                return DateTime.MinValue;
                            }
                            return DateTime.Parse(target.GetString()!, null, System.Globalization.DateTimeStyles.AdjustToUniversal);
                        case TypeCode.Decimal:
                            return stringValue.ParseAsDecimal();
                    }
                    if (type == typeof(DateTimeOffset))
                    {
                        if (String.IsNullOrEmpty(stringValue))
                        {
                            if (isNullable)
                                return null;
                            return DateTime.MinValue;
                        }
                        return DateTimeOffset.Parse(target.GetString()!, null, System.Globalization.DateTimeStyles.AdjustToUniversal);
                    }
                    break;
                case JsonValueKind.Number:
                    if (type.IsEnum)
                    {
                        return Enum.ToObject(type, target.GetInt32());
                    }
                    switch (Type.GetTypeCode(type))
                    {
                        case TypeCode.Boolean:
                            return target.GetDouble() != 0;
                        case TypeCode.Int16:
                            return target.GetInt16();
                        case TypeCode.Int32:
                            return target.GetInt32();
                        case TypeCode.Int64:
                            return target.GetInt64();
                        case TypeCode.UInt16:
                            return target.GetUInt16();
                        case TypeCode.UInt32:
                            return target.GetUInt32();
                        case TypeCode.UInt64:
                            return target.GetUInt64();
                        case TypeCode.SByte:
                            return target.GetSByte();
                        case TypeCode.Byte:
                            return target.GetByte();
                        case TypeCode.Char:
                            return target.GetInt16();
                        case TypeCode.String:
                            return target.GetRawText();
                        case TypeCode.Single:
                            return target.GetSingle();
                        case TypeCode.Double:
                            return target.GetDouble();
                        case TypeCode.DateTime:
                            return new DateTime(target.GetInt64());
                        case TypeCode.Decimal:
                            return target.GetDecimal();
                    }
                    break;
            }

            throw new ArgumentException($"Cannot convert {target.ValueKind} {target} to {type.FullName}");
            //if (type.IsEnum)
            //{
            //    if (target.ValueKind == JsonValueKind.Number)
            //        return Enum.ToObject(type, target.GetInt32());
            //    return Enum.Parse(type, target.GetString()!, true);
            //}
            //switch (Type.GetTypeCode(type))
            //{
            //    case TypeCode.Boolean:
            //        if(target.ValueKind == JsonValueKind.Number)
            //        {
            //            return target.GetDouble() != 0;
            //        }
            //        if (target.ValueKind == JsonValueKind.True)
            //            return true;
            //        if (target.ValueKind == JsonValueKind.False)
            //            return false;
            //        break;
            //    case TypeCode.Int16:
            //        if(target.ValueKind == JsonValueKind.Number)
            //            return target.GetInt16();
            //        break;
            //    case TypeCode.Int32:
            //        if (target.ValueKind == JsonValueKind.Number)
            //            return target.GetInt32();
            //        break;
            //    case TypeCode.Int64:
            //        if (target.ValueKind == JsonValueKind.Number)
            //            return target.GetInt64();
            //        break;
            //    case TypeCode.UInt16:
            //        if (target.ValueKind == JsonValueKind.Number)
            //            return target.GetUInt16();
            //        break;
            //    case TypeCode.UInt32:
            //        if (target.ValueKind == JsonValueKind.Number)
            //            return target.GetUInt32();
            //        break;
            //    case TypeCode.UInt64:
            //        if (target.ValueKind == JsonValueKind.Number)
            //            return target.GetUInt64();
            //        break;
            //    case TypeCode.SByte:
            //        if (target.ValueKind == JsonValueKind.Number)
            //            return target.GetSByte();
            //        break;
            //    case TypeCode.Byte:
            //        if (target.ValueKind == JsonValueKind.Number)
            //            return target.GetByte();
            //        break;
            //    case TypeCode.Char:
            //        if (target.ValueKind == JsonValueKind.Number)
            //            return target.GetInt16();
            //        break;
            //    case TypeCode.String:
            //        if (target.ValueKind == JsonValueKind.String)
            //            return target.GetString()!;
            //        return target.GetRawText();
            //    case TypeCode.Single:
            //        if (target.ValueKind == JsonValueKind.Number)
            //            return target.GetSingle();
            //        break;
            //    case TypeCode.Double:
            //        if (target.ValueKind == JsonValueKind.Number)
            //            return target.GetDouble();
            //        break;
            //    case TypeCode.DateTime:
            //        return DateTime.Parse(target.GetString()!);

            //}
            //string text = target.ValueKind == JsonValueKind.String
            //    ? target.GetString()!
            //    : target.GetRawText();
            //if(type == typeof(DateTimeOffset))
            //{
            //    return DateTimeOffset.Parse(text);
            //}
            
            //return Convert.ChangeType(text, type);
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
