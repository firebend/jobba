using System;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Events;
using Jobba.Core.Interfaces;
using Jobba.Core.Interfaces.Subscribers;
using Microsoft.Extensions.DependencyInjection;

namespace Jobba.Core.Implementations
{
    //todo: test
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
                return;
            }

            if (string.IsNullOrWhiteSpace(jobWatchEvent.StateTypeName))
            {
                return;
            }

            var jobParametersType = Type.GetType(jobWatchEvent.ParamsTypeName);
            var jobStateType = Type.GetType(jobWatchEvent.StateTypeName);
            var jobWatcherType = typeof(IJobWatcher<,>).MakeGenericType(jobParametersType, jobStateType);

            using var scope = _serviceProvider.CreateScope();
            var watcher = scope.ServiceProvider.GetService(jobWatcherType);

            if (watcher == null)
            {
                return;
            }

            var methodInfo = jobWatcherType.GetMethod(nameof(IJobWatcher<object, object>.WatchJobAsync));

            if (methodInfo == null)
            {
                return;
            }

            var invokeReturn = methodInfo.Invoke(watcher, new object[] { jobWatchEvent.JobId, cancellationToken });

            if (invokeReturn is Task task)
            {
                await task;
            }

        }
    }
}
