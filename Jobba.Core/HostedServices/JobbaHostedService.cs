using System;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jobba.Core.HostedServices
{
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
                try
                {
                    return jobScheduler.RestartFaultedJobsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex, "Error trying to restart failed jobs");
                }
            }

            _logger.LogCritical("Could not resolve job scheduler");
            return Task.CompletedTask;
        }
    }
}
