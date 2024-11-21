using System;
using System.Collections.Generic;
using Jobba.Core.HostedServices;
using Jobba.Core.Implementations;
using Jobba.Core.Interfaces;
using Jobba.Core.Interfaces.Subscribers;
using Jobba.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Jobba.Core.Builders;

public record JobAddedEventArgs
{
    public Type JobType { get; set; }
    public Type JobParamsType { get; set; }
    public Type JobStateType { get; set; }
}

public class JobbaBuilder
{
    private readonly string _systemMoniker;

    public JobbaBuilder(IServiceCollection services, string systemMoniker)
    {
        _systemMoniker = systemMoniker;
        Services = services;
        AddDefaultServices();
    }

    public IServiceCollection Services { get; }

    public Action<JobAddedEventArgs> OnJobAdded { get; set; }

    public Dictionary<string, JobRegistration> Registrations { get; } = new();

    private void AddDefaultServices()
    {
        Services.TryAddScoped<IJobbaGuidGenerator, DefaultJobbaGuidGenerator>();
        Services.TryAddScoped<IJobCancellationTokenStore, DefaultJobCancellationTokenStore>();
        Services.TryAddTransient<IJobEventPublisher, DefaultJobEventPublisher>();
        Services.TryAddScoped<IJobLockService, DefaultJobLockService>();
        Services.TryAddScoped<IJobScheduler, DefaultJobScheduler>();
        Services.TryAddScoped<IJobRunner, DefaultJobRunner>();
        Services.TryAddScoped<IJobReScheduler, DefaultJobReScheduler>();
        Services.TryAddScoped<IOnJobCancelSubscriber, DefaultOnJobCancelSubscriber>();
        Services.TryAddScoped<IOnJobRestartSubscriber, DefaultOnJobRestartSubscriber>();
        Services.TryAddScoped<IOnJobWatchSubscriber, DefaultOnJobWatchSubscriber>();
        Services.TryAddScoped<IJobOrchestrationService, DefaultJobOrchestrationService>();
        Services.TryAddSingleton<IJobSystemInfoProvider>(new DefaultJobSystemInfoProvider(_systemMoniker));

        Services.AddHostedService<JobbaHostedService>();
        Services.AddHostedService<JobbaCleanUpHostedService>();
    }

    public JobbaBuilder AddJob<TJob, TJobParams, TJobState>(string name,
        string description = null,
        Action<JobRegistration> configureRegistration = null)
        where TJob : class, IJob<TJobParams, TJobState>
        where TJobParams : IJobParams
        where TJobState : IJobState
    {
        Services.TryAddScoped<IJobWatcher<TJobParams, TJobState>, DefaultJobWatcher<TJobParams, TJobState>>();

        if (Registrations.ContainsKey(name))
        {
            throw new Exception($"Job {name} is already registered");
        }

        var registration = new JobRegistration
        {
            JobName = name,
            Description = description,
            JobType = typeof(TJob),
            JobParamsType = typeof(TJobParams),
            JobStateType = typeof(TJobState)
        };

        configureRegistration?.Invoke(registration);

        Registrations.Add(name, registration);

        Services.AddSingleton(registration);

        OnJobAdded?.Invoke(new JobAddedEventArgs
        {
            JobType = typeof(TJob),
            JobStateType = typeof(TJobState),
            JobParamsType = typeof(TJobParams)
        });

        return this;
    }
}
