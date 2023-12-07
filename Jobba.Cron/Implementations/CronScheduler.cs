using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
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
        IJobLockService lockService)
    {
        _scheduler = scheduler;
        _logger = logger;
        _cronService = cronService;
        _jobRegistrationStore = jobRegistrationStore;
        _lockService = lockService;
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

        var tasks = jobs
            .Select(x => InvokeJobUsingReflectionAsync(x.Registration, cancellationToken))
            .ToArray();

        await Task.WhenAll(tasks);

        await UpdateJobRegistrationStoreWithNextExecutionAsync(jobs, cancellationToken);
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

    private async Task InvokeJobUsingReflectionAsync(JobRegistration registration, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Job is set for execution. {JobName} {CronExpression} {Start}",
            registration.JobName,
            registration.CronExpression,
            DateTimeOffset.UtcNow);

        var methodInfo = typeof(CronScheduler)
            .GetMethod(nameof(EnqueueJobAsync), BindingFlags.NonPublic | BindingFlags.Static)
            ?.MakeGenericMethod(registration.JobParamsType, registration.JobStateType);

        if (methodInfo is null)
        {
            _logger.LogCritical("Could not find method info for {Method}", nameof(EnqueueJobAsync));
            return;
        }

        var methodInfoParameters = new object[]
        {
            _scheduler,
            registration,
            cancellationToken
        };

        var enqueueTask = methodInfo.Invoke(this, methodInfoParameters);

        if (enqueueTask is Task task)
        {
            await task;
        }
    }

    private DateTimeOffset? GetNextExecutionDate(string cron, TimeZoneInfo timeZoneInfo, DateTimeOffset? start = null)
    {
        start ??= DateTimeOffset.UtcNow;

        var next = _cronService.GetNextExecutionDate(cron, start.Value, timeZoneInfo);

        return next?.TrimSeconds();
    }

    public static bool ShouldExecute(DateTimeOffset? previous,
        DateTimeOffset? next,
        DateTimeOffset windowMin,
        DateTimeOffset windowMax)
    {
        var isInWindow = next >= windowMin && next <= windowMax;

        //********************************************
        // Author: JMA
        // Date: 2023-07-20 03:36:12
        // Comment: Job has not previously executed, we should invoke it
        //*******************************************
        if (previous is null)
        {
            return isInWindow;
        }

        //********************************************
        // Author: JMA
        // Date: 2023-07-20 03:39:37
        // Comment: If we are in the window of execution, make sure our previous execution is old enough
        // this will prevent the hosted service from triggering jobs too frequently
        //*******************************************
        var should = previous <= windowMin && isInWindow;

        return should;
    }

    private async Task<List<JobExecutionInfo>> GetCronJobsAsync(CronSchedulerContext context, CancellationToken cancellationToken)
    {
        var jobs = new List<JobExecutionInfo>();

        var registrations = await _jobRegistrationStore.GetJobsWithCronExpressionsAsync(cancellationToken);

        foreach (var registry in registrations)
        {
            var cron = registry.CronExpression;
            var previous = registry.PreviousExecutionDate;
            var currentExecutionDate = GetNextExecutionDate(cron, registry.TimeZoneInfo, context.Max);
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

    private static Task EnqueueJobAsync<TJobParams, TJobState>(IJobScheduler jobScheduler,
        JobRegistration jobRegistration,
        CancellationToken cancellationToken)
        where TJobParams : IJobParams
        where TJobState : IJobState
        => jobScheduler.ScheduleJobAsync(jobRegistration.Id,
            jobRegistration.DefaultParams == default ? default : (TJobParams)jobRegistration.DefaultParams,
            jobRegistration.DefaultState == default ? default : (TJobState)jobRegistration.DefaultState,
            cancellationToken);
}
