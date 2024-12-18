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

public class DefaultJobScheduler(
    IJobEventPublisher publisher,
    IJobStore jobStore,
    IJobbaGuidGenerator guidGenerator,
    IJobLockService lockService,
    ILogger<DefaultJobScheduler> logger,
    IServiceScopeFactory scopeFactory,
    IJobRegistrationStore jobRegistrationStore)
    : IJobScheduler, IDisposable
{
    public void Dispose() => GC.SuppressFinalize(this);

    public async Task<JobInfo<TJobParams, TJobState>> ScheduleJobAsync<TJobParams, TJobState>(
        JobRequest<TJobParams, TJobState> request,
        CancellationToken cancellationToken)
        where TJobParams : IJobParams
        where TJobState : IJobState
    {
        if (string.IsNullOrWhiteSpace(request.JobName))
        {
            throw new ArgumentException("Job name cannot be null or whitespace.", nameof(request));
        }

        var jobRegistration = await jobRegistrationStore.GetByJobNameAsync(request.JobName, cancellationToken) ?? throw new Exception($"Job registration not found for JobName {request.JobName}");

        var info = await DoScheduleJobAsync(request, jobRegistration, cancellationToken);

        return info;
    }

    public async Task<JobInfo<TJobParams, TJobState>> ScheduleJobAsync<TJobParams, TJobState>(Guid registrationId,
        TJobParams parameters,
        TJobState state,
        CancellationToken cancellationToken)
        where TJobParams : IJobParams
        where TJobState : IJobState
    {
        var registration = await jobRegistrationStore.GetJobRegistrationAsync(registrationId, cancellationToken)
            ?? throw new Exception($"Could not resolve job registration from store with job registration id {registrationId}");

        var request = new JobRequest<TJobParams, TJobState>
        {
            InitialJobState = state,
            JobParameters = parameters,
            JobType = registration.JobType,
            JobWatchInterval = registration.DefaultJobWatchInterval,
            MaxNumberOfTries = registration.DefaultMaxNumberOfTries,
            JobName = registration.JobName,
        };

        var info = await DoScheduleJobAsync(request, registration, cancellationToken);

        return info;
    }

    public async Task CancelJobAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var job = await jobStore.GetJobByIdAsync(jobId, cancellationToken);

        if (job is null)
        {
            return;
        }

        var canBeCancelled = job.Status is JobStatus.Enqueued or JobStatus.InProgress;

        if (canBeCancelled is false)
        {
            return;
        }

        await publisher.PublishJobCancellationRequestAsync(
            new CancelJobEvent(job.Id, job.JobRegistrationId),
            cancellationToken);
    }

    public async Task<JobInfo<TJobParams, TJobState>> DoScheduleJobAsync<TJobParams, TJobState>(
        JobRequest<TJobParams, TJobState> request,
        JobRegistration jobRegistration,
        CancellationToken cancellationToken)
        where TJobParams : IJobParams
        where TJobState : IJobState
    {
        if (ValidateJobRegistration(jobRegistration, request.JobType) is false)
        {
            return null;
        }

        var jobId = await GetJobIdAsync(request, cancellationToken);

        if (!await IsJobStatusValidAsync(jobId, cancellationToken))
        {
            return null;
        }

        using var jobLock = await lockService.LockJobAsync(jobId, cancellationToken);

        if (!await IsJobStatusValidAsync(jobId, cancellationToken))
        {
            return null;
        }

        var jobInfo = await UpdateAttemptsOrCreateJobAsync(request, cancellationToken);
        await jobStore.SetJobStatusAsync(jobId, JobStatus.Enqueued, DateTimeOffset.UtcNow, cancellationToken);
        await WatchJobAsync<TJobParams, TJobState>(jobId, jobRegistration.Id, request.JobWatchInterval, cancellationToken);
        var context = GetJobStartContext(request, jobInfo, jobRegistration);
        _ = RunJobAsync(jobRegistration, request.JobType, context, cancellationToken);
        await NotifyJobStartedAsync(jobId, jobRegistration.Id, cancellationToken);

        return jobInfo;
    }

    private bool ValidateJobRegistration(JobRegistration registration, Type jobType)
    {
        if (registration is null)
        {
            logger.LogCritical("Could not resolve job registration");
            return false;
        }

        if (registration.IsInactive)
        {
            logger.LogDebug("Job is inactive. Job Registration Id {JobRegistrationId} Job Name {JobName}",
                registration.Id,
                registration.JobName);

            return false;
        }

        if (registration.JobType is null)
        {
            logger.LogCritical("Job type is null. Job Registration Id {JobRegistrationId} Job Name {JobName}",
                registration.Id,
                registration.JobName);

            return false;
        }

        if (registration.JobType != jobType)
        {
            logger.LogCritical("Job type mismatch. Job Registration Id {JobRegistrationId}. Expected {RegistrationJobType}. Actual {JobType}",
                registration.JobType,
                registration.JobType,
                jobType);

            return false;
        }

        return true;
    }

    private async Task<bool> IsJobStatusValidAsync(Guid jobId, CancellationToken cancellationToken)
    {
        if (jobId == Guid.Empty)
        {
            return true;
        }

        var existingJob = await jobStore.GetJobByIdAsync(jobId, cancellationToken);

        return existingJob?.Status is not (JobStatus.Enqueued or JobStatus.InProgress);
    }

    private async Task<Guid> GetJobIdAsync<TJobParams, TJobState>(JobRequest<TJobParams, TJobState> request, CancellationToken cancellationToken)
        where TJobParams : IJobParams
        where TJobState : IJobState
    {
        if (request.IsRestart && request.JobId != Guid.Empty)
        {
            return request.JobId;
        }

        var newGuid = await guidGenerator.GenerateGuidAsync(cancellationToken);
        request.JobId = newGuid;

        return request.JobId;
    }

    private Task NotifyJobStartedAsync(Guid jobId, Guid jobRegistrationId, CancellationToken token)
        => publisher.PublishJobStartedEvent(
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

        await publisher.PublishWatchJobEventAsync(watchEvent, watchInterval, cancellationToken);
    }

    private async Task<JobInfo<TJobParams, TJobState>> UpdateAttemptsOrCreateJobAsync<TJobParams, TJobState>(
        JobRequest<TJobParams, TJobState> request,
        CancellationToken cancellationToken)
        where TJobParams : IJobParams
        where TJobState : IJobState
    {
        JobInfo<TJobParams, TJobState> jobInfo;

        if (request.IsRestart && request.JobId != Guid.Empty)
        {
            logger.LogDebug("Updating number of tries for job. JobId: {JobId}. JobName: {JobName} Tries : {Tries}",
                request.JobId,
                request.JobName,
                request.NumberOfTries);
            jobInfo = await jobStore.SetJobAttempts<TJobParams, TJobState>(request.JobId, request.NumberOfTries, cancellationToken);
        }
        else
        {
            jobInfo = await jobStore.AddJobAsync(request, cancellationToken);
            logger.LogDebug("Created job. JobId: {JobId} {JobName}", jobInfo.Id, jobInfo.JobName);
        }

        return jobInfo;
    }

    /// <summary>
    ///     Runs the requested job as a task, publishes messages, and updates initial state
    /// </summary>
    /// <param name="registration">
    ///    The job registration
    /// </param>
    /// <param name="jobType">
    ///     The job type to resolve from the IoC container
    /// </param>
    /// <param name="context">
    ///     The job start context containing job run parameters.
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
    private async Task RunJobAsync<TJobParams, TJobState>(JobRegistration registration,
        Type jobType,
        JobStartContext<TJobParams, TJobState> context,
        CancellationToken cancellationToken)
        where TJobParams : IJobParams
        where TJobState : IJobState
    {
        if (scopeFactory.TryCreateScope(out var scope))
        {
            var job = CreateJobInstance<TJobParams, TJobState>(registration, jobType, scope);

            if (job is null)
            {
                return;
            }

            var jobRunner = scope.ServiceProvider.GetRequiredService<IJobRunner>();
            await Task.Run(async () =>
            {
                await jobRunner.RunJobAsync(job, context, cancellationToken);
                scope.Dispose();
            }, cancellationToken);
        }
    }

    private IJob<TJobParams, TJobState> CreateJobInstance<TJobParams, TJobState>(
        JobRegistration registration,
        Type jobType,
        IServiceScope scope)
        where TJobParams : IJobParams
        where TJobState : IJobState
    {
        var instance = scope.ServiceProvider.Materialize(registration.JobType);

        if (instance is null)
        {
            logger.LogCritical("Could not materialize job from service provider JobType {JobType}", jobType);
            return null;
        }

        if (instance is not IJob<TJobParams, TJobState> job)
        {
            logger.LogCritical("Instance type {Type} does not match Job Type {JobType}", instance.GetType(), jobType);
            return null;
        }

        return job;
    }

    private static JobStartContext<TJobParams, TJobState> GetJobStartContext<TJobParams, TJobState>(
        JobRequest<TJobParams, TJobState> request,
        JobInfoBase jobInfo,
        JobRegistration jobRegistration)
        where TJobParams : IJobParams
        where TJobState : IJobState => new()
        {
            JobId = jobInfo.Id,
            JobParameters = request.JobParameters ?? (TJobParams)jobRegistration.DefaultParams,
            JobState = request.InitialJobState ?? (TJobState)jobRegistration.DefaultState,
            StartTime = jobInfo.EnqueuedTime,
            IsRestart = request.IsRestart,
            LastProgressDate = jobInfo.LastProgressDate,
            LastProgressPercentage = jobInfo.LastProgressPercentage,
            CurrentNumberOfTries = request.NumberOfTries,
            JobRegistration = jobRegistration,
        };
}
