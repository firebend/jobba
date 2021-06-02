using Jobba.Core.Builders;
using Jobba.Core.Interfaces;
using Jobba.MassTransit.HostedServices;
using Jobba.MassTransit.Implementations;
using Jobba.MassTransit.Interfaces;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Jobba.MassTransit.Extensions
{
    public static class JobbaMassTransitBuilderExtensions
    {
        public static JobbaBuilder UsingMassTransit(this JobbaBuilder builder)
        {
            builder.Services.AddHostedService<MassTransitJobbaReceiverHostedService>();

            builder.Services.TryAddScoped<IJobbaMassTransitConsumerInfoProvider, JobbaMassTransitConsumerInfoProvider>();

            ReplaceService<IJobEventPublisher, MassTransitJobEventPublisher>(builder);

            RegisterConsumer<OnJobCancelConsumer>(builder);
            RegisterConsumer<OnJobRestartConsumer>(builder);
            RegisterConsumer<OnJobWatchConsumer>(builder);

            return builder;
        }

        private static void RegisterConsumer<TConsumer>(JobbaBuilder builder)
            where TConsumer : class, IJobbaMassTransitConsumer, IConsumer
        {
            builder.Services.TryAddScoped<IJobbaMassTransitConsumer, TConsumer>();
            builder.Services.TryAddScoped<TConsumer>();
        }

        private static void ReplaceService<TServiceType, TImplementationType>(JobbaBuilder builder, ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            var serviceDescriptor = new ServiceDescriptor(typeof(TServiceType), typeof(TImplementationType), lifetime);
            builder.Services.Replace(serviceDescriptor);
        }
    }
}
