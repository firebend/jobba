using System;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Events;
using Jobba.Core.Extensions;
using Jobba.Core.Interfaces;
using Jobba.Core.Interfaces.Subscribers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jobba.Core.Implementations;

public class DefaultOnJobWatchSubscriber : IOnJobWatchSubscriber
{
    private readonly ILogger<DefaultOnJobWatchSubscriber> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public DefaultOnJobWatchSubscriber(ILogger<DefaultOnJobWatchSubscriber> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }


    public async Task WatchJobAsync(JobWatchEvent jobWatchEvent, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(jobWatchEvent.ParamsTypeName))
            {
                throw new ArgumentException("No job parameters type name provided.", nameof(jobWatchEvent));
            }

            if (string.IsNullOrWhiteSpace(jobWatchEvent.StateTypeName))
            {
                throw new ArgumentException("No job state type name provided.", nameof(jobWatchEvent));
            }

            var jobParametersType = Type.GetType(jobWatchEvent.ParamsTypeName)
                                    ?? throw new Exception($"Could not find type for parameters: {jobWatchEvent.ParamsTypeName}");

            var jobStateType = Type.GetType(jobWatchEvent.StateTypeName)
                               ?? throw new Exception($"Could not find type for state : {jobWatchEvent.StateTypeName}");

            var jobWatcherType = typeof(IJobWatcher<,>).MakeGenericType(jobParametersType, jobStateType);

            if (_scopeFactory.TryCreateScope(out var scope))
            {
                using var serviceScope = scope;

                var watcher = CreateWatcher(scope,
                    jobWatcherType,
                    jobParametersType,
                    jobStateType);

                var methodInfo = jobWatcherType.GetMethod(nameof(IJobWatcher<DefaultJobParams, DefaultJobState>.WatchJobAsync)) ??
                                 throw new Exception("Could not find job watcher watch job method.");

                var invokeReturn = methodInfo.Invoke(watcher, new object[]
                {
                    jobWatchEvent.JobId,
                    cancellationToken
                });

                if (invokeReturn is Task task)
                {
                    await task;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Error watching jobs");
        }
    }

    private static object CreateWatcher(IServiceScope scope, Type jobWatcherType, Type paramsType, Type stateType)
    {
        var watcher = scope.ServiceProvider.GetService(jobWatcherType);

        if (watcher is not null)
        {
            return watcher;
        }

        var defaultWatcherType = typeof(DefaultJobWatcher<,>).MakeGenericType(paramsType, stateType);
        watcher = scope.ServiceProvider.Materialize(defaultWatcherType);

        return watcher?? throw new Exception($"Could not find job watch. Type: {jobWatcherType}");
    }
}
