using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace NeuroSpeech.EntityAccessControl
{
    public static class JsonExtensions
    {

        public static bool TryGetPropertyCaseInsensitive(
            in this JsonElement element,
            string name,
            out JsonElement value)
        {
            foreach(var kvp in element.EnumerateObject())
            {
                if(kvp.Name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                {
                    value = kvp.Value;
                    return true;
                }
            }
            value = default;
            return false;
        }

        public static bool TryGetStringProperty(
            in this JsonElement element, 
            string name, 
            [NotNullWhen(true)]
            out string? value)
        {
            if(element.TryGetProperty(name, out var token))
            {
                value = token.GetString();
                return true;
            }
            value = default;
            return false;
        }

        private static object[] Empty = new object[0];

        public static object?[] JsonToNative(this object?[]? values)
        {
            if (values == null)
                return Empty;
            var r = new object?[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                r[i] = JsonToNative(values[i]);
            }
            return r;
        }

        public static object? JsonToNative(this object? v)
        {
            if (v is JsonElement je)
            {
                switch (je.ValueKind)
                {
                    case JsonValueKind.Undefined:
                        return null;
                    case JsonValueKind.Object:
                        return null;
                    case JsonValueKind.Array:
                        var l = je.GetArrayLength();
                        var a = new object?[l];
                        for (int i = 0; i < l; i++)
                        {
                            a[i] = JsonToNative(je[i]);
                        }
                        return a;
                    case JsonValueKind.String:
                        return je.GetString();
                    case JsonValueKind.Number:
                        var d = je.GetDouble();
                        if ((d % 1) == 0) {
                            if(int.MinValue <= d && d <= int.MaxValue)
                            {
                                return (int)d;
                            }
                            return (long)d;
                        }
                        return d;
                    case JsonValueKind.True:
                        return true;
                    case JsonValueKind.False:
                        return false;
                    case JsonValueKind.Null:
                        return null;
                }
            }
            return v;
        }

        public static long AsInt64( in this JsonElement @this)
        {
            switch (@this.ValueKind)
            {
                case JsonValueKind.Number:
                    return @this.GetInt64();
                case JsonValueKind.String:
                    return long.Parse(@this.GetString()!);
                case JsonValueKind.Null:
                    return 0;
            }
            throw new ArgumentException($"Unable to convert {@this} to long");
        }

        public static int AsInt32(in this JsonElement @this)
        {
            switch (@this.ValueKind)
            {
                case JsonValueKind.Number:
                    return @this.GetInt32();
                case JsonValueKind.String:
                    return int.Parse(@this.GetString()!);
                case JsonValueKind.Null:
                    return 0;
            }
            throw new ArgumentException($"Unable to convert {@this} to int");
        }

        public static float AsSingle(in this JsonElement @this)
        {
            switch (@this.ValueKind)
            {
                case JsonValueKind.Number:
                    return @this.GetSingle();
                case JsonValueKind.String:
                    return float.Parse(@this.GetString()!);
                case JsonValueKind.Null:
                    return 0;
            }
            throw new ArgumentException($"Unable to convert {@this} to double");
        }

        public static double AsDouble(in this JsonElement @this)
        {
            switch (@this.ValueKind)
            {
                case JsonValueKind.Number:
                    return @this.GetDouble();
                case JsonValueKind.String:
                    return double.Parse(@this.GetString()!);
                case JsonValueKind.Null:
                    return 0;
            }
            throw new ArgumentException($"Unable to convert {@this} to double");
        }

        public static decimal AsDecimal(in this JsonElement @this)
        {
            switch (@this.ValueKind)
            {
                case JsonValueKind.Number:
                    return @this.GetDecimal();
                case JsonValueKind.String:
                    return decimal.Parse(@this.GetString()!);
                case JsonValueKind.Null:
                    return 0;
            }
            throw new ArgumentException($"Unable to convert {@this} to decimal");
        }

        public static bool AsBoolean(in this JsonElement @this)
        {
            switch (@this.ValueKind)
            {
                case JsonValueKind.Number:
                    return @this.GetDouble() > 0;
                case JsonValueKind.True:
                    return true;
                case JsonValueKind.False:
                    return false;
                case JsonValueKind.String:
                    var text = @this.GetString()!;
                    if (text.EqualsIgnoreCase("true"))
                        return true;
                    if (double.TryParse(text, out var n))
                        return n > 0;
                    if (text.EqualsIgnoreCase("false"))
                        return false;
                    return text.Length > 0;
                case JsonValueKind.Null:
                    return false;
            }
            throw new ArgumentException($"Unable to convert {@this} to bool");
        }

        public static string? AsString(in this JsonElement @this)
        {
            if (@this.ValueKind == JsonValueKind.String)
            {
                return @this.GetString()!;
            }
            if (@this.ValueKind == JsonValueKind.Null)
            {
                return null;
            }
            return @this.GetRawText();
        }

    }
}
