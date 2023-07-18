using System;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Events;
using Jobba.Core.Interfaces;
using Jobba.Core.Interfaces.Subscribers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jobba.Core.Implementations;

public class DefaultOnJobWatchSubscriber : IOnJobWatchSubscriber
{
    private readonly ILogger<DefaultOnJobWatchSubscriber> _logger;
    private readonly IServiceProvider _serviceProvider;

    public DefaultOnJobWatchSubscriber(IServiceProvider serviceProvider, ILogger<DefaultOnJobWatchSubscriber> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
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

            var jobParametersType = Type.GetType(jobWatchEvent.ParamsTypeName) ?? throw new Exception($"Could not find type for parameters: {jobWatchEvent.ParamsTypeName}");

            var jobStateType = Type.GetType(jobWatchEvent.StateTypeName) ?? throw new Exception($"Could not find type for state : {jobWatchEvent.StateTypeName}");

            var jobWatcherType = typeof(IJobWatcher<,>).MakeGenericType(jobParametersType, jobStateType);

            using var scope = _serviceProvider.CreateScope();
            {
                var watcher = scope.ServiceProvider.GetService(jobWatcherType) ?? throw new Exception($"Could not find job watch. Type: {jobWatcherType}");

                var methodInfo = jobWatcherType.GetMethod(nameof(IJobWatcher<object, object>.WatchJobAsync)) ?? throw new Exception("Could not find job watcher watch job method.");

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
}
