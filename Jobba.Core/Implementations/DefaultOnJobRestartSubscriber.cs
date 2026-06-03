using System;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Events;
using Jobba.Core.Interfaces;
using Jobba.Core.Interfaces.Repositories;
using Jobba.Core.Interfaces.Subscribers;
using Jobba.Core.Models;
using Microsoft.Extensions.Logging;

namespace Jobba.Core.Implementations;

public class DefaultOnJobRestartSubscriber<TJob, TJobParams, TJobState> : AbstractJobbaEventSubscriber, IOnJobRestartSubscriber<TJob, TJobParams, TJobState>
    where TJob : IJob<TJobParams, TJobState>
    where TJobParams : IJobParams
    where TJobState : IJobState
{
    private readonly IJobLockService _jobLockService;
    private readonly IJobScheduler _jobScheduler;
    private readonly IJobStore _jobStore;
    private readonly ILogger<DefaultOnJobRestartSubscriber<TJob, TJobParams, TJobState>> _logger;

    public DefaultOnJobRestartSubscriber(
        IJobLockService jobLockService,
        IJobStore jobStore,
        IJobScheduler jobScheduler,
        IJobSystemInfoProvider systemInfoProvider,
        ILogger<DefaultOnJobRestartSubscriber<TJob, TJobParams, TJobState>> logger)
        : base(systemInfoProvider)
    {
        _jobLockService = jobLockService;
        _jobStore = jobStore;
        _jobScheduler = jobScheduler;
        _logger = logger;
    }

    public async Task OnJobRestartAsync(JobRestartEvent<TJob> jobRestartEvent, CancellationToken cancellationToken)
    {
        if (!ShouldProcessEvent(jobRestartEvent))
        {
            return;
        }

        using var _ = await _jobLockService.LockJobAsync(jobRestartEvent.JobId, "restart", cancellationToken);

        await RestartJob(jobRestartEvent.JobId, cancellationToken);
    }

    public async Task RestartJob(Guid jobId, CancellationToken cancellationToken)
    {
        var job = await _jobStore.GetJobByIdAsync<TJobParams, TJobState>(jobId, cancellationToken);

        if (job == null)
        {
            return;
        }

        if (job.Status != JobStatus.Faulted
            && job.Status != JobStatus.ForceCancelled
            && job.Status != JobStatus.Unknown)
        {
            return;
        }

        if (job.CurrentNumberOfTries >= job.MaxNumberOfTries)
        {
            return;
        }

        JobRequest<TJobParams, TJobState> request;

        try
        {
            request = JobRequest<TJobParams, TJobState>.FromJobInfo(job);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to create job request from job info for job {jobId}.");
            return;
        }

        _ = await _jobScheduler.ScheduleJobAsync(request, cancellationToken);
    }
}
