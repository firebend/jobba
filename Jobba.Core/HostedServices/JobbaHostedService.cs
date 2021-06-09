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

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var jobScheduler = scope.ServiceProvider.GetService<IJobReScheduler>();

            if (jobScheduler != null)
            {
                try
                {
                    await jobScheduler.RestartFaultedJobsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex, "Error trying to restart failed jobs");
                }
            }

            stoppingToken.Register(CancelAllJobs);

            //todo: test to see if we need this "keep alive thread" probably don't :P

            // while (!stoppingToken.IsCancellationRequested)
            // {
            //     try
            //     {
            //         await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            //     }
            //     catch (TaskCanceledException)
            //     {
            //
            //     }
            // }
        }

        //todo:test
        private void CancelAllJobs()
        {
            _logger.LogInformation("Cancelling all jobs!");
            using var scope = _serviceProvider.CreateScope();
            var cancellationTokenStore = scope.ServiceProvider.GetService<IJobCancellationTokenStore>();
            cancellationTokenStore?.CancelAllJobs();
        }
    }
}
