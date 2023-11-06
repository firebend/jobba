using System;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Events;
using Jobba.Core.Extensions;
using Jobba.Core.Interfaces;
using Jobba.Core.Interfaces.Repositories;
using Jobba.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jobba.Core.Implementations;

public class DefaultJobScheduler : IJobScheduler, IDisposable
{
    private readonly IJobbaGuidGenerator _guidGenerator;
    private readonly IJobCancellationTokenStore _jobCancellationTokenStore;
    private readonly IJobStore _jobStore;
    private readonly IJobLockService _lockService;
    private readonly ILogger<DefaultJobScheduler> _logger;
    private readonly IJobEventPublisher _publisher;
    private readonly IJobRegistrationStore _jobRegistrationStore;
    private readonly IServiceScopeFactory _scopeFactory;

    public DefaultJobScheduler(IJobEventPublisher publisher,
        IJobStore jobStore,
        IJobCancellationTokenStore jobCancellationTokenStore,
        IJobbaGuidGenerator guidGenerator,
        IJobLockService lockService,
        ILogger<DefaultJobScheduler> logger,
        IServiceScopeFactory scopeFactory,
        IJobRegistrationStore jobRegistrationStore)
    {
        _publisher = publisher;
        _jobStore = jobStore;
        _jobCancellationTokenStore = jobCancellationTokenStore;
        _guidGenerator = guidGenerator;
        _lockService = lockService;
        _logger = logger;
        _scopeFactory = scopeFactory;
        _jobRegistrationStore = jobRegistrationStore;
    }

    public void Dispose() => GC.SuppressFinalize(this);

    public async Task<JobInfo<TJobParams, TJobState>> ScheduleJobAsync<TJobParams, TJobState>(
        JobRequest<TJobParams, TJobState> request,
        CancellationToken cancellationToken)
    {
        var jobId = await GetJobIdAsync(request, cancellationToken);

        if (!await CanRunAsync(jobId, cancellationToken))
        {
            return null;
        }

        using var jobLock = await _lockService.LockJobAsync(jobId, cancellationToken);

        if (!await CanRunAsync(jobId, cancellationToken))
        {
            return null;
        }

        var jobInfo = await UpdateAttemptsOrCreateJobAsync(request, cancellationToken);
        await _jobStore.SetJobStatusAsync(jobId, JobStatus.Enqueued, DateTimeOffset.UtcNow, cancellationToken);
        var token = _jobCancellationTokenStore.CreateJobCancellationToken(jobId, cancellationToken);
        await WatchJobAsync<TJobParams, TJobState>(jobId, request.JobRegistrationId, request.JobWatchInterval, cancellationToken);
        var context = GetJobStartContext(request, jobInfo);
        _ = RunJobAsync(request.JobRegistrationId, jobId, request.JobType, context, token, cancellationToken);
        await NotifyJobStartedAsync<TJobParams, TJobState>(jobId, request.JobRegistrationId, cancellationToken);

        return jobInfo;
    }

    public async Task CancelJobAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var job = await _jobStore.GetJobByIdAsync(jobId, cancellationToken);

        if (job is null)
        {
            return;
        }

        var canBeCancelled = job.Status is JobStatus.Enqueued or JobStatus.InProgress;

        if (canBeCancelled is false)
        {
            return;
        }

