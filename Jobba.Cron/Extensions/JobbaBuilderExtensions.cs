using System;
using Jobba.Core.Builders;
using Jobba.Core.Interfaces;
using Jobba.Cron.Builders;
using Jobba.Cron.Implementations;
using Microsoft.Extensions.DependencyInjection;

namespace Jobba.Cron.Extensions;

public static class JobbaBuilderExtensions
{
    /// <summary>
    /// Adds cron capabilities to Jobba
    /// </summary>
    /// <param name="builder">
    /// The Jobba Builder
    /// </param>
    /// <param name="configure">
    /// A callback to configure the cron builder.
    /// </param>
    /// <returns>
    /// The Jobba Builder
    /// </returns>
    public static JobbaBuilder UsingCron(this JobbaBuilder builder, Action<JobbaCronBuilder> configure = null)
    {
        var cronRegistry = new CronJobTypeRegistry();
        builder.Services.AddSingleton(cronRegistry);
        builder.AddRegistrar(new CronJobTypeRegistrar(cronRegistry));

        var cronBuilder = new JobbaCronBuilder(builder);
        configure?.Invoke(cronBuilder);

        return builder;
    }
}

internal class CronJobTypeRegistrar : IJobTypeRegistrar
{
    private readonly CronJobTypeRegistry _registry;

    public CronJobTypeRegistrar(CronJobTypeRegistry registry)
    {
        _registry = registry;
    }

    public void OnJobAdded<TJob, TJobParams, TJobState>(JobbaBuilder builder)
        where TJob : class, IJob<TJobParams, TJobState>
        where TJobParams : IJobParams
        where TJobState : IJobState
    {
        _registry.RegisterJobType<TJob, TJobParams, TJobState>();
    }
}
