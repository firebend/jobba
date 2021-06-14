using System;
using Jobba.Core.Builders;
using Jobba.Redis.Builders;
using LitRedis.Core.Builders;

namespace Jobba.Redis
{
    //todo: test
    public static class LitRedisJobbaExtensions
    {
        public static JobbaBuilder UsingLitRedis(this JobbaBuilder builder, Action<LitRedisServiceCollectionBuilder> litRedisConfigure = null)
        {
            var _ = new JobbaLitRedisBuilder(builder.Services, litRedisConfigure);
            return builder;
        }

        public static JobbaBuilder UsingLitRedis(this JobbaBuilder builder, string connectionString)
        {
            var _ = new JobbaLitRedisBuilder(builder.Services, litRedis => litRedis
                .WithLocking()
                .WithConnectionString(connectionString));
            return builder;
        }
    }
}
