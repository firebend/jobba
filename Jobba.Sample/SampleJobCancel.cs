using System;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Abstractions;
using Jobba.Core.Interfaces.Repositories;
using Jobba.Core.Models;
using Microsoft.Extensions.Logging;

namespace Jobba.Sample
{
    public class SampleJobCancel : AbstractJobBaseClass<object, object>
    {
        private Guid MyId { get; } = Guid.NewGuid();

        private readonly ILogger<SampleJobCancel> _logger;

        public SampleJobCancel(IJobProgressStore progressStore, ILogger<SampleJobCancel> logger) : base(progressStore)
        {
            _logger = logger;
        }

        protected override async Task OnStartAsync(JobStartContext<object, object> jobStartContext, CancellationToken cancellationToken)
        {
            _logger.LogInformation("I'm running and running {@MyId}", MyId);

            while (!cancellationToken.IsCancellationRequested)
            {

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                }
                catch (TaskCanceledException)
                {

                }
            }

            _logger.LogInformation("Now i'm done running and running {@MyId}", MyId);
        }

        public override string JobName => "Sample Job Cancel";
    }
}
