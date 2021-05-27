using System;
using Jobba.Core.Builders;
using Jobba.Store.Mongo.Builders;

namespace Jobba.Store.Mongo.Extensions
{
    public static class MongoJobbaBuilderExtensions
    {
        public static JobbaBuilder UsingMongo(this JobbaBuilder jobbaBuilder,
            string connectionString,
            bool enableCommandLogging,
            Action<JobbaMongoBuilder> configure = null)
        {
            var jobbaMongoBuilder = new JobbaMongoBuilder(jobbaBuilder, connectionString, enableCommandLogging);
            configure?.Invoke(jobbaMongoBuilder);
            return jobbaBuilder;
        }
    }
}
