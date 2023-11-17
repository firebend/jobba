using System;
using System.Collections.Concurrent;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization.Conventions;

namespace Jobba.Store.Mongo.Serializers;

public class JobbaDiscriminatorConvention : IDiscriminatorConvention
{
    public static ConcurrentDictionary<string, Type> TypeCache { get; } = new();

    public static JobbaDiscriminatorConvention Instance { get; } = new();

    public Type GetActualType(IBsonReader bsonReader, Type nominalType)
    {
        var bookmark = bsonReader.GetBookmark();
        bsonReader.ReadStartDocument();

        string discriminator = null;

        if (bsonReader.FindElement(ElementName))
        {
            discriminator = bsonReader.ReadString();
        }

        bsonReader.ReturnToBookmark(bookmark);

        return string.IsNullOrWhiteSpace(discriminator)
            ? nominalType
            : TypeCache.GetOrAdd(discriminator, ValueFactory, (discriminator, nominalType));
    }

    private static Type ValueFactory(string key, (string discriminator, Type nominalType) args)
    {
        var type = Type.GetType(args.discriminator) ?? AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(x => x.ExportedTypes)
            .FirstOrDefault(x => x.Name == args.discriminator);

        return type ?? throw new Exception($"Could not resolve type for descriptor {args.discriminator}");
    }

    public BsonValue GetDiscriminator(Type nominalType, Type actualType) => actualType.AssemblyQualifiedName;

    public string ElementName => "_t";
}
