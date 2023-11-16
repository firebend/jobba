using System;
using System.Collections.Generic;
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

    private record JobExecutionInfo(JobRegistration Registration,
        DateTimeOffset? NextExecutionDate,
        DateTimeOffset? PreviousExecutionDate)
    {
        public override string ToString() => $"{Registration?.JobName} {NextExecutionDate} {PreviousExecutionDate}";
    };

    public CronScheduler(IJobScheduler scheduler,
        ILogger<CronScheduler> logger,
        ICronService cronService,
        IJobRegistrationStore jobRegistrationStore)
    {
        _scheduler = scheduler;
        _logger = logger;
        _cronService = cronService;
        _jobRegistrationStore = jobRegistrationStore;
    }

    public async Task EnqueueJobsAsync(DateTimeOffset min, DateTimeOffset max, CancellationToken cancellationToken)
    {
        var jobs = await GetCronJobsAsync(min, max, cancellationToken);

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

    private DateTimeOffset? GetNextExecutionDate(string cron, DateTimeOffset? start = null)
    {
        start ??= DateTimeOffset.UtcNow;

        var next = _cronService.GetNextExecutionDate(cron, start.Value);

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

    private async Task<List<JobExecutionInfo>> GetCronJobsAsync(DateTimeOffset min,
        DateTimeOffset max,

        CancellationToken cancellationToken)
    {
        var jobs = new List<JobExecutionInfo>();

        var registrations = await _jobRegistrationStore.GetJobsWithCronExpressionsAsync(cancellationToken);

        foreach (var registry in registrations)
        {
            var cron = registry.CronExpression;
            var previous = registry.PreviousExecutionDate;
            var currentExecutionDate = GetNextExecutionDate(cron, max);
            var shouldExecute = ShouldExecute(previous, currentExecutionDate, min, max);

            if (shouldExecute is false)
            {
                continue;
            }

            var nextExecutionDate = GetNextExecutionDate(cron, currentExecutionDate.GetValueOrDefault().AddSeconds(1));

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
