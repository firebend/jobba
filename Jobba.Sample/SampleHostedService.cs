using System;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Interfaces;
using Jobba.Core.Models;
using Microsoft.Extensions.Hosting;

namespace Jobba.Sample
{
    public class SampleHostedService : BackgroundService
    {
        private readonly IJobScheduler _jobScheduler;

        public SampleHostedService(IJobScheduler jobScheduler)
        {
            _jobScheduler = jobScheduler;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var request = new JobRequest<SampleJobParameters, SampleJobState>
            {
                Description = "A Sample Job",
                JobParameters = new SampleJobParameters { Greeting = "Hello" },
                JobType = typeof(SampleJob),
                InitialJobState = new SampleJobState { Tries = 0 },
                JobWatchInterval = TimeSpan.FromSeconds(10),
                MaxNumberOfTries = 100
            };

            await _jobScheduler.ScheduleJobAsync(request, stoppingToken);

            var cancelJobRequest = new JobRequest<object, object>
            {
                Description = "A Sample Job that should get cancelled",
                JobParameters = new object(),
                JobType = typeof(SampleJobCancel),
                InitialJobState = new object(),
                JobWatchInterval = TimeSpan.FromSeconds(2),
                MaxNumberOfTries = 100
            };

            var jobToCancel = await _jobScheduler.ScheduleJobAsync(cancelJobRequest, stoppingToken);
            // ReSharper disable once UnusedVariable
            var jobToRunUntilClose = await _jobScheduler.ScheduleJobAsync(cancelJobRequest, stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            await _jobScheduler.CancelJobAsync(jobToCancel.Id, stoppingToken);
        }
    }
}
