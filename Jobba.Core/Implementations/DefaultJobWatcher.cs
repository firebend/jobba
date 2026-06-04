using System;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Interfaces;
using Jobba.Core.Interfaces.Repositories;
using Jobba.Core.Models;

namespace Jobba.Core.Implementations;

public class DefaultJobWatcher<TJob, TJobParams, TJobState> : IJobWatcher<TJob, TJobParams, TJobState>
    where TJob : IJob<TJobParams, TJobState>
    where TJobParams : IJobParams
    where TJobState : IJobState
{
    private readonly IJobScheduler _jobScheduler;
    private readonly IJobStore _jobStore;
    private readonly IJobEventDispatcher _dispatcher;

    public DefaultJobWatcher(IJobEventDispatcher dispatcher,
        IJobStore jobStore,
        IJobScheduler jobScheduler)
    {
        _dispatcher = dispatcher;
        _jobStore = jobStore;
        _jobScheduler = jobScheduler;
    }


    public async Task WatchJobAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var job = await _jobStore.GetJobByIdAsync<TJobParams, TJobState>(jobId, cancellationToken);

        if (job == null || job.Status == JobStatus.Completed)
        {
            return;
        }

        switch (job.Status)
        {
            case JobStatus.InProgress or JobStatus.Enqueued:
            {
                await ContinueWatchingAsync(job, cancellationToken);
                return;
            }
            case JobStatus.Faulted:
            {
                await RestartIfNeededAsync(job, cancellationToken);

                break;
            }
        }
    }

    private async Task RestartIfNeededAsync(JobInfo<TJobParams, TJobState> job, CancellationToken cancellationToken)
    {
        if (job.CurrentNumberOfTries != job.MaxNumberOfTries)
        {
            var request = new JobRequest<TJobParams, TJobState>
            {
                Description = job.Description,
                IsRestart = true,
                JobId = job.Id,
                JobType = typeof(TJob),
                JobWatchInterval = job.JobWatchInterval,
                NumberOfTries = job.CurrentNumberOfTries + 1,
                JobParameters = job.JobParameters,
                InitialJobState = job.CurrentState,
                JobName = job.JobName,
                MaxNumberOfTries = job.MaxNumberOfTries,
            };

            _ = await _jobScheduler.ScheduleJobAsync(request, cancellationToken);
        }
    }

    private async Task ContinueWatchingAsync(JobInfo<TJobParams, TJobState> job, CancellationToken cancellationToken)
    {
        await _dispatcher.PublishWatchAsync(typeof(TJob), job.Id,
            typeof(TJobParams), typeof(TJobState),
            job.JobRegistrationId, job.JobWatchInterval, cancellationToken);
    }
}
