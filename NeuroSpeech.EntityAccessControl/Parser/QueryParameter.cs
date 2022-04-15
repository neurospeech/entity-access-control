﻿using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;

namespace NeuroSpeech.EntityAccessControl
{
    public static class CastAs
    {
        public static string String(object n) => n.ToString()!;

        public static string String(int n) => n.ToString();

        internal static MethodInfo StringMethod = typeof(CastAs)
            .GetRuntimeMethod(nameof(String), new Type[] { typeof(int) })!;
    }

    public readonly struct QueryParameter: IEnumerable<object>
    {
        private readonly JsonElement element;

        public QueryParameter(System.Text.Json.JsonElement element)
        {
            this.element = element;
        }

        public IEnumerator<object> GetEnumerator()
        {
            foreach (var item in element.EnumerateArray())
            {
                switch (item.ValueKind)
                {
                    case JsonValueKind.Undefined:
                        break;
                    case JsonValueKind.Object:
                        break;
                    case JsonValueKind.Array:
                        break;
                    case JsonValueKind.String:
                        yield return item.GetString()!;
                        break;
                    case JsonValueKind.Number:
                        yield return item.GetInt64()!;
                        break;
                    case JsonValueKind.True:
                        yield return true;
                        break;
                    case JsonValueKind.False:
                        yield return false;
                        break;
                    case JsonValueKind.Null:
                        break;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public static implicit operator long (QueryParameter q)
        {
            return q.element.GetInt64();
        }

        public static implicit operator long?(QueryParameter q)
        {
            return q.element.ValueKind == JsonValueKind.Null ? null : q.element.GetInt64();
        }

        public static implicit operator int(QueryParameter q)
        {
            return q.element.GetInt32();
        }

        public static implicit operator int?(QueryParameter q)
        {
            return q.element.ValueKind == JsonValueKind.Null ? null : q.element.GetInt32();
        }

        public static implicit operator bool(QueryParameter q)
        {
            return q.element.GetBoolean();
        }

        public static implicit operator bool?(QueryParameter q)
        {
            return q.element.ValueKind == JsonValueKind.Null ? null : q.element.GetBoolean();
        }

        public static implicit operator string?(QueryParameter q)
        {
            return q.element.ValueKind == JsonValueKind.Null ? null : q.element.GetString();
        }

        public static implicit operator DateTime(QueryParameter q)
        {
            return DateTime.Parse(q.element.GetString()!, null, System.Globalization.DateTimeStyles.AdjustToUniversal);
        }

        public static implicit operator DateTimeOffset(QueryParameter q)
        {
            return DateTimeOffset.Parse(q.element.GetString()!, null, System.Globalization.DateTimeStyles.AdjustToUniversal);
        }


        public static implicit operator DateTime?(QueryParameter q)
        {
            JsonElement element = q.element;
            return element.ValueKind == JsonValueKind.Null ? null : global::System.DateTime.Parse(element.GetString()!, null, global::System.Globalization.DateTimeStyles.AdjustToUniversal);
        }

        public static implicit operator DateTimeOffset?(QueryParameter q)
        {
            JsonElement element = q.element;
            return element.ValueKind == JsonValueKind.Null ? null : global::System.DateTimeOffset.Parse(element.GetString()!, null, global::System.Globalization.DateTimeStyles.AdjustToUniversal);
        }

        private static WKTReader? wktReader;

        public static implicit operator Geometry?(QueryParameter q)
        {
            wktReader ??= new WKTReader(GeometryFactory.Default);
            JsonElement element = q.element;
            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    return wktReader.Read(element.GetString());
                case JsonValueKind.Array:
                    return new Point(element[0].GetDouble(), element[1].GetDouble()) { SRID = 4326 };
                case JsonValueKind.Object:
                    int srid = 4326;
                    if(element.TryGetProperty("srid", out var sridValue)) {
                        if(!sridValue.TryGetInt32(out srid))
                        {
                            srid = 4326;
                        }
                    }
                    if (element.TryGetProperty("x", out var x))
                    {
                        var y = element.GetProperty("y");
                        return new Point(x.GetDouble(), y.GetDouble()) { SRID = srid };
                    }
                    return new Point(element.GetProperty("longitude").GetDouble(), element.GetProperty("latitude").GetDouble())
                    {
                        SRID = srid
                    };
            }
            return null;
        }
    }
}
