using NetTopologySuite.Geometries;
using NeuroSpeech.EntityAccessControl.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace NeuroSpeech.EntityAccessControl
{
    //public class EntityJsonConverterFactory : JsonConverterFactory
    //{
    //    private readonly EntitySerializationSettings settings;

    //    public EntityJsonConverterFactory(EntitySerializationSettings settings)
    //    {
    //        this.settings = settings;
    //    }

    //    public override bool CanConvert(Type typeToConvert)
    //    {
    //        if (typeToConvert.IsValueType)
    //            return false;
    //        if (typeToConvert == typeof(string))
    //            return false;
    //        if (typeof(System.Collections.IEnumerable).IsAssignableFrom(typeToConvert))
    //            return false;
    //        return true;
    //    }

    //    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    //    {
    //        return settings.Converter;
    //    }

    //}

}
