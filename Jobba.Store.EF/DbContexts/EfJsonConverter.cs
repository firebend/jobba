using System;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Jobba.Store.EF.DbContexts;

public class EfJsonConverter<T> : ValueConverter<T, string>
{
    public EfJsonConverter(JsonSerializerSettings? serializerSettings = null,
        ConverterMappingHints? mappingHints = null) :
        base(arg => ToJson(arg, serializerSettings),
            json => FromJson(json), mappingHints)
    {
    }

    public static string ToJson(T? instance, JsonSerializerSettings? settings) => JsonConvert.SerializeObject(instance, settings);

    public static T FromJson(string json)
    {
        var jObject = JObject.Parse(json);
        var typeStr = jObject.GetValue("$type")?.Value<string>() ?? throw new InvalidOperationException("Invalid JSON format.");

        var type = Type.GetType(typeStr) ?? throw new InvalidOperationException("Invalid type.");

        return (T)JsonConvert.DeserializeObject(json, type)!;
    }
}
