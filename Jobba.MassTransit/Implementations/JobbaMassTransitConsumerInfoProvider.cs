using System;
using System.Collections.Generic;
using System.Linq;
using Jobba.Core.Interfaces;
using Jobba.MassTransit.Interfaces;
using Jobba.MassTransit.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Jobba.MassTransit.Implementations
{
    //todo: test
    public class JobbaMassTransitConsumerInfoProvider : IJobbaMassTransitConsumerInfoProvider
    {
        private readonly IServiceProvider _provider;
        private readonly JobbaMassTransitConfigurationContext _configurationContext;

        public JobbaMassTransitConsumerInfoProvider(IServiceProvider provider,
            JobbaMassTransitConfigurationContext configurationContext)
        {
            _provider = provider;
            _configurationContext = configurationContext;
        }

        public IEnumerable<JobbaMassTransitConsumerInfo> GetConsumerInfos()
        {
            using var scope = _provider.CreateScope();

            var consumers = scope
                .ServiceProvider
                .GetServices<IJobbaMassTransitConsumer>()
                .ToList();

            var jobs = Enumerable.Empty<IJob>();

            if (_configurationContext.QueueMode == JobbaMassTransitQueueMode.OnePerJob)
            {
                jobs = scope
                    .ServiceProvider
                    .GetServices<IJob>();

                foreach (var job in jobs)
                {
                    foreach (var consumer in consumers)
                    {
                        yield return new JobbaMassTransitConsumerInfo {ConsumerType = consumer.GetType(), QueueName = job.JobName.Replace(" ", "_")};
                    }
                }
            }
            else
            {
                foreach (var consumer in consumers)
                {
                    yield return new JobbaMassTransitConsumerInfo {ConsumerType = consumer.GetType(), QueueName = string.Empty};
                }
            }
        }
    }
}
