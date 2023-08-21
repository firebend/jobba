using System;
using System.Collections.Generic;
using System.Linq;
using Jobba.Core.Extensions;
using Jobba.Core.Interfaces;
using Jobba.MassTransit.Interfaces;
using Jobba.MassTransit.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Jobba.MassTransit.Implementations;

public class JobbaMassTransitConsumerInfoProvider : IJobbaMassTransitConsumerInfoProvider, IDisposable
{
    private readonly JobbaMassTransitConfigurationContext _configurationContext;
    private readonly IServiceScopeFactory _scopeFactory;

    public JobbaMassTransitConsumerInfoProvider(JobbaMassTransitConfigurationContext configurationContext, IServiceScopeFactory scopeFactory)
    {
        _configurationContext = configurationContext;
        _scopeFactory = scopeFactory;
    }

    public void Dispose() => GC.SuppressFinalize(this);

    public IEnumerable<JobbaMassTransitConsumerInfo> GetConsumerInfos()
    {
        if (!_scopeFactory.TryCreateScope(out var scope))
        {
            yield break;
        }

        using (scope)
        {
            var consumers = scope
                .ServiceProvider
                .GetServices<IJobbaMassTransitConsumer>()
                .ToList();

            if (_configurationContext.QueueMode == JobbaMassTransitQueueMode.OnePerJob)
            {
                var jobs = scope
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
}
