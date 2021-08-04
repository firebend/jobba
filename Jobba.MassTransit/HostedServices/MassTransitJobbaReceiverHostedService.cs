using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Jobba.MassTransit.Interfaces;
using Jobba.MassTransit.Models;
using MassTransit;
using MassTransit.Registration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jobba.MassTransit.HostedServices
{
    public class MassTransitJobbaReceiverHostedService : BackgroundService
    {
        private readonly JobbaMassTransitConfigurationContext _configurationContext;
        private readonly IReceiveEndpointConnector _endpointConnector;
        private readonly ILogger<MassTransitJobbaReceiverHostedService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public MassTransitJobbaReceiverHostedService(
            JobbaMassTransitConfigurationContext configurationContext,
            IReceiveEndpointConnector endpointConnector,
            ILogger<MassTransitJobbaReceiverHostedService> logger,
            IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _configurationContext = configurationContext;
            _endpointConnector = endpointConnector;
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
                    RegisterJobbaEndpoints(consumers);
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Error connecting MassTransit receivers");
            }

            return Task.CompletedTask;
        }

        private void RegisterJobbaEndpoints(List<JobbaMassTransitConsumerInfo> listeners)
        {
            var configureConsumer = typeof(MassTransitJobbaReceiverHostedService).GetMethod(nameof(ConfigureConsumer),
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Static);

            if (configureConsumer == null)
            {
                return;
            }

            var queues = GetQueues(_configurationContext.QueueMode, _configurationContext.ReceiveEndpointPrefix, listeners);

            foreach (var (queueName, consumerInfos) in queues)
            {
                _endpointConnector.ConnectReceiveEndpoint(queueName, (context, configurator) =>
                {
                    foreach (var consumerInfo in consumerInfos)
                    {
                        configureConsumer.MakeGenericMethod(consumerInfo.ConsumerType)
                            .Invoke(null, new object[] { context, configurator });
                    }
                });
            }
        }

        private Dictionary<string, List<JobbaMassTransitConsumerInfo>> GetQueues(JobbaMassTransitQueueMode queueMode,
            string receiveEndpointPrefix,
            List<JobbaMassTransitConsumerInfo> consumerInfos)
        {
            if (queueMode == JobbaMassTransitQueueMode.Unknown)
            {
                throw new ArgumentException("Queue mode is unknown", nameof(queueMode));
            }

            var prefix = _configurationContext.QueuePrefix;

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
            IConfigurationServiceProvider context,
            IReceiveEndpointConfigurator receiveEndpointConfigurator)
            where TConsumer : class, IConsumer => receiveEndpointConfigurator.Consumer(typeof(TConsumer), _ => context.GetService<TConsumer>());
    }
}
