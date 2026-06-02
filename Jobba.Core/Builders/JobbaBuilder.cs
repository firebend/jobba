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

public class JobbaBuilder
{
    private readonly string _systemMoniker;

    public JobbaBuilder(IServiceCollection services, string systemMoniker)
    {
        _systemMoniker = systemMoniker;
        Services = services;
        Registry = new JobTypeRegistry();
        Services.AddSingleton(Registry);
        AddDefaultServices();
    }

    public IServiceCollection Services { get; }

    public JobTypeRegistry Registry { get; }

    private readonly List<IJobTypeRegistrar> _registrars = [];

    public void AddRegistrar(IJobTypeRegistrar registrar)
    {
        _registrars.Add(registrar);

        foreach (var registration in Registrations.Values)
        {
            ApplyRegistrar(registrar, registration);
        }
    }

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
        Services.TryAddTransient<IJobEventDispatcher, JobEventDispatcher>();

        Services.TryAddScoped<IJobOrchestrationService, DefaultJobOrchestrationService>();
        Services.TryAddSingleton<IJobSystemInfoProvider>(new DefaultJobSystemInfoProvider(_systemMoniker));

        Services.AddOptions<JobbaCoreOptions>();

        Services.AddHostedService<JobbaHostedService>();
        Services.AddHostedService<JobbaCleanUpHostedService>();
    }

    /// <summary>
    /// Configures JobbaCoreOptions for the jobba builder.
    /// </summary>
    /// <param name="configure">Configuration action to customize JobbaCoreOptions.</param>
    /// <returns>The JobbaBuilder instance.</returns>
    public JobbaBuilder ConfigureOptions(Action<JobbaCoreOptions> configure)
    {
        Services.Configure(configure);
        return this;
    }

    /// <summary>
    /// Registers an additional <see cref="IJobbaReadyGate"/>. All registered gates are awaited in parallel before
    /// Jobba performs startup work that depends on infrastructure being ready (e.g. restarting faulted jobs).
    /// </summary>
    public JobbaBuilder AddReadyGate<TGate>() where TGate : class, IJobbaReadyGate
    {
        Services.TryAddSingleton<TGate>();
        Services.AddSingleton<IJobbaReadyGate>(sp => sp.GetRequiredService<TGate>());
        return this;
    }

    public JobbaBuilder AddJob<TJob, TJobParams, TJobState>(string name,
        string description = null,
        Action<JobRegistration> configureRegistration = null)
        where TJob : class, IJob<TJobParams, TJobState>
        where TJobParams : IJobParams
        where TJobState : IJobState
    {
        Services.TryAddScoped<IJobWatcher<TJob, TJobParams, TJobState>, DefaultJobWatcher<TJob, TJobParams, TJobState>>();
        Services.TryAddScoped<IOnJobCancelSubscriber<TJob, TJobParams, TJobState>, DefaultOnJobCancelSubscriber<TJob, TJobParams, TJobState>>();
        Services.TryAddScoped<IOnJobRestartSubscriber<TJob, TJobParams, TJobState>, DefaultOnJobRestartSubscriber<TJob, TJobParams, TJobState>>();
        Services.TryAddScoped<IOnJobWatchSubscriber<TJob, TJobParams, TJobState>, DefaultOnJobWatchSubscriber<TJob, TJobParams, TJobState>>();

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
            JobStateType = typeof(TJobState),
            SystemMoniker = _systemMoniker
        };

        configureRegistration?.Invoke(registration);

        Registrations.Add(name, registration);

        Services.AddSingleton(registration);

        Registry.RegisterJobType<TJob, TJobParams, TJobState>();

        foreach (var registrar in _registrars)
        {
            registrar.OnJobAdded<TJob, TJobParams, TJobState>(this);
        }

        return this;
    }

    private void ApplyRegistrar(IJobTypeRegistrar registrar, JobRegistration registration)
    {
        var method = GetType().GetMethod(nameof(ApplyRegistrarGeneric), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var genericMethod = method?.MakeGenericMethod(registration.JobType, registration.JobParamsType, registration.JobStateType);
        _ = (genericMethod?.Invoke(this, [registrar]));
    }

    private void ApplyRegistrarGeneric<TJob, TJobParams, TJobState>(IJobTypeRegistrar registrar)
        where TJob : class, IJob<TJobParams, TJobState>
        where TJobParams : IJobParams
        where TJobState : IJobState
        => registrar.OnJobAdded<TJob, TJobParams, TJobState>(this);
}
