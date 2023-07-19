using System;
using System.Collections.Generic;
using System.Linq;
using Jobba.Core.Interfaces;
using Jobba.MassTransit.Interfaces;
using Jobba.MassTransit.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Jobba.MassTransit.Implementations;

public class JobbaMassTransitConsumerInfoProvider : IJobbaMassTransitConsumerInfoProvider, IDisposable
{
    private readonly JobbaMassTransitConfigurationContext _configurationContext;
    private readonly IServiceScope _serviceScope;

    public JobbaMassTransitConsumerInfoProvider(IServiceProvider serviceProvider,
        JobbaMassTransitConfigurationContext configurationContext)
    {
        _serviceScope = serviceProvider.CreateScope();
        _configurationContext = configurationContext;
    }

    public void Dispose() => _serviceScope?.Dispose();

    public IEnumerable<JobbaMassTransitConsumerInfo> GetConsumerInfos()
    {
        var consumers = _serviceScope
            .ServiceProvider
            .GetServices<IJobbaMassTransitConsumer>()
            .ToList();

        if (_configurationContext.QueueMode == JobbaMassTransitQueueMode.OnePerJob)
        {
            var jobs = _serviceScope
                .ServiceProvider
                .GetServices<IJob>();

            foreach (var job in jobs)
            {
                foreach (var consumer in consumers)
                {
                    yield return new JobbaMassTransitConsumerInfo
                    {
                        ConsumerType = consumer.GetType(),
                        QueueName = job.JobName.Replace(" ", "_")
                    };
                }
            }
        }
        else
        {
            foreach (var consumer in consumers)
            {
                yield return new JobbaMassTransitConsumerInfo
                {
                    ConsumerType = consumer.GetType(),
                    QueueName = string.Empty
                };
            }
        }
    }
}
