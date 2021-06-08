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
        }
    }
}