        await _publisher.PublishJobCancellationRequestAsync(
            new CancelJobEvent(job.Id, job.JobRegistrationId),
            cancellationToken);
    }

    private async Task<bool> CanRunAsync(Guid jobId, CancellationToken cancellationToken)
    {
        if (jobId == Guid.Empty)
        {
            return true;
        }

        var existingJob = await _jobStore.GetJobByIdAsync(jobId, cancellationToken);

        return existingJob?.Status is not (JobStatus.Enqueued or JobStatus.InProgress);
    }

    private async Task<Guid> GetJobIdAsync<TJobParams, TJobState>(JobRequest<TJobParams, TJobState> request, CancellationToken cancellationToken)
    {
        if (!request.IsRestart || request.JobId == Guid.Empty)
        {
            var newGuid = await _guidGenerator.GenerateGuidAsync(cancellationToken);
            request.JobId = newGuid;
        }

        return request.JobId;
    }

    private Task NotifyJobStartedAsync<TJobParams, TJobState>(Guid jobId, Guid jobRegistrationId, CancellationToken token)
        => _publisher.PublishJobStartedEvent(
            new JobStartedEvent(jobId, jobRegistrationId),
            token);

    private async Task WatchJobAsync<TJobParams, TJobState>(Guid jobId,
        Guid jobRegistrationId,
        TimeSpan watchInterval,
        CancellationToken cancellationToken)
    {
        var watchEvent = new JobWatchEvent(jobId,
            typeof(TJobParams).AssemblyQualifiedName,
            typeof(TJobState).AssemblyQualifiedName,
            jobRegistrationId);

        await _publisher.PublishWatchJobEventAsync(watchEvent, watchInterval, cancellationToken);
    }

    private async Task<JobInfo<TJobParams, TJobState>> UpdateAttemptsOrCreateJobAsync<TJobParams, TJobState>(
        JobRequest<TJobParams, TJobState> request,
        CancellationToken cancellationToken)
    {
        JobInfo<TJobParams, TJobState> jobInfo;

        if (request.IsRestart && request.JobId != Guid.Empty)
        {
            _logger.LogDebug("Updating number of tries for job. JobId: {JobId}. Tries : {Tries}", request.JobId, request.NumberOfTries);
            jobInfo = await _jobStore.SetJobAttempts<TJobParams, TJobState>(request.JobId, request.NumberOfTries, cancellationToken);
        }
        else
        {
            jobInfo = await _jobStore.AddJobAsync(request, cancellationToken);
            _logger.LogDebug("Created job. JobId: {JobId}", jobInfo.Id);
        }

        return jobInfo;
    }

    /// <summary>
    ///     Runs the requested job as a task, publishes messages, and updates initial state
    /// </summary>
    /// <param name="jobId">
    ///     The ID of the job to run
    /// </param>
    /// <param name="jobType">
    ///     The job type to resolve from the IoC container
    /// </param>
    /// <param name="context">
    ///     The job start context containing job run parameters.
    /// </param>
    /// <param name="jobCancellationToken">
    ///     A cancellation token pointing to a source that should only cancel the job. i.e. from a job cancellation event.
    /// </param>
    /// <param name="cancellationToken">
    ///     A cancellation token that was passed to the job scheduler. i.e. app is shutting down, http cancellation, etc
    /// </param>
    /// <typeparam name="TJobParams">
    ///     The parameters of the job
    /// </typeparam>
    /// <typeparam name="TJobState">
    ///     The state of the job.
    /// </typeparam>
    /// <exception cref="Exception">
    ///     Throws an exception if the job cannot be resolved from the IoC container.
    /// </exception>
    private async Task RunJobAsync<TJobParams, TJobState>(Guid jobRegistrationId,
        Guid jobId,
        Type jobType,
        JobStartContext<TJobParams, TJobState> context,
        CancellationToken jobCancellationToken,
        CancellationToken cancellationToken)
    {
        if (_scopeFactory.TryCreateScope(out var scope))
        {
            var registration = await _jobRegistrationStore.GetJobRegistrationAsync(jobRegistrationId, cancellationToken);

            if (registration is null)
            {
               throw new Exception($"Could not resolve job registration from store. Job Registration Id {jobRegistrationId}");
            }

            if (registration.JobType != jobType)
            {
                throw new Exception($"Job type mismatch. Job Registration Id {jobRegistrationId}. Expected {registration.JobType}. Actual {jobType}");
            }

            if (scope.ServiceProvider.Materialize(registration.JobType) is not IJob<TJobParams, TJobState> job)
            {
                throw new Exception($"Could not resolve job from service provider. Job Type {jobType}");
            }

            await Task.Run(async () =>
            {
                await _jobStore.SetJobStatusAsync(jobId, JobStatus.InProgress, DateTimeOffset.UtcNow, default);

                try
                {
                    await job.StartAsync(context, jobCancellationToken);

                    if (jobCancellationToken.IsCancellationRequested || cancellationToken.IsCancellationRequested)
                    {
                        await OnJobCancelledAsync(jobId, cancellationToken.IsCancellationRequested, default);
                    }
                    else
                    {
                        await OnJobCompletedAsync(jobId, jobRegistrationId, default);
                        _jobCancellationTokenStore.RemoveCompletedJob(jobId);
                    }
                }
                catch (TaskCanceledException)
                {
                    if (jobCancellationToken.IsCancellationRequested || cancellationToken.IsCancellationRequested)
                    {
                        await OnJobCancelledAsync(jobId, cancellationToken.IsCancellationRequested, default);
                    }
                }
                catch (Exception ex)
                {
                    await OnJobFaulted<TJobParams, TJobState>(jobId, jobRegistrationId, ex);
                }
                finally
                {
                    scope.Dispose();
                }
            }, cancellationToken);
        }
    }

    private async Task OnJobFaulted<TJobParams, TJobState>(Guid jobId, Guid jobRegistrationId, Exception ex)
    {
        _logger.LogDebug("Job Faulted. JobId: {JobId}. Message: {ExceptionMessage}", jobId, ex.Message);

        await _jobStore.LogFailureAsync(jobId, ex, default);

        await _publisher.PublishJobFaultedEventAsync(
            new JobFaultedEvent(jobId, jobRegistrationId),
            default);
    }

    private Task OnJobCancelledAsync(Guid jobId, bool wasForced, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Job Cancelled. JobId: {JobId} WasForced: {WasForced}", jobId, wasForced);

        return _jobStore.SetJobStatusAsync(jobId, wasForced ? JobStatus.ForceCancelled : JobStatus.Cancelled, DateTimeOffset.UtcNow, cancellationToken);
    }

    private async Task OnJobCompletedAsync(Guid jobId, Guid jobRegistrationId, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Job Completed. Id: {JobId}", jobId);

        await _jobStore.SetJobStatusAsync(jobId, JobStatus.Completed, DateTimeOffset.UtcNow, cancellationToken);

        await _publisher.PublishJobCompletedEventAsync(
            new JobCompletedEvent(jobId, jobRegistrationId),
            cancellationToken);
    }

    private static JobStartContext<TJobParams, TJobState> GetJobStartContext<TJobParams, TJobState>(
        JobRequest<TJobParams, TJobState> request,
        JobInfoBase jobInfo) => new()
        {
            JobId = jobInfo.Id,
            JobParameters = request.JobParameters,
            JobState = request.InitialJobState,
            StartTime = jobInfo.EnqueuedTime,
            IsRestart = request.IsRestart,
            LastProgressDate = jobInfo.LastProgressDate,
            LastProgressPercentage = jobInfo.LastProgressPercentage,
            CurrentNumberOfTries = request.NumberOfTries,
            JobRegistrationId = request.JobRegistrationId,
        };
}
