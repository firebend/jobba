using System;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Interfaces;
using Jobba.Core.Interfaces.Repositories;
using Jobba.Core.Models;

namespace Jobba.Core.Abstractions;

public abstract class AbstractJobBaseClass<TJobParams, TJobState> : IJob<TJobParams, TJobState>
{
    private readonly IJobProgressStore _progressStore;
    private Guid _jobId;

    protected AbstractJobBaseClass(IJobProgressStore progressStore)
    {
        _progressStore = progressStore;
    }

    public Task StartAsync(JobStartContext<TJobParams, TJobState> jobStartContext, CancellationToken cancellationToken)
    {
        _jobId = jobStartContext.JobId;
        return OnStartAsync(jobStartContext, cancellationToken);
    }

    public abstract string JobName { get; }

    protected abstract Task OnStartAsync(JobStartContext<TJobParams, TJobState> jobStartContext, CancellationToken cancellationToken);

    protected Task LogProgressAsync(TJobState state,
        decimal progressPercentage,
        string message = null,
        CancellationToken cancellationToken = default)
    {
        var progress = new JobProgress<TJobState>
        {
            Date = DateTimeOffset.UtcNow,
            Message = message,
            Progress = progressPercentage,
            JobId = _jobId,
            JobState = state
        };

        return _progressStore.LogProgressAsync(progress, cancellationToken);
    }
}
