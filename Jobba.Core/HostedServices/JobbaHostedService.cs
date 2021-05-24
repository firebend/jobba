using System;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jobba.Core.HostedServices
{
    //todo: test
    public class JobbaHostedService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<JobbaHostedService> _logger;

        public JobbaHostedService(IServiceProvider serviceProvider, ILogger<JobbaHostedService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var jobScheduler = scope.ServiceProvider.GetService<IJobReScheduler>();

            if (jobScheduler != null)
            {
                return jobScheduler.RestartFaultedJobs(stoppingToken);
            }

            _logger.LogCritical("Could not resolve job scheduler");
            return Task.CompletedTask;
        }
    }
}
