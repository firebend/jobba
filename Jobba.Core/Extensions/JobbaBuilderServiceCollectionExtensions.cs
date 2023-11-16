using System;
using Jobba.Core.Builders;
using Microsoft.Extensions.DependencyInjection;

namespace Jobba.Core.Extensions;

public static class JobbaBuilderServiceCollectionExtensions
{
    /// <summary>
    /// Adds jobba services to the service collection
    /// </summary>
    /// <param name="services">
    /// The service collection to add jobba services to.
    /// </param>
    /// <param name="systemMoniker">
    /// The system moniker to use to identify this jobba workload. Defaults to jobba
    /// </param>
    /// <param name="configure">
    /// An optional configuration action to configure jobba services.
    /// </param>
    /// <returns>
    /// The service collection with jobba services added.
    /// </returns>
    public static IServiceCollection AddJobba(this IServiceCollection services,
        string systemMoniker = "jobba",
        Action<JobbaBuilder> configure = null)
    {
        var jobbaBuilder = new JobbaBuilder(services, systemMoniker);
        configure?.Invoke(jobbaBuilder);
        return services;
    }


    /// <summary>
    /// Registers an in memory provider for all the necessary jobba stores
    /// </summary>
    /// <param name="jobbaBuilder">
    /// The jobba builder to register the in memory provider with.
    /// </param>
    /// <param name="configure">
    /// An optional configuration action to configure the in memory provider.
    /// </param>
    /// <returns></returns>
    public static JobbaBuilder UsingInMemory(this JobbaBuilder jobbaBuilder, Action<JobbaInMemoryBuilder> configure = null)
    {
        var inMemory = new JobbaInMemoryBuilder(jobbaBuilder);
        configure?.Invoke(inMemory);
        return jobbaBuilder;
    }
}
