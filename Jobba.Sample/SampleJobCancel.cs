using System;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Abstractions;
using Jobba.Core.Interfaces;
using Jobba.Core.Interfaces.Repositories;
using Jobba.Core.Models;
using Microsoft.Extensions.Logging;

namespace Jobba.Sample;

public class SampleJobCancel : AbstractJobBaseClass<DefaultJobParams, DefaultJobState>
{
    public const string Name = "sample-job-cancel";

    private readonly ILogger<SampleJobCancel> _logger;

    public SampleJobCancel(IJobProgressStore progressStore, ILogger<SampleJobCancel> logger) : base(progressStore)
    {
        _logger = logger;
    }

    private Guid MyId { get; } = Guid.NewGuid();

    public override string JobName => Name;

    protected override async Task OnStartAsync(JobStartContext<DefaultJobParams, DefaultJobState> jobStartContext, CancellationToken cancellationToken)
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
}
