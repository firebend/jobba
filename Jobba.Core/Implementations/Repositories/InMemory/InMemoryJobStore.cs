using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Extensions;
using Jobba.Core.Interfaces;
using Jobba.Core.Interfaces.Repositories;
using Jobba.Core.Models;
using Jobba.Core.Models.Entities;

namespace Jobba.Core.Implementations.Repositories.InMemory;

internal static class InMemoryJobStoreCache
{
    public static ConcurrentDictionary<Guid, JobEntity> Jobs { get; } = new();
}

public class InMemoryJobStore : IJobStore
{
    private static JobEntity FindJobById(Guid id) => InMemoryJobStoreCache.Jobs.TryGetValue(id, out var entity) ? entity : null;

    private static JobEntity ModifyJob(Guid id, Action<JobEntity> act)
    {
        var entity = FindJobById(id);

        if (entity is not null)
        {
            act(entity);
        }

        return entity;
    }

    public Task<JobInfo<TJobParams, TJobState>> AddJobAsync<TJobParams, TJobState>(JobRequest<TJobParams, TJobState> jobRequest,
        CancellationToken cancellationToken)
        where TJobParams : IJobParams
        where TJobState : IJobState
    {
        jobRequest.JobId = jobRequest.JobId.Coalesce();
        var entity = InMemoryJobStoreCache.Jobs.GetOrAdd(jobRequest.JobId, static (_, request) => JobEntity.FromRequest(request), jobRequest);
        var info = entity?.ToJobInfo<TJobParams, TJobState>();
        return Task.FromResult(info);
    }

    public Task<JobInfo<TJobParams, TJobState>> SetJobAttempts<TJobParams, TJobState>(Guid jobId, int attempts, CancellationToken cancellationToken)
        where TJobParams : IJobParams
        where TJobState : IJobState
    {
        var entity = ModifyJob(jobId, x => x.CurrentNumberOfTries += 1);
        var info = entity?.ToJobInfo<TJobParams, TJobState>();
        return Task.FromResult(info);
    }

    public Task SetJobStatusAsync(Guid jobId, JobStatus status, DateTimeOffset date, CancellationToken cancellationToken)
    {
        ModifyJob(jobId, x =>
        {
            x.Status = status;
            x.LastProgressDate = date;
        });

        return Task.CompletedTask;
    }

    public Task LogFailureAsync(Guid jobId, Exception ex, CancellationToken cancellationToken)
    {
        ModifyJob(jobId, x =>
        {
            x.Status = JobStatus.Faulted;
            x.FaultedReason = ex.ToString();
        });

        return Task.CompletedTask;
    }

    public Task<JobInfoBase> GetJobByIdAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var found = FindJobById(jobId);
        return Task.FromResult(found?.ToJobInfoBase());
    }

    public Task<JobInfo<TJobParams, TJobState>> GetJobByIdAsync<TJobParams, TJobState>(Guid jobId, CancellationToken cancellationToken)
        where TJobParams : IJobParams
        where TJobState : IJobState
    {
        var found = FindJobById(jobId);
        return Task.FromResult(found?.ToJobInfo<TJobParams, TJobState>());
    }
}
