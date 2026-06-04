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

        jobbaBuilder.AddRegistrar(new MongoJobTypeRegistrar());
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

    internal static void RegisterBsonTypes(params Type[] types)
    {
        foreach (var type in types)
        {
            BsonClassMap.LookupClassMap(type);
        }
    }
}

internal class MongoJobTypeRegistrar : IJobTypeRegistrar
{
    public void OnJobAdded<TJob, TJobParams, TJobState>(JobbaBuilder builder)
        where TJob : class, IJob<TJobParams, TJobState>
        where TJobParams : IJobParams
        where TJobState : IJobState
    {
        MongoJobbaBuilderExtensions.RegisterBsonTypes(
            typeof(TJob),
            typeof(TJobParams),
            typeof(TJobState),
            typeof(JobInfo<TJobParams, TJobState>),
            typeof(JobProgress<TJobState>),
            typeof(JobRequest<TJobParams, TJobState>));
    }
}
