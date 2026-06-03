using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Extensions;
using Jobba.Core.Interfaces;
using Jobba.MassTransit.Interfaces;
using Jobba.MassTransit.Models;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jobba.MassTransit.HostedServices;

public class MassTransitJobbaReceiverHostedService : BackgroundService, IJobbaReadyGate
{
    private readonly TaskCompletionSource _endpointsReadySource = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private readonly ILogger<MassTransitJobbaReceiverHostedService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public MassTransitJobbaReceiverHostedService(ILogger<MassTransitJobbaReceiverHostedService> logger,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    /// <summary>
    /// Awaits until the MassTransit receive endpoints have been connected AND each endpoint has signalled
    /// that it is ready to receive messages. If endpoint registration fails, the awaiting task will observe
    /// the underlying exception.
    /// </summary>
    public Task WaitAsync(CancellationToken cancellationToken)
        => _endpointsReadySource.Task.WaitAsync(cancellationToken);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            if (!_scopeFactory.TryCreateScope(out var scope))
            {
                _logger.LogWarning("Could not create service scope to register MassTransit receivers; opening ready gate with no endpoints.");
                _endpointsReadySource.TrySetResult();
                return;
            }

            List<HostReceiveEndpointHandle> handles;

            using (scope)
            {
                var consumerInfoProvider = scope.ServiceProvider.GetService<IJobbaMassTransitConsumerInfoProvider>();

                var consumers = consumerInfoProvider?.GetConsumerInfos()?.ToList() ?? [];

                if (consumers.Count == 0)
                {
                    _endpointsReadySource.TrySetResult();
                    return;
                }

                handles = RegisterJobbaEndpoints(scope, consumers);

                if (handles.Count == 0)
                {
                    _endpointsReadySource.TrySetResult();
                    return;
                }

                await Task.WhenAll(handles.Select(h => h.Ready)).WaitAsync(stoppingToken);
                _endpointsReadySource.TrySetResult();
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _endpointsReadySource.TrySetCanceled(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Error connecting MassTransit receivers");
            _endpointsReadySource.TrySetException(ex);
        }
    }

    private List<HostReceiveEndpointHandle> RegisterJobbaEndpoints(IServiceScope scope, List<JobbaMassTransitConsumerInfo> listeners)
    {
        var handles = new List<HostReceiveEndpointHandle>();

        var configurationContext = scope.ServiceProvider.GetService<JobbaMassTransitConfigurationContext>();
        var endpointConnector = scope.ServiceProvider.GetService<IReceiveEndpointConnector>();

        if (configurationContext == null || endpointConnector == null)
        {
            return handles;
        }

        var queues = GetQueues(scope, configurationContext.QueueMode, configurationContext.ReceiveEndpointPrefix, listeners);

        foreach (var (queueName, consumerInfos) in queues)
        {
            var handle = endpointConnector.ConnectReceiveEndpoint(queueName, (_, configurator) =>
            {
                foreach (var consumerInfo in consumerInfos)
                {
                    var consumerType = consumerInfo.ConsumerType;
                    configurator.Consumer(consumerType, _ => {
                        if (!_scopeFactory.TryCreateScope(out var scope))
                        {
                            return null;
                        }

                        using (scope)
                        {
                            var consumer = scope.ServiceProvider.GetService(consumerType);
                            return consumer;
                        }
                    });
                }
            });

            handles.Add(handle);
        }

        return handles;
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
        var systemInfoProvider = scope.ServiceProvider.GetService<IJobSystemInfoProvider>();

        var prefix = configurationContext?.QueuePrefix ?? string.Empty;
        var systemMoniker = systemInfoProvider?.GetSystemInfo().SystemMoniker ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(systemMoniker))
        {
            prefix = string.IsNullOrWhiteSpace(prefix)
                ? systemMoniker
                : $"{prefix}_{systemMoniker}";
        }

        if (!string.IsNullOrWhiteSpace(receiveEndpointPrefix))
        {
            prefix = string.IsNullOrWhiteSpace(prefix)
                ? receiveEndpointPrefix
                : $"{prefix}_{receiveEndpointPrefix}";
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

}
