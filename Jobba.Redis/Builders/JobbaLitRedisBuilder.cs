using System;
using Jobba.Core.Interfaces;
using Jobba.Redis.Implementations;
using LitRedis.Core;
using LitRedis.Core.Builders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Jobba.Redis.Builders
{
    public class JobbaLitRedisBuilder
    {
        public IServiceCollection Services { get; }

        public JobbaLitRedisBuilder(IServiceCollection services, Action<LitRedisServiceCollectionBuilder> litRedisBuilder = null)
        {
            Services = services;
            services.TryAddScoped<IJobLockService, LitRedisJobLockService>();

            if (litRedisBuilder != null)
            {
                services.AddLitRedis(litRedisBuilder);
            }
        }
    }
}
