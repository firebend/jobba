using System;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Events;
using Jobba.Core.Interfaces;
using Jobba.Core.Interfaces.Subscribers;
using Microsoft.Extensions.DependencyInjection;

namespace Jobba.Core.Implementations
{
    public class DefaultOnJobWatchSubscriber : IOnJobWatchSubscriber
    {
        private readonly IServiceProvider _serviceProvider;

        public DefaultOnJobWatchSubscriber(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task WatchJobAsync(JobWatchEvent jobWatchEvent, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(jobWatchEvent.ParamsTypeName))
            {
                throw new ArgumentException("No job parameters type name provided.", nameof(jobWatchEvent));
            }

            if (string.IsNullOrWhiteSpace(jobWatchEvent.StateTypeName))
            {
                throw new ArgumentException("No job state type name provided.", nameof(jobWatchEvent));
            }

            var jobParametersType = Type.GetType(jobWatchEvent.ParamsTypeName);

            if (jobParametersType == null)
            {
                throw new Exception($"Could not find type for parameters: {jobWatchEvent.ParamsTypeName}");
            }

            var jobStateType = Type.GetType(jobWatchEvent.StateTypeName);

            if (jobStateType == null)
            {
                throw new Exception($"Could not find type for state : {jobWatchEvent.StateTypeName}");
            }

            var jobWatcherType = typeof(IJobWatcher<,>).MakeGenericType(jobParametersType, jobStateType);

            using var scope = _serviceProvider.CreateScope();
            var watcher = scope.ServiceProvider.GetService(jobWatcherType);

            if (watcher == null)
            {
                throw new Exception($"Could not find job watch. Type: {jobWatcherType}");
            }

            var methodInfo = jobWatcherType.GetMethod(nameof(IJobWatcher<object, object>.WatchJobAsync));

            if (methodInfo == null)
            {
                throw new Exception("Could not find job watcher watch job method.");
            }

            var invokeReturn = methodInfo.Invoke(watcher, new object[] { jobWatchEvent.JobId, cancellationToken });

            if (invokeReturn is Task task)
            {
                await task;
            }

        }
    }
}
