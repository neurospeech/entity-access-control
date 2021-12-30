using System;
using System.Text.Json;

namespace NeuroSpeech.EntityAccessControl
{
    public readonly struct QueryParameter
    {
        private readonly JsonElement element;

        public QueryParameter(System.Text.Json.JsonElement element)
        {
            this.element = element;
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

        public static implicit operator DateTime?(QueryParameter q)
        {
            return q.element.ValueKind == JsonValueKind.Null ? null : DateTime.Parse(q.element.GetString()!);
        }

        public static implicit operator DateTimeOffset?(QueryParameter q)
        {
            return q.element.ValueKind == JsonValueKind.Null ? null : DateTimeOffset.Parse(q.element.GetString()!);
        }

    }
}
