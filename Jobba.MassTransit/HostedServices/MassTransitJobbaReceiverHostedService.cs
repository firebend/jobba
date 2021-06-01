using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Jobba.MassTransit.Interfaces;
using Jobba.MassTransit.Models;
using MassTransit;
using Microsoft.Extensions.Hosting;

namespace Jobba.MassTransit.HostedServices
{
    public class MassTransitJobbaReceiverHostedService : BackgroundService
    {
        private readonly JobbaMassTransitConfigurationContext _configurationContext;
        private readonly IReceiveEndpointConnector _endpointConnector;
        private readonly IJobbaMassTransitConsumerInfoProvider _consumerInfoProvider;

        public MassTransitJobbaReceiverHostedService(
            JobbaMassTransitConfigurationContext configurationContext,
            IReceiveEndpointConnector endpointConnector,
            IJobbaMassTransitConsumerInfoProvider consumerInfoProvider)
        {
            _configurationContext = configurationContext;
            _endpointConnector = endpointConnector;
            _consumerInfoProvider = consumerInfoProvider;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var consumers = _consumerInfoProvider.GetConsumerInfos()?.ToList() ?? new List<JobbaMassTransitConsumerInfo>();

            if (!consumers.Any())
            {
                return Task.CompletedTask;
            }

            RegisterJobbaEndpoints(consumers);

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

            var queues = GetQueues(_configurationContext.QueueMode,
                _configurationContext.ReceiveEndpointPrefix,
                listeners);

            foreach (var (queueName, consumerInfos) in queues)
            {
                _endpointConnector.ConnectReceiveEndpoint(queueName, (context, configurator) =>
                {
                    foreach (var consumerInfo in consumerInfos)
                    {
                        configureConsumer.MakeGenericMethod(consumerInfo.ConsumerType)
                            .Invoke(null, new object[]
                            {
                                context,
                                configurator
                            });
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

            switch (queueMode)
            {
                case JobbaMassTransitQueueMode.OneQueue:
                    return new Dictionary<string, List<JobbaMassTransitConsumerInfo>> { { prefix, consumerInfos } };

                case JobbaMassTransitQueueMode.OnePerJob:
                    return consumerInfos.GroupBy(x => $"{prefix}_{x.EntityActionDescription}")
                        .ToDictionary(x => x.Key,
                            x => x.ToList());
            }

            return new Dictionary<string, List<JobbaMassTransitConsumerInfo>>();
        }

        private static void ConfigureConsumer<TConsumer>(
            IRegistration context,
            IReceiveEndpointConfigurator receiveEndpointConfigurator)
            where TConsumer : class, IConsumer =>
            context.ConfigureConsumer<TConsumer>(receiveEndpointConfigurator);
    }
}
