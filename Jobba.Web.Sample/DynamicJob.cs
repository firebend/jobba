using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Abstractions;
using Jobba.Core.Interfaces.Repositories;
using Jobba.Core.Models;
using Microsoft.Extensions.Logging;

namespace Jobba.Web.Sample;

public static class DynamicJobStatics
{
    public static readonly ConcurrentDictionary<Guid, bool> Runs = new();
}

public class DynamicJob : AbstractJobBaseClass<SampleWebJobParameters, SampleWebJobState>
{
    public const string Name = "job-dynamic";
    private readonly ILogger<DynamicJob> _logger;

    public DynamicJob(IJobProgressStore progressStore, ILogger<DynamicJob> logger) : base(progressStore)
    {
        _logger = logger;
    }

    public override string JobName => Name;

    protected override Task OnStartAsync(JobStartContext<SampleWebJobParameters, SampleWebJobState> jobStartContext, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Dynamic job is running {Str1} {Str2}",
            jobStartContext.JobParameters.Greeting,
            jobStartContext.JobState.Tries);
        DynamicJobStatics.Runs[jobStartContext.JobId] = true;
        return Task.CompletedTask;
    }
}
