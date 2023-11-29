using System;
using Jobba.Core.Builders;
using Jobba.Core.Interfaces;
using Jobba.Core.Models;
using Jobba.Core.Models.Entities;
using Jobba.Store.Mongo.Builders;
using Jobba.Store.Mongo.Serializers;
using MongoDB.Bson.Serialization;

namespace Jobba.Store.Mongo.Extensions;

public static class MongoJobbaBuilderExtensions
{
    public static JobbaBuilder UsingMongo(this JobbaBuilder jobbaBuilder,
        string connectionString,
        bool enableCommandLogging,
        Action<JobbaMongoBuilder> configure = null)
    {
        RegisterSerializers();

        jobbaBuilder.OnJobAdded += OnJobAdded;
        var jobbaMongoBuilder = new JobbaMongoBuilder(jobbaBuilder, connectionString, enableCommandLogging);
        configure?.Invoke(jobbaMongoBuilder);
        return jobbaBuilder;
    }

    private static void RegisterSerializers()
    {
        RegisterBsonTypes(typeof(JobStatus));

        var typeSerializer = new TypeSerializer();

        BsonClassMap.TryRegisterClassMap<DefaultJobState>(cm => cm.AutoMap());

        BsonClassMap.TryRegisterClassMap<DefaultJobParams>(cm => cm.AutoMap());

        BsonClassMap.RegisterClassMap<JobInfoBase>(map =>
        {
            map.AutoMap();
            map.MapProperty(x => x.Status);
        });

        BsonClassMap.RegisterClassMap<JobProgressEntity>(map =>
        {
            map.AutoMap();
            map.MapProperty(x => x.JobState);
        });

        BsonClassMap.RegisterClassMap<JobEntity>(map =>
        {
            map.AutoMap();
            map.MapProperty(x => x.Status);
        });

        BsonClassMap.RegisterClassMap<JobRegistration>(map =>
        {
            map.AutoMap();
            map.MapProperty(x => x.JobType).SetSerializer(typeSerializer);
            map.MapProperty(x => x.JobStateType).SetSerializer(typeSerializer);
            map.MapProperty(x => x.JobParamsType).SetSerializer(typeSerializer);
        });
    }

    private static void OnJobAdded(JobAddedEventArgs obj)
        => RegisterBsonTypes(
            obj.JobType,
            obj.JobParamsType,
            obj.JobStateType,
            typeof(JobInfo<,>).MakeGenericType(obj.JobParamsType, obj.JobStateType),
            typeof(JobProgress<>).MakeGenericType(obj.JobStateType),
            typeof(JobRequest<,>).MakeGenericType(obj.JobParamsType, obj.JobStateType));

    private static void RegisterBsonTypes(params Type[] types)
    {
        foreach (var type in types)
        {
            BsonClassMap.LookupClassMap(type);
        }
    }
}
