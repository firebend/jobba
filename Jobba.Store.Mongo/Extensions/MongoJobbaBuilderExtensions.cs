using System;
using Jobba.Core.Builders;
using Jobba.Core.Models;
using Jobba.Core.Models.Entities;
using Jobba.Store.Mongo.Builders;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Jobba.Store.Mongo.Extensions
{
    public static class MongoJobbaBuilderExtensions
    {
        public static JobbaBuilder UsingMongo(this JobbaBuilder jobbaBuilder,
            string connectionString,
            bool enableCommandLogging,
            Action<JobbaMongoBuilder> configure = null)
        {
            RegisterBsonTypes(typeof(JobStatus));

            var anySerializer = new ObjectSerializer(_ => true);

            BsonClassMap.RegisterClassMap<JobInfoBase>(map =>
            {
                map.AutoMap();
                map.MapProperty(x => x.Status);
            });

            BsonClassMap.RegisterClassMap<JobProgressEntity>(map =>
            {
                map.AutoMap();
                map.MapProperty(x => x.JobState).SetSerializer(anySerializer);
            });

            BsonClassMap.RegisterClassMap<JobEntity>(map =>
            {
                map.AutoMap();
                map.MapProperty(x => x.JobState).SetSerializer(anySerializer);
                map.MapProperty(x => x.JobParameters).SetSerializer(anySerializer);
                map.MapProperty(x => x.Status);
            });

            jobbaBuilder.OnJobAdded += OnJobAdded;
            var jobbaMongoBuilder = new JobbaMongoBuilder(jobbaBuilder, connectionString, enableCommandLogging);
            configure?.Invoke(jobbaMongoBuilder);
            return jobbaBuilder;
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
}
