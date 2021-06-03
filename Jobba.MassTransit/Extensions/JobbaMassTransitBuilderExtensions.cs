using Jobba.Core.Builders;
using Jobba.Core.Interfaces;
using Jobba.Core.Extensions;
using Jobba.MassTransit.HostedServices;
using Jobba.MassTransit.Implementations;
using Jobba.MassTransit.Interfaces;
using Jobba.MassTransit.Models;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Jobba.MassTransit.Extensions
{
    //todo: test
    public static class JobbaMassTransitBuilderExtensions
    {
        private static JobbaMassTransitConfigurationContext _configurationContext = new();

        public static JobbaBuilder UsingMassTransit(this JobbaBuilder builder)
        {
            builder.Services.AddHostedService<MassTransitJobbaReceiverHostedService>();

            builder.Services.TryAddScoped<IJobbaMassTransitConsumerInfoProvider, JobbaMassTransitConsumerInfoProvider>();

            builder.Services.RegisterReplace<IJobEventPublisher, MassTransitJobEventPublisher>();

            RegisterConsumer<OnJobCancelConsumer>(builder);
            RegisterConsumer<OnJobRestartConsumer>(builder);
            RegisterConsumer<OnJobWatchConsumer>(builder);

            builder.Services.RegisterReplace(_configurationContext);

            return builder;
        }

        private static void RegisterConsumer<TConsumer>(JobbaBuilder builder)
            where TConsumer : class, IJobbaMassTransitConsumer, IConsumer
        {
            builder.Services.TryAddScoped<IJobbaMassTransitConsumer, TConsumer>();
            builder.Services.TryAddScoped<TConsumer>();
        }
    }
}
