using System;
using Jobba.Core.Extensions;
using Jobba.Core.Interfaces;
using Jobba.Redis.Implementations;
using LitRedis.Core;
using LitRedis.Core.Builders;
using Microsoft.Extensions.DependencyInjection;

namespace Jobba.Redis.Builders;

public class JobbaLitRedisBuilder
{
    public JobbaLitRedisBuilder(IServiceCollection services, Action<LitRedisServiceCollectionBuilder> litRedisBuilder = null)
    {
        Services = services;

        services.RegisterReplace<IJobLockService, LitRedisJobLockService>();

        if (litRedisBuilder != null)
        {
            services.AddLitRedis(litRedisBuilder);
        }
    }

    public IServiceCollection Services { get; }
}
