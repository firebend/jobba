using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Abstractions;
using Jobba.Core.Interfaces.Repositories;
using Jobba.Core.Models;
using Microsoft.Extensions.Logging;
using static System.Threading.Tasks.Task;

namespace Jobba.Web.Sample
{
    public class SampleWebJobState
    {
        public int Tries { get; set; }
    }

    public class SampleWebJobParameters
    {
        public string Greeting { get; set; }
    }

    public class SampleWebJob : AbstractJobBaseClass<SampleWebJobParameters, SampleWebJobState>
    {
        private readonly ILogger<SampleWebJob> _logger;

        public SampleWebJob(IJobProgressStore progressStore, ILogger<SampleWebJob> logger) : base(progressStore)
        {
            _logger = logger;
        }

        protected override async Task OnStartAsync(JobStartContext<SampleWebJobParameters, SampleWebJobState> jobStartContext, CancellationToken cancellationToken)
        {
            if (jobStartContext.IsRestart)
            {
                _logger.LogInformation("I was restarted");
                return;
            }

            var tries = jobStartContext.JobState.Tries + 1;
            await LogProgressAsync(new SampleWebJobState { Tries = tries }, 50, jobStartContext.JobParameters.Greeting, cancellationToken);

            while (true)
            {
                _logger.LogInformation("Waiting for someone to cancel me. {JobId}", jobStartContext.JobId);
                await Delay(1_000, cancellationToken);
            }
        }

        public override string JobName => "Sample Job";
    }
}
