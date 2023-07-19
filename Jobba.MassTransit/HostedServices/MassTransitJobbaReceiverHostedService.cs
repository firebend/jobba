using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Jobba.MassTransit.Interfaces;
using Jobba.MassTransit.Models;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jobba.MassTransit.HostedServices;

public class MassTransitJobbaReceiverHostedService : BackgroundService
{
    private readonly ILogger<MassTransitJobbaReceiverHostedService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public MassTransitJobbaReceiverHostedService(
        ILogger<MassTransitJobbaReceiverHostedService> logger,
        IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var consumerInfoProvider = scope.ServiceProvider.GetService<IJobbaMassTransitConsumerInfoProvider>();
            var consumers = consumerInfoProvider?.GetConsumerInfos()?.ToList() ?? new List<JobbaMassTransitConsumerInfo>();

            if (consumers.Any())
            {
                RegisterJobbaEndpoints(scope, consumers);
            }
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Error connecting MassTransit receivers");
        }

        return Task.CompletedTask;
    }

    private void RegisterJobbaEndpoints(IServiceScope scope, List<JobbaMassTransitConsumerInfo> listeners)
    {
        var configurationContext = scope.ServiceProvider.GetService<JobbaMassTransitConfigurationContext>();
        var endpointConnector = scope.ServiceProvider.GetService<IReceiveEndpointConnector>();
        var configureConsumer =
            typeof(MassTransitJobbaReceiverHostedService).GetMethod(nameof(ConfigureConsumer), BindingFlags.Static | BindingFlags.NonPublic);

        if (configureConsumer == null || configurationContext == null || endpointConnector == null)
        {
            return;
        }

        var queues = GetQueues(scope, configurationContext.QueueMode, configurationContext.ReceiveEndpointPrefix, listeners);

        foreach (var (queueName, consumerInfos) in queues)
        {
            endpointConnector.ConnectReceiveEndpoint(queueName, (_, configurator) =>
            {
                foreach (var consumerInfo in consumerInfos)
                {
                    configureConsumer.MakeGenericMethod(consumerInfo.ConsumerType)
                        .Invoke(null, new object[]
                        {
                            configurator,
                            _serviceProvider
                        });
                }
            });
        }
    }

    private static Dictionary<string, List<JobbaMassTransitConsumerInfo>> GetQueues(
        IServiceScope scope,
        JobbaMassTransitQueueMode queueMode,
        string receiveEndpointPrefix,
        List<JobbaMassTransitConsumerInfo> consumerInfos)
    {
        if (queueMode == JobbaMassTransitQueueMode.Unknown)
        {
            throw new ArgumentException("Queue mode is unknown", nameof(queueMode));
        }

        var configurationContext = scope.ServiceProvider.GetService<JobbaMassTransitConfigurationContext>();

        var prefix = configurationContext?.QueuePrefix ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(receiveEndpointPrefix))
        {
            prefix = $"{prefix}_{receiveEndpointPrefix}";
        }

        return queueMode switch
        {
            JobbaMassTransitQueueMode.OneQueue
                => new Dictionary<string, List<JobbaMassTransitConsumerInfo>> { { prefix, consumerInfos } },
            JobbaMassTransitQueueMode.OnePerJob
                => consumerInfos
                    .GroupBy(x => $"{prefix}_{x.QueueName}")
                    .ToDictionary(x => x.Key, x => x.ToList()),
            _ => new Dictionary<string, List<JobbaMassTransitConsumerInfo>>()
        };
    }

    private static void ConfigureConsumer<TConsumer>(
        IReceiveEndpointConfigurator receiveEndpointConfigurator,
        IServiceProvider serviceProvider)
        where TConsumer : class, IConsumer => receiveEndpointConfigurator.Consumer(typeof(TConsumer), _ =>
    {
        var scope = serviceProvider.CreateScope();
        var consumer = scope.ServiceProvider.GetService<TConsumer>();
        return consumer;
    });
}
