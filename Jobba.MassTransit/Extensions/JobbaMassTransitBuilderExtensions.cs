using System;
using Jobba.Core.Builders;
using Jobba.Core.Events;
using Jobba.Core.Extensions;
using Jobba.Core.Interfaces;
using Jobba.MassTransit.HostedServices;
using Jobba.MassTransit.Implementations;
using Jobba.MassTransit.Implementations.Consumers;
using Jobba.MassTransit.Interfaces;
using Jobba.MassTransit.Models;
using MassTransit.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Jobba.MassTransit.Extensions;

public static class JobbaMassTransitBuilderExtensions
{
    private static readonly JobbaMassTransitConfigurationContext ConfigurationContext = new();

    internal static readonly Type[] OpenConsumerTypes =
    [
        typeof(OnJobCancelConsumer<,,>),
        typeof(OnJobCancelledConsumer<,,>),
        typeof(OnJobCompleteConsumer<,,>),
        typeof(OnJobFaultedConsumer<,,>),
        typeof(OnJobProgressConsumer<,,>),
        typeof(OnJobRestartConsumer<,,>),
        typeof(OnJobStartedConsumer<,,>),
        typeof(OnJobWatchConsumer<,,>),
    ];

    public static JobbaBuilder UsingMassTransit(this JobbaBuilder builder)
    {
        builder.Services.TryAddScoped<IJobbaMassTransitConsumerInfoProvider, JobbaMassTransitConsumerInfoProvider>();
        builder.Services.RegisterReplace<IJobEventPublisher, MassTransitJobEventPublisher>();
        builder.Services.TryAddSingleton<ICancelRequestClientResolver, CancelRequestClientResolver>();

        foreach (var openType in OpenConsumerTypes)
        {
            builder.Services.AddSingleton(new JobbaMassTransitOpenConsumerRegistration { OpenConsumerType = openType });
        }

        builder.Services.RegisterReplace(ConfigurationContext);

        builder.AddRegistrar(new MassTransitJobTypeRegistrar());

        builder.Services.AddSingleton<MassTransitJobbaReceiverHostedService>();
        builder.Services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<MassTransitJobbaReceiverHostedService>());
        builder.Services.AddSingleton<IJobbaReadyGate>(sp => sp.GetRequiredService<MassTransitJobbaReceiverHostedService>());

        return builder;
    }
}

internal class MassTransitJobTypeRegistrar : IJobTypeRegistrar
{
    public void OnJobAdded<TJob, TJobParams, TJobState>(JobbaBuilder builder)
        where TJob : class, IJob<TJobParams, TJobState>
        where TJobParams : IJobParams
        where TJobState : IJobState
    {
        foreach (var openType in JobbaMassTransitBuilderExtensions.OpenConsumerTypes)
        {
            var closedType = openType.MakeGenericType(typeof(TJob), typeof(TJobParams), typeof(TJobState));
            builder.Services.TryAddScoped(closedType);
        }

        IContainerRegistrar registrar = new DependencyInjectionContainerRegistrar(builder.Services);
        registrar.RegisterRequestClient<CancelJobEvent<TJob>>();
    }
}
