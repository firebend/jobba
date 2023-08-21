using System;
using Jobba.Core.Builders;
using Microsoft.Extensions.DependencyInjection;

namespace Jobba.Core.Extensions;

public static class JobbaBuilderServiceCollectionExtensions
{
    public static IServiceCollection AddJobba(this IServiceCollection services, Action<JobbaBuilder> configure = null)
    {
        var jobbaBuilder = new JobbaBuilder(services);
        configure?.Invoke(jobbaBuilder);
        return services;
    }

    public static JobbaBuilder UsingInMemory(this JobbaBuilder jobbaBuilder, Action<JobbaInMemoryBuilder> configure = null)
    {
        var inMemory = new JobbaInMemoryBuilder(jobbaBuilder);
        configure?.Invoke(inMemory);
        return jobbaBuilder;
    }
}
