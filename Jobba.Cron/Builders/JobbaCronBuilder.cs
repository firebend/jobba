using System;
using Cronos;
using Jobba.Core.Builders;
using Jobba.Core.Interfaces;
using Jobba.Core.Models;
using Jobba.Cron.HostedServices;
using Jobba.Cron.Implementations;
using Jobba.Cron.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Jobba.Cron.Builders;

public class JobbaCronBuilder
{
    public JobbaBuilder Builder { get; init; }

    public JobbaCronBuilder(JobbaBuilder builder)
    {
        Builder = builder;

        RegisterRequiredServices();
    }

    private void RegisterRequiredServices()
    {
        Builder.Services.AddHostedService<JobbaCronHostedService>();

        Builder.Services.TryAddTransient<ICronScheduler, CronScheduler>();
        Builder.Services.TryAddTransient<ICronService, CronService>();
    }

    /// <summary>
    /// Adds a cron job.
    /// </summary>
    /// <param name="builder">
    /// The builder.
    /// </param>
    /// <param name="cron">
    /// The cron expression.
    /// </param>
    /// <param name="timeZone">
    /// The time zone the cron should run in.
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
    public JobbaCronBuilder AddCronJob<TJob, TJobParams, TJobState>(string cron,
        string jobName,
        string description = null,
        TimeZoneInfo timeZone = null,
        Action<JobRegistration> configureRegistration = null)
        where TJob : class, ICronJob<TJobParams, TJobState>
        where TJobState : class, IJobState, new()
        where TJobParams : class, IJobParams, new()
    {
        //********************************************
        // Author: JMA
        // Date: 2023-07-19 10:13:54
        // Comment: Attempt to parse the cron expression.
        // invalid crons will throw an exception
        //*******************************************
        CronExpression.Parse(cron, CronFormat.Standard);

        timeZone ??= TimeZoneInfo.Utc;

        Builder.AddJob<TJob, TJobParams, TJobState>(jobName,
            description,
            reg =>
            {
                reg.CronExpression = cron;
                reg.TimeZoneId = timeZone.Id;
                configureRegistration?.Invoke(reg);
            });

        return this;
    }
}
