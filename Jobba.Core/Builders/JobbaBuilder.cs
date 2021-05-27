using Jobba.Core.HostedServices;
using Jobba.Core.Implementations;
using Jobba.Core.Interfaces;
using Jobba.Core.Interfaces.Subscribers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Jobba.Core.Builders
{
    //todo: test
    public class JobbaBuilder
    {
        public IServiceCollection Services { get; }

        public JobbaBuilder(IServiceCollection services)
        {
            Services = services;
            AddDefaultServices();
        }

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

            Services.AddHostedService<JobbaHostedService>();
        }

        public JobbaBuilder AddJob<TJob, TJobParams, TJobState>()
            where TJob : class, IJob<TJobParams, TJobState>
        {
            Services.TryAddScoped<IJobWatcher<TJobParams, TJobState>, DefaultJobWatcher<TJobParams, TJobState>>();
            Services.TryAddScoped<IJob<TJobParams, TJobState>, TJob>();
            Services.TryAddScoped<TJob>();

            return this;
        }
    }
}
