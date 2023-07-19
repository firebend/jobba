using System;
using System.Reflection;
using Jobba.Core.Interfaces;
using Jobba.Store.Mongo.Implementations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;

namespace Jobba.Store.Mongo;

public static class JobbaMongoDbConfigurator
{
    private static bool _configured;
    private static readonly object Key = new();

    public static void Configure()
    {
        if (_configured)
        {
            return;
        }

        lock (Key)
        {
            if (_configured)
            {
                return;
            }

            _configured = true;

            BsonSerializer.RegisterSerializer(typeof(Guid), new GuidSerializer(BsonType.String));
            BsonSerializer.RegisterSerializer(typeof(decimal), new DecimalSerializer(BsonType.Decimal128));
            BsonSerializer.RegisterSerializer(typeof(decimal?), new NullableSerializer<decimal>(new DecimalSerializer(BsonType.Decimal128)));
            BsonSerializer.RegisterSerializer(typeof(DateTimeOffset), new DateTimeOffsetSerializer(BsonType.String));

            var pack = new ConventionPack
            {
                new CamelCaseElementNameConvention(),
                new EnumRepresentationConvention(BsonType.String),
                new IgnoreExtraElementsConvention(true)
            };

            var mongoEntityType = typeof(IJobbaEntity);
            const string mongoEntityIdName = "Id";

            pack.AddClassMapConvention("Jobba Mongo ID Guid Generator", map =>
            {
                if (map.ClassType.BaseType == null ||
                    map.ClassType.BaseType.IsInterface ||
                    map.ClassType.BaseType.GetProperty(mongoEntityIdName) == null)
                {
                    if (mongoEntityType.IsAssignableFrom(map.ClassType))
                    {
                        map.MapIdProperty(mongoEntityIdName)
                            .SetIdGenerator(JobbaMongoIdGenerator.Instance)
                            .SetSerializer(new GuidSerializer(BsonType.String));

                        map.SetIgnoreExtraElements(true);
                    }
                }
            });

            pack.AddMemberMapConvention("Jobba Ignore Default Values", m => m.SetIgnoreIfDefault(!m.MemberType.GetTypeInfo().IsEnum));

            ConventionRegistry.Register("Jobba Custom Conventions", pack, _ => true);
        }
    }
}
