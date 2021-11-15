using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace NeuroSpeech.EntityAccessControl.Internal
{

    public static class EnumerableExtensions
    {

        public static bool TryGetFirst<T,TCompare>(this IEnumerable<T> target, TCompare key, Func<T, TCompare, bool> fx, out T value)
        {
            foreach (var v in target)
            {
                if (fx(v, key))
                {
                    value = v;
                    return true;
                }
            }
            value = default!;
            return false;
        }

        public static bool TryGetFirst<T>(this IEnumerable<T> target, Func<T, bool> fx, out T value)
        {
            foreach (var v in target)
            {
                if (fx(v))
                {
                    value = v;
                    return true;
                }
            }
            value = default!;
            return false;
        }

    }

    public static class DictionaryExtensions
    {
        public static TValue GetOrCreate<TKey, TValue>(this IDictionary<TKey, TValue> d, TKey key, Func<TKey, TValue> factory)
        {
            if (d.TryGetValue(key, out var v))
                return v;
            d[key] = v = factory(key);
            return v;
        }
    }
    public static class TypeExtensions
    {

        private static Dictionary<Type, object> defaults = new Dictionary<Type, object> {
        };

        private static object Boolean = false;
        private static object Char = (char)0;
        private static object SByte = (sbyte)0;
        private static object Byte = (byte)0;
        private static object Int16 = (short)0;
        private static object UInt16 = (ushort)0;
        private static object Int32 = 0;
        private static object UInt32 = (uint)0;
        private static object Int64 = (long)0;
        private static object UInt64 = (ulong)0;
        private static object Single = (float)0;
        private static object Double = (double)0;
        private static object Decimal = (decimal)0;
        private static object DateTime = System.DateTime.MinValue;
        private static object DateTimeOffset = System.DateTimeOffset.MinValue;

        public static object? GetDefaultForType(this Type type) {
            type = Nullable.GetUnderlyingType(type) ?? type;
            if (!type.IsValueType)
                return null;
            var tc = Type.GetTypeCode(type);
            switch (tc)
            {
                case TypeCode.Boolean:
                    return Boolean;
                case TypeCode.Char:
                    return Char;
                case TypeCode.SByte:
                    return SByte;
                case TypeCode.Byte:
                    return Byte;
                case TypeCode.Int16:
                    return Int16;
                case TypeCode.UInt16:
                    return UInt16;
                case TypeCode.Int32:
                    return Int32;
                case TypeCode.UInt32:
                    return UInt32;
                case TypeCode.Int64:
                    return Int64;
                case TypeCode.UInt64:
                    return UInt64;
                case TypeCode.Single:
                    return Single;
                case TypeCode.Double:
                    return Double;
                case TypeCode.Decimal:
                    return Decimal;
                case TypeCode.DateTime:
                    return DateTime;
            }
            if (type == typeof(System.DateTimeOffset))
                return DateTimeOffset;
            return defaults.GetOrCreate(type, x => Activator.CreateInstance(x)!);
        }

        public static string ToTypeScript(this Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;
            var tc = Type.GetTypeCode(type);
            switch (tc)
            {
                case TypeCode.Boolean:
                    return "boolean";
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:
                    return "number";
                case TypeCode.DateTime:
                    return "DateTime";
                case TypeCode.Char:
                case TypeCode.String:
                    return "string";
            }

            if (type == typeof(DateTimeOffset))
                return "DateTime";
            return $"any /*{type.FullName}*/";
        }

        public static object GetOrCreate(this PropertyInfo property, object target)
        {
            var value = property.GetValue(target);
            if(value == null)
            {
                value = property.PropertyType.CreateClassInstance()!;
                property.SetValue(target, value);
            }
            return value;
        }


        /// <summary>
        /// Creates new Instance of class if it is a class, if it is of IList, ICollection as List
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static object? CreateClassInstance(this Type type)
        {
            if (type.IsInterface)
            {
                if (type.IsGenericType)
                {
                    var t = type.GetGenericArguments()[0];
                    var gt = type.GetGenericTypeDefinition();
                    if(gt == typeof(ICollection<>) || gt == typeof(IList<>))
                    {
                        var ct = typeof(List<>).MakeGenericType(t);
                        return Activator.CreateInstance(ct);
                    }
                }
            }
            return Activator.CreateInstance(type);
        }

        public static object? InvokeMethod(this object target, string method, params object[] values)
        {
            return target.GetType().InvokeMember(method,
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Default,
                null,
                target,
                values);
        }

    }
}
