using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Interfaces;
using Jobba.Core.Models;

namespace Jobba.Cron.Implementations;

public class CronJobTypeRegistry
{
    private readonly ConcurrentDictionary<(Type, Type, Type), Func<IJobScheduler, JobRegistration, CancellationToken, Task>> _enqueueHandlers = new();

    public void RegisterJobType<TJob, TJobParams, TJobState>()
        where TJob : IJob<TJobParams, TJobState>
        where TJobParams : IJobParams
        where TJobState : IJobState
    {
        _enqueueHandlers[(typeof(TJob), typeof(TJobParams), typeof(TJobState))] =
            EnqueueJobAsync<TJobParams, TJobState>;
    }

    public bool TryGetEnqueueHandler(
        Type jobType,
        Type paramsType,
        Type stateType,
        out Func<IJobScheduler, JobRegistration, CancellationToken, Task> handler)
        => _enqueueHandlers.TryGetValue((jobType, paramsType, stateType), out handler);

    private static Task EnqueueJobAsync<TJobParams, TJobState>(
        IJobScheduler jobScheduler,
        JobRegistration jobRegistration,
        CancellationToken cancellationToken)
        where TJobParams : IJobParams
        where TJobState : IJobState
        => jobScheduler.ScheduleJobAsync(
            jobRegistration.Id,
            jobRegistration.DefaultParams == default ? default : (TJobParams)jobRegistration.DefaultParams,
            jobRegistration.DefaultState == default ? default : (TJobState)jobRegistration.DefaultState,
            cancellationToken);
}
