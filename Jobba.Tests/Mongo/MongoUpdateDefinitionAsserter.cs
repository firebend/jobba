using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Jobba.Tests.Mongo;

public static class Ext
{
    public static string ToCamelCase(this string input)
        => char.ToLowerInvariant(input[0]) + input[1..];

    public static BsonValue SerializeToBsonValue<T>(this T value)
        => BsonSerializer.SerializerRegistry.GetSerializer<T>().ToBsonValue(value);
}

public class MongoUpdateDefinitionAsserter<T>
{
    private readonly UpdateDefinition<T> _updateDefinition;
    private string _json;
    private BsonDocument _setDoc;

    public string Json => _json ??= _updateDefinition.Render(
            BsonSerializer.SerializerRegistry.GetSerializer<T>(),
            BsonSerializer.SerializerRegistry)
        .ToBsonDocument()
        .ToString();

    public BsonDocument SetDoc => _setDoc ??= BsonDocument.Parse(Json)["$set"].AsBsonDocument;

    public MongoUpdateDefinitionAsserter(UpdateDefinition<T> updateDefinition)
    {
        _updateDefinition = updateDefinition;
    }

    public bool ShouldSetField(string field)
    {
        var fieldName = field.ToCamelCase();
        var shouldGet = SetDoc.TryGetValue(fieldName, out var value);
        return shouldGet && value is not null;
    }

    public bool ShouldSetFieldWithValue(string field, BsonValue expectedValue)
    {
        var fieldName = field.ToCamelCase();
        var shouldGet = SetDoc.TryGetValue(fieldName, out var value);
        return shouldGet && value is not null && expectedValue == value;
    }

    public bool ShouldSetFieldsWithValues(Dictionary<string, BsonValue> expectedValues)
    {
        foreach (var (key, bsonValue) in expectedValues)
        {
            if (ShouldSetFieldWithValue(key, bsonValue) is false)
            {
                return false;
            }
        }

        return true;
    }

    public bool ShouldSetFields(params string[] fields)
        => fields.All(field => ShouldSetField(field));
}
