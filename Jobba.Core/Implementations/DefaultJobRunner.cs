using System;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Events;
using Jobba.Core.Interfaces;
using Jobba.Core.Interfaces.Repositories;
using Jobba.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jobba.Core.Implementations;

public class DefaultJobRunner(
    ILogger<DefaultJobRunner> logger,
    IJobStore jobStore,
    IJobEventPublisher publisher,
    IJobCancellationTokenStore jobCancellationTokenStore,
    IServiceScopeFactory serviceScopeFactory) : IJobRunner
{
    public async Task RunJobAsync<TJobParams, TJobState>(
        IJob<TJobParams, TJobState> job,
        JobStartContext<TJobParams, TJobState> context,
        CancellationToken cancellationToken) where TJobParams : IJobParams where TJobState : IJobState
    {
        var jobCancellationToken = jobCancellationTokenStore.CreateJobCancellationToken(context.JobId, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        await jobStore.SetHeartbeatAsync(context.JobId, now, CancellationToken.None);
        await jobStore.SetJobStatusAsync(context.JobId, JobStatus.InProgress, now, CancellationToken.None);

        using var heartbeatCts = CancellationTokenSource.CreateLinkedTokenSource(jobCancellationToken, cancellationToken);
        var heartbeatInterval = context.JobWatchInterval;
        var heartbeatTask = Task.Run(async () => await RunHeartbeatAsync(context.JobId, heartbeatInterval, heartbeatCts.Token));

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
        finally
        {
            try
            {
                heartbeatCts.Cancel();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error cancelling heartbeat token for job {JobId}", context.JobId);
            }

            var heartbeatTimeout = heartbeatInterval > TimeSpan.Zero
                ? heartbeatInterval * 2
                : TimeSpan.FromSeconds(5);

            try
            {
                await heartbeatTask.WaitAsync(heartbeatTimeout, CancellationToken.None);
            }
            catch (TimeoutException)
            {
                logger.LogWarning("Heartbeat task did not complete within {Timeout} for job {JobId}", heartbeatTimeout, context.JobId);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation succeeds
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error waiting for heartbeat task to complete for job {JobId}", context.JobId);
            }
            finally
            {
                jobCancellationTokenStore.RemoveCompletedJob(context.JobId);
            }
        }
    }

    private async Task RunHeartbeatAsync(Guid jobId, TimeSpan interval, CancellationToken cancellationToken)
    {
        logger.LogDebug("Starting heartbeat loop for job {JobId} with interval {Interval}", jobId, interval);

        if (interval <= TimeSpan.Zero)
        {
            logger.LogWarning("Heartbeat interval must be greater than zero for job {JobId}. Heartbeat disabled.", jobId);
            return;
        }

        using var scope = serviceScopeFactory.CreateScope();
        var scopedStore = scope.ServiceProvider.GetRequiredService<IJobStore>();

        try
        {
            await Task.Delay(interval, cancellationToken);

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await scopedStore.SetHeartbeatAsync(jobId, DateTimeOffset.UtcNow, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Heartbeat write failed for job {JobId}; will retry next tick", jobId);
                }

                await Task.Delay(interval, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Heartbeat loop terminated unexpectedly for job {JobId}", jobId);
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
