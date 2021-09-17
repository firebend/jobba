using System;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Abstractions;
using Jobba.Core.Interfaces.Repositories;
using Jobba.Core.Models;
using Microsoft.Extensions.Logging;

namespace Jobba.Web.Sample
{
    public class SampleWebJobCancel : AbstractJobBaseClass<object, object>
    {
        private Guid MyId { get; } = Guid.NewGuid();

        private readonly ILogger<SampleWebJobCancel> _logger;

        public SampleWebJobCancel(IJobProgressStore progressStore, ILogger<SampleWebJobCancel> logger) : base(progressStore)
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
