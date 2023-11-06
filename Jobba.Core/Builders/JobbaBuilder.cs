using System;
using Jobba.Core.HostedServices;
using Jobba.Core.Implementations;
using Jobba.Core.Interfaces;
using Jobba.Core.Interfaces.Subscribers;
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
    public JobbaBuilder(IServiceCollection services, IJobRegistrationStore registrationStore = null)
    {
        JobRegistrationStore = registrationStore ?? DefaultJobRegistrationStore.Instance;
        Services = services;
        AddDefaultServices();
    }

    public IServiceCollection Services { get; }

    public Action<JobAddedEventArgs> OnJobAdded { get; set; }

    public IJobRegistrationStore JobRegistrationStore { get; }

    private void AddDefaultServices()
    {
        Services.TryAddScoped<IJobbaGuidGenerator, DefaultJobbaGuidGenerator>();
        Services.TryAddScoped<IJobCancellationTokenStore, DefaultJobCancellationTokenStore>();
        Services.TryAddTransient<IJobEventPublisher, DefaultJobEventPublisher>();
        Services.TryAddScoped<IJobLockService, DefaultJobLockService>();
        Services.TryAddScoped<IJobReScheduler, DefaultJobReScheduler>();
        Services.TryAddScoped<IJobScheduler, DefaultJobScheduler>();
        Services.TryAddScoped<IJobReScheduler, DefaultJobReScheduler>();
        Services.TryAddScoped<IOnJobCancelSubscriber, DefaultOnJobCancelSubscriber>();
        Services.TryAddScoped<IOnJobRestartSubscriber, DefaultOnJobRestartSubscriber>();
        Services.TryAddScoped<IOnJobWatchSubscriber, DefaultOnJobWatchSubscriber>();
        Services.TryAddSingleton(JobRegistrationStore);

        Services.AddHostedService<JobbaHostedService>();
        Services.AddHostedService<JobbaCleanUpHostedService>();
    }

    public JobbaBuilder AddJob<TJob, TJobParams, TJobState>()
        where TJob : class, IJob<TJobParams, TJobState>
    {
        Services.TryAddScoped<IJobWatcher<TJobParams, TJobState>, DefaultJobWatcher<TJobParams, TJobState>>();
        Services.TryAddScoped<IJob<TJobParams, TJobState>, TJob>();
        Services.TryAddScoped<TJob>();

        OnJobAdded?.Invoke(new JobAddedEventArgs
        {
            JobType = typeof(TJob),
            JobStateType = typeof(TJobState),
            JobParamsType = typeof(TJobParams)
        });

        return this;
    }
}
