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

    }

    public static class DictionaryExtensions
    {
        public static TValue GetOrCreate<TKey, TValue>(this IDictionary<TKey, TValue> d, TKey key, Func<TKey, TValue> factory)
        {
            if (d.TryGetValue(key, out var v))
                return v;
            v = factory(key);
            d.Add(key, v);
            return v;
        }
    }
    public static class TypeExtensions
    {
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
