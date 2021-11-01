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

    }
}
