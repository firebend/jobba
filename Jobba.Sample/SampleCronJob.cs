using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Interfaces;
using Jobba.Core.Interfaces.Repositories;
using Jobba.Core.Models;
using Jobba.Cron.Abstractions;
using Microsoft.Extensions.Logging;

namespace Jobba.Sample;

public class SampleCronJob : AbstractCronJobBaseClass<DefaultJobParams,DefaultJobState>
{
    private readonly ILogger<SampleCronJob> _logger;

    public const string Name = "sample-cron-job";

    public SampleCronJob(IJobProgressStore progressStore, ILogger<SampleCronJob> logger) : base(progressStore)
    {
        _logger = logger;
    }

    public override string JobName => Name;

    protected override Task OnStartAsync(JobStartContext<DefaultJobParams, DefaultJobState> jobStartContext, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Starting sample cron job");
        return Task.CompletedTask;
    }
}
