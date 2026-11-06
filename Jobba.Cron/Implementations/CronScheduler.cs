using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Interfaces;
using Jobba.Core.Interfaces.Repositories;
using Jobba.Core.Models;
using Jobba.Cron.Extensions;
using Jobba.Cron.Interfaces;
using Microsoft.Extensions.Logging;

namespace Jobba.Cron.Implementations;


public class CronScheduler : ICronScheduler
{
    private readonly ILogger<CronScheduler> _logger;
    private readonly IJobScheduler _scheduler;
    private readonly ICronService _cronService;
    private readonly IJobRegistrationStore _jobRegistrationStore;
    private readonly IJobLockService _lockService;
    private readonly CronJobTypeRegistry _cronRegistry;

    private record JobExecutionInfo(JobRegistration Registration,
        DateTimeOffset? NextExecutionDate,
        DateTimeOffset? PreviousExecutionDate)
    {
        public override string ToString()
            => $"JobName: {Registration?.JobName} Next Execution Date: {NextExecutionDate} Previous Execution Date: {PreviousExecutionDate}";
    };

    public CronScheduler(IJobScheduler scheduler,
        ILogger<CronScheduler> logger,
        ICronService cronService,
        IJobRegistrationStore jobRegistrationStore,
        IJobLockService lockService,
        CronJobTypeRegistry cronRegistry)
    {
        _scheduler = scheduler;
        _logger = logger;
        _cronService = cronService;
        _jobRegistrationStore = jobRegistrationStore;
        _lockService = lockService;
        _cronRegistry = cronRegistry;
    }

    public async Task EnqueueJobsAsync(CronSchedulerContext context, CancellationToken cancellationToken)
    {
        var systemLock = await _lockService.LockSystemAsync(
            context.SystemMoniker,
            TimeSpan.FromMilliseconds(100),
            cancellationToken);

        using (systemLock.Lock)
        {
            if (systemLock.WasLockAcquired is false)
            {
                _logger.LogDebug("Could not acquire lock for {SystemMoniker} another process must be scheduling cron jobs", context.SystemMoniker);
                return;
            }

            var start = Stopwatch.GetTimestamp();
            await DoJobScheduling(context, cancellationToken);
            await HoldLockAsync(context, start, cancellationToken);
        }
    }

    private async Task HoldLockAsync(CronSchedulerContext context, long start, CancellationToken cancellationToken)
    {
        var end = Stopwatch.GetTimestamp();
        var duration = Stopwatch.GetElapsedTime(start, end);
        var diffTime = context.Interval - duration;
        var holdTime = diffTime - TimeSpan.FromMilliseconds(100);

        _logger.LogDebug("Holding lock for {HoldTime}", holdTime);

        if (holdTime > TimeSpan.Zero)
        {
            await Task.Delay(holdTime, cancellationToken);
        }
        else
        {
            _logger.LogDebug("Hold time was negative, not holding lock. {HoldTime}", holdTime);
        }
    }

    private async Task DoJobScheduling(CronSchedulerContext context, CancellationToken cancellationToken)
    {
        var jobs = await GetCronJobsAsync(context, cancellationToken);

        if (jobs.Count <= 0)
        {
            _logger.LogDebug("No jobs to execute at this time");
            return;
        }

        var results = await Task.WhenAll(jobs.Select(x => TryInvokeJobAsync(x, cancellationToken)));

        // Save the run dates of every job that was queued, even when another job failed. Otherwise a queued
        // occurrence on a shared window boundary would be queued again on the next tick.
        var queued = jobs.Where((_, i) => results[i]).ToList();

        await UpdateJobRegistrationStoreWithNextExecutionAsync(queued, cancellationToken);
    }

    private async Task<bool> TryInvokeJobAsync(JobExecutionInfo job, CancellationToken cancellationToken)
    {
        try
        {
            await InvokeJobAsync(job.Registration, cancellationToken);
            return true;
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            _logger.LogError(e, "Failed to enqueue cron job. {JobName} {JobRegistrationId}",
                job.Registration.JobName,
                job.Registration.Id);
            return false;
        }
    }

    private Task UpdateJobRegistrationStoreWithNextExecutionAsync(IEnumerable<JobExecutionInfo> jobs, CancellationToken cancellationToken)
    {
        var tasks = jobs
            .Select(x =>
            _jobRegistrationStore.UpdateNextAndPreviousInvocationDatesAsync(x.Registration.Id,
                x.NextExecutionDate,
                x.PreviousExecutionDate,
                cancellationToken));

        return Task.WhenAll(tasks);
    }

    private async Task InvokeJobAsync(JobRegistration registration, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Job is set for execution. {JobName} {CronExpression} {Start}",
            registration.JobName,
            registration.CronExpression,
            DateTimeOffset.UtcNow);

        if (!_cronRegistry.TryGetEnqueueHandler(registration.JobType, out var handler))
        {
            _logger.LogCritical("No enqueue handler registered for job {JobType}. Was AddJob<> called?",
                registration.JobType);
            return;
        }

        await handler(_scheduler, registration, cancellationToken);
    }

    private DateTimeOffset? GetNextExecutionDate(string cron, TimeZoneInfo timeZoneInfo, DateTimeOffset? start = null)
    {
        start ??= DateTimeOffset.UtcNow;

        var next = _cronService.GetNextExecutionDate(cron, start.Value, timeZoneInfo);

        return next?.TrimSeconds();
    }

    // The occurrence must be in the window and later than the previous run. Consecutive windows share a
    // boundary, so an occurrence on it falls in both; the previous-run check keeps it from running twice.
    public static bool ShouldExecute(DateTimeOffset? previous,
        DateTimeOffset? next,
        DateTimeOffset windowMin,
        DateTimeOffset windowMax)
        => next >= windowMin && next <= windowMax && (previous is null || previous < next);

    private async Task<List<JobExecutionInfo>> GetCronJobsAsync(CronSchedulerContext context, CancellationToken cancellationToken)
    {
        var jobs = new List<JobExecutionInfo>();

        var registrations = await _jobRegistrationStore.GetJobsWithCronExpressionsAsync(cancellationToken);

        foreach (var registry in registrations)
        {
            var cron = registry.CronExpression;
            var previous = registry.PreviousExecutionDate;
            // The first occurrence at or after the window start. Starting from Max instead only finds an
            // occurrence when the window ends exactly on it, and ticks are rarely aligned to the minute.
            var currentExecutionDate = GetNextExecutionDate(cron, registry.TimeZoneInfo, context.Min);
            var shouldExecute = ShouldExecute(previous, currentExecutionDate, context.Min, context.Max);

            if (shouldExecute is false)
            {
                continue;
            }

            var nextExecutionDate = GetNextExecutionDate(
                cron,
                registry.TimeZoneInfo,
                currentExecutionDate.GetValueOrDefault().AddSeconds(1));

            var jobExecutionInfo = new JobExecutionInfo(registry, nextExecutionDate, currentExecutionDate);

            _logger.LogDebug("Job Context: {Context}", jobExecutionInfo);

            jobs.Add(jobExecutionInfo);
        }

        return jobs;
    }

}
