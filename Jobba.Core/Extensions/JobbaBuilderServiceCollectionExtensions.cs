using System;
using Jobba.Core.Builders;
using Microsoft.Extensions.DependencyInjection;

namespace Jobba.Core.Extensions;

public static class JobbaBuilderServiceCollectionExtensions
{
    public static IServiceCollection AddJobba(this IServiceCollection services, Action<JobbaBuilder> configure)
    {
        var jobbaBuilder = new JobbaBuilder(services);
        configure(jobbaBuilder);
        return services;
    }
}
