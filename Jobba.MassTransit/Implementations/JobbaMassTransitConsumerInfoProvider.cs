using System;
using System.Collections.Generic;
using System.Linq;
using Jobba.Core.Extensions;
using Jobba.Core.Interfaces;
using Jobba.Core.Models;
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
            var openConsumerTypes = scope
                .ServiceProvider
                .GetServices<JobbaMassTransitOpenConsumerRegistration>()
                .Select(r => r.OpenConsumerType)
                .ToList();

            var registrations = scope
                .ServiceProvider
                .GetServices<JobRegistration>()
                .ToList();

            foreach (var registration in registrations)
            {
                foreach (var openConsumerType in openConsumerTypes)
                {
                    var closedType = openConsumerType.MakeGenericType(registration.JobType, registration.JobParamsType, registration.JobStateType);
                    yield return new JobbaMassTransitConsumerInfo
                    {
                        ConsumerType = closedType,
                        QueueName = registration.JobName.Replace(" ", "_")
                    };
                }
            }
        }
    }
}
