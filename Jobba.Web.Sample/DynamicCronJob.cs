using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Interfaces.Repositories;
using Jobba.Core.Models;
using Jobba.Cron.Abstractions;
using Microsoft.Extensions.Logging;

namespace Jobba.Web.Sample;

public static class DynamicCronJobStatics
{
    public static readonly ConcurrentDictionary<Guid, Guid> Runs = new();
}

public class DynamicCronJob : AbstractCronJobBaseClass<CronParameters, CronState>
{
    public const string Name = "job-cron-dynamic";
    private readonly ILogger<DynamicCronJob> _logger;

    public DynamicCronJob(IJobProgressStore progressStore, ILogger<DynamicCronJob> logger) : base(progressStore)
    {
        _logger = logger;
    }

    public override string JobName => Name;

    protected override Task OnStartAsync(JobStartContext<CronParameters, CronState> jobStartContext, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Dynamic cron job is running {Str1} {Str2}",
            jobStartContext.JobParameters.StartDate,
            jobStartContext.JobState.Phrase);

        DynamicCronJobStatics.Runs[jobStartContext.JobRegistration.Id] = jobStartContext.JobId;

        return Task.CompletedTask;
    }
}
