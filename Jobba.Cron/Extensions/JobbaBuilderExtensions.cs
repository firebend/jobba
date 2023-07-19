using System;
using Cronos;
using Jobba.Core.Builders;
using Jobba.Cron.HostedServices;
using Jobba.Cron.Implementations;
using Jobba.Cron.Interfaces;
using Jobba.Cron.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Jobba.Cron.Extensions;

public static class JobbaBuilderExtensions
{
    /// <summary>
    /// Adds a cron job.
    /// </summary>
    /// <param name="builder">
    /// The builder.
    /// </param>
    /// <param name="cron">
    /// The cron expression.
    /// </param>
    /// <param name="jobName">
    /// The job's name.
    /// </param>
    /// <param name="description">
    /// The job's description.
    /// </param>
    /// <param name="configureRegistry">
    /// An optional callback to configure the registration for this job.
    /// </param>
    /// <param name="configureProvider">
    /// An optional callback to configure the state and parameters for the job.
    /// </param>
    /// <typeparam name="TJob">
    /// The job type.
    /// </typeparam>
    /// <typeparam name="TJobParams">
    /// The parameters type.
    /// </typeparam>
    /// <typeparam name="TJobState">
    /// The state type.
    /// </typeparam>
    /// <returns></returns>
    public static JobbaBuilder AddCronJob<TJob, TJobParams, TJobState>(this JobbaBuilder builder,
        string cron,
        string jobName,
        string description,
        Action<CronJobServiceRegistry> configureRegistry = null,
        Action<DefaultCronJobStateParamsProvider<TJobParams, TJobState>> configureProvider = null)
        where TJob : class, ICronJob<TJobParams, TJobState>
        where TJobState : class, new()
        where TJobParams : class, new()
    {
        //********************************************
        // Author: JMA
        // Date: 2023-07-19 10:13:54
        // Comment: Attempt to parse the cron expression.
        // invalid crons will throw an exception
        //*******************************************
        CronExpression.Parse(cron, CronFormat.Standard);

        var registry = new CronJobServiceRegistry
        {
            Cron = cron,
            JobName = jobName,
            Description = description,
            JobType = typeof(TJob),
            JobParamsType = typeof(TJobParams),
            JobStateType = typeof(TJobState)
        };

        configureRegistry?.Invoke(registry);

        builder.Services.AddSingleton(registry);

        var provider = new DefaultCronJobStateParamsProvider<TJobParams, TJobState>();
        configureProvider?.Invoke(provider);
        builder.Services.AddSingleton<ICronJobStateParamsProvider<TJobParams, TJobState>>(provider);

        builder.AddJob<TJob, TJobParams, TJobState>();

        builder.Services.TryAddTransient<ICronScheduler, CronScheduler>();
        builder.Services.TryAddTransient<ICronService, CronService>();

        builder.Services.AddHostedService<JobbaCronHostedService>();

        return builder;
    }
}
