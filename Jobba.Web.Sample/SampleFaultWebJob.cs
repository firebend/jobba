using System;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Abstractions;
using Jobba.Core.Interfaces.Repositories;
using Jobba.Core.Models;
using Microsoft.Extensions.Logging;
using static System.Threading.Tasks.Task;

namespace Jobba.Web.Sample
{
    public static class SampleFaultWebJobFaultContext
    {
        private static bool _shouldFault;

        private static readonly object Locker = new();

        public static bool ShouldFault()
        {
            lock (Locker)
            {
                return _shouldFault;
            }
        }

        public static void ShouldFault(bool b)
        {
            lock (Locker)
            {
                _shouldFault = b;
            }
        }
    }

    public class SampleFaultWebJobState
    {
        public int Tries { get; set; }
    }

    public class SampleFaultWebJobParameters
    {
        public string Greeting { get; set; }
    }

    public class SampleFaultWebJob : AbstractJobBaseClass<SampleFaultWebJobParameters, SampleFaultWebJobState>
    {
        private readonly ILogger<SampleFaultWebJob> _logger;

        public SampleFaultWebJob(IJobProgressStore progressStore, ILogger<SampleFaultWebJob> logger) : base(progressStore)
        {
            _logger = logger;
        }

        protected override async Task OnStartAsync(JobStartContext<SampleFaultWebJobParameters, SampleFaultWebJobState> jobStartContext, CancellationToken cancellationToken)
        {
            if (jobStartContext.IsRestart)
            {
                _logger.LogInformation("I was restarted");
                return;
            }

            var tries = jobStartContext.JobState.Tries + 1;
            await LogProgressAsync(new SampleFaultWebJobState { Tries = tries }, 50, jobStartContext.JobParameters.Greeting, cancellationToken);

            while (true)
            {
                if (SampleFaultWebJobFaultContext.ShouldFault())
                {
                    throw new Exception("I faulted!");
                }

                _logger.LogInformation("Waiting for someone to fault me. {JobId}", jobStartContext.JobId);
                await Delay(1_000, cancellationToken);
            }
        }

        public override string JobName => "Sample Job";
    }
}
