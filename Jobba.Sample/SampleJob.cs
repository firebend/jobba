using System;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Abstractions;
using Jobba.Core.Interfaces.Repositories;
using Jobba.Core.Models;
using Microsoft.Extensions.Logging;

namespace Jobba.Sample
{
    public class SampleJobState
    {
        public int Tries { get; set; }
    }

    public class SampleJobParameters
    {
        public string Greeting { get; set; }
    }

    public class SampleJob : AbstractJobBaseClass<SampleJobParameters, SampleJobState>
    {
        private readonly ILogger<SampleJob> _logger;

        public SampleJob(IJobProgressStore progressStore, ILogger<SampleJob> logger) : base(progressStore)
        {
            _logger = logger;
        }

        protected override async Task OnStartAsync(JobStartContext<SampleJobParameters, SampleJobState> jobStartContext, CancellationToken cancellationToken)
        {
            var tries = jobStartContext.JobState.Tries + 1;
            _logger.LogInformation("Hey I'm trying! Tries: {Tries} {JobId} {Now}", tries, jobStartContext.JobId, DateTimeOffset.Now);
            await LogProgressAsync(new SampleJobState { Tries = tries }, 50, jobStartContext.JobParameters.Greeting, cancellationToken);
            await Task.Delay(100 * tries, cancellationToken);

            if (tries < 10)
            {
                throw new Exception($"Haven't tried enough {tries}");
            }
            _logger.LogInformation("Now I'm done!");
        }

        public override string JobName => "Sample Job";
    }
}
