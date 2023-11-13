using System;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Interfaces;
using Jobba.Core.Interfaces.Repositories;
using Jobba.Core.Models;

namespace Jobba.Core.Implementations;

public class DefaultJobOrchestrationService : IJobOrchestrationService
{
    private readonly IJobRegistrationStore _jobRegistrationStore;
    private readonly IJobScheduler _jobScheduler;

    public DefaultJobOrchestrationService(IJobRegistrationStore jobRegistrationStore, IJobScheduler jobScheduler)
    {
        _jobRegistrationStore = jobRegistrationStore;
        _jobScheduler = jobScheduler;
    }
    public async Task<JobOrchestrationResult> OrchestrateJobAsync<TJob, TParams, TState>(JobOrchestrationRequest<TJob, TParams, TState> request,
        CancellationToken cancellationToken)
        where TJob : IJob<TParams, TState>
        where TParams : IJobParams
        where TState : IJobState
    {
        var registration = JobRegistration.FromTypes<TJob, TParams, TState>(
            request.JobName,
            request.Description,
            request.Cron,
            request.DefaultJobParams,
            request.DefaultJobState);

        var created = await _jobRegistrationStore.RegisterJobAsync(registration, cancellationToken);

        if (string.IsNullOrWhiteSpace(request.Cron) is false)
        {
            //********************************************
            // Author: JMA
            // Date: 2023-11-11 03:51:08
            // Comment: if this is a new cron expression job
            // add the registration and the cron scheduler will kick off the job when needed
            //*******************************************
            return new(registration, null);
        }

        var jobInfo = await _jobScheduler.ScheduleJobAsync<TParams, TState>(
            created.Id,
            request.DefaultJobParams,
            request.DefaultJobState,
            cancellationToken);

        return new(registration, jobInfo);
    }

    public Task<JobRegistration> DeleteJobRegistrationAsync(Guid jobRegistrationId, CancellationToken cancellationToken)
        => _jobRegistrationStore.RemoveByIdAsync(jobRegistrationId, cancellationToken);
}
