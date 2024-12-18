using System;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Interfaces;
using Jobba.Core.Interfaces.Repositories;
using Jobba.Core.Models;

namespace Jobba.Core.Implementations;

public class DefaultJobOrchestrationService(IJobSystemInfoProvider systemInfoProvider, IJobRegistrationStore jobRegistrationStore, IJobScheduler jobScheduler)
    : IJobOrchestrationService
{
    public async Task<JobOrchestrationResult> OrchestrateJobAsync<TJob, TParams, TState>(JobOrchestrationRequest<TJob, TParams, TState> request,
        CancellationToken cancellationToken)
        where TJob : IJob<TParams, TState>
        where TParams : IJobParams
        where TState : IJobState
    {
        var registration = JobRegistration.FromTypes<TJob, TParams, TState>(
            systemInfoProvider.GetSystemInfo().SystemMoniker,
            request.JobName,
            request.Description,
            request.Cron,
            request.DefaultJobParams,
            request.DefaultJobState,
            request.IsInactive,
            request.TimeZone);

        var created = await jobRegistrationStore.RegisterJobAsync(registration, cancellationToken);

        if (string.IsNullOrWhiteSpace(request.Cron) is false)
        {
            //********************************************
            // Author: JMA
            // Date: 2023-11-11 03:51:08
            // Comment: if this is a new cron expression job
            // add the registration and the cron scheduler will kick off the job when needed
            //*******************************************
            return new(created, null);
        }

        if (request.IsInactive)
        {
            //********************************************
            // Author: JMA
            // Date: 2023-12-05 05:00:29
            // Comment: if the job is defaulted to not be enabled yet, do not run it
            //*******************************************
            return new(created, null);
        }

        var jobInfo = await jobScheduler.ScheduleJobAsync(
            created.Id,
            request.DefaultJobParams,
            request.DefaultJobState,
            cancellationToken);

        return new(created, jobInfo);
    }

    public Task<JobRegistration> DeleteJobRegistrationAsync(Guid jobRegistrationId, CancellationToken cancellationToken)
        => jobRegistrationStore.RemoveByIdAsync(jobRegistrationId, cancellationToken);

    public Task<JobRegistration> SetJobRegistrationInactiveAsync(Guid jobRegistrationId, bool isInactive, CancellationToken cancellationToken)
        => jobRegistrationStore.SetIsInactiveAsync(jobRegistrationId, isInactive, cancellationToken);
}
