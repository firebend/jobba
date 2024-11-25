using System;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Events;
using Jobba.Core.Interfaces;
using Jobba.Core.Interfaces.Repositories;
using Jobba.Core.Models;
using Microsoft.Extensions.Logging;

namespace Jobba.Core.Implementations;

public class DefaultJobRunner(
    ILogger<DefaultJobRunner> logger,
    IJobStore jobStore,
    IJobEventPublisher publisher,
    IJobCancellationTokenStore jobCancellationTokenStore) : IJobRunner
{
    public async Task RunJobAsync<TJobParams, TJobState>(
        IJob<TJobParams, TJobState> job,
        JobStartContext<TJobParams, TJobState> context,
        CancellationToken cancellationToken) where TJobParams : IJobParams where TJobState : IJobState
    {
        var jobCancellationToken = jobCancellationTokenStore.CreateJobCancellationToken(context.JobId, cancellationToken);
        await jobStore.SetJobStatusAsync(context.JobId, JobStatus.InProgress, DateTimeOffset.UtcNow, default);

        try
        {
            await job.StartAsync(context, jobCancellationToken);

            if (jobCancellationToken.IsCancellationRequested || cancellationToken.IsCancellationRequested)
            {
                await OnJobCancelledAsync(context.JobId, cancellationToken.IsCancellationRequested, default);
            }
            else
            {
                await OnJobCompletedAsync(context.JobId, context.JobRegistration.Id, job.JobName, default);
                jobCancellationTokenStore.RemoveCompletedJob(context.JobId);
            }
        }
        catch (TaskCanceledException)
        {
            if (jobCancellationToken.IsCancellationRequested || cancellationToken.IsCancellationRequested)
            {
                await OnJobCancelledAsync(context.JobId, cancellationToken.IsCancellationRequested,
                    default);
            }
        }
        catch (Exception ex)
        {
            await OnJobFaulted(context.JobId, context.JobRegistration.Id, ex);
        }
    }


    private async Task OnJobFaulted(Guid jobId, Guid jobRegistrationId, Exception ex)
    {
        logger.LogDebug("Job Faulted. JobId: {JobId}. Message: {ExceptionMessage}", jobId, ex.Message);

        await jobStore.LogFailureAsync(jobId, ex, default);

        await publisher.PublishJobFaultedEventAsync(
            new JobFaultedEvent(jobId, jobRegistrationId),
            default);
    }

    private Task OnJobCancelledAsync(Guid jobId, bool wasForced, CancellationToken cancellationToken)
    {
        logger.LogDebug("Job Cancelled. JobId: {JobId} WasForced: {WasForced}", jobId, wasForced);

        return jobStore.SetJobStatusAsync(jobId, wasForced ? JobStatus.ForceCancelled : JobStatus.Cancelled,
            DateTimeOffset.UtcNow, cancellationToken);
    }

    private async Task OnJobCompletedAsync(Guid jobId, Guid jobRegistrationId, string jobName,
        CancellationToken cancellationToken)
    {
        logger.LogDebug("Job Completed. Id: {JobId} Name: {Name}", jobId, jobName);

        await jobStore.SetJobStatusAsync(jobId, JobStatus.Completed, DateTimeOffset.UtcNow, cancellationToken);

        logger.LogDebug("Publishing job completed event. JobId: {JobId}", jobId);

        await publisher.PublishJobCompletedEventAsync(
            new JobCompletedEvent(jobId, jobRegistrationId),
            cancellationToken);
    }
}
