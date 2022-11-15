using Jobba.Core.Builders;
using Jobba.Core.Events;
using Jobba.Core.Extensions;
using Jobba.Core.Interfaces;
using Jobba.MassTransit.HostedServices;
using Jobba.MassTransit.Implementations;
using Jobba.MassTransit.Implementations.Consumers;
using Jobba.MassTransit.Interfaces;
using Jobba.MassTransit.Models;
using MassTransit;
using MassTransit.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Jobba.MassTransit.Extensions
{
    public static class JobbaMassTransitBuilderExtensions
    {
        private static readonly JobbaMassTransitConfigurationContext ConfigurationContext = new();

        public static JobbaBuilder UsingMassTransit(this JobbaBuilder builder)
        {
            builder.Services.TryAddScoped<IJobbaMassTransitConsumerInfoProvider, JobbaMassTransitConsumerInfoProvider>();

            builder.Services.RegisterReplace<IJobEventPublisher, MassTransitJobEventPublisher>();

            RegisterConsumer<OnJobCancelConsumer>(builder);
            RegisterConsumer<OnJobCancelledConsumer>(builder);
            RegisterConsumer<OnJobCompleteConsumer>(builder);
            RegisterConsumer<OnJobFaultedConsumer>(builder);
            RegisterConsumer<OnJobProgressConsumer>(builder);
            RegisterConsumer<OnJobRestartConsumer>(builder);
            RegisterConsumer<OnJobStartedConsumer>(builder);
            RegisterConsumer<OnJobWatchConsumer>(builder);

            builder.Services.RegisterReplace(ConfigurationContext);

            IContainerRegistrar registrar = new DependencyInjectionContainerRegistrar(builder.Services);
            registrar.RegisterRequestClient<CancelJobEvent>();

            builder.Services.AddHostedService<MassTransitJobbaReceiverHostedService>();

            return builder;
        }

        private static void RegisterConsumer<TConsumer>(JobbaBuilder builder)
            where TConsumer : class, IJobbaMassTransitConsumer, IConsumer
        {
            builder.Services.AddScoped<IJobbaMassTransitConsumer, TConsumer>();
            builder.Services.TryAddScoped<TConsumer>();
        }
    }
}
