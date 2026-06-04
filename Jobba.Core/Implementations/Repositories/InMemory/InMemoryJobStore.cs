using System;
using System.Collections.Concurrent;
using System.Linq;
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
    public static readonly object ReclaimLock = new();
}

public class InMemoryJobStore : IJobStore
{
    private readonly IJobRegistrationStore _jobRegistrationStore;
    private readonly IJobSystemInfoProvider _jobSystemInfoProvider;

    public InMemoryJobStore(IJobRegistrationStore jobRegistrationStore, IJobSystemInfoProvider jobSystemInfoProvider)
    {
        _jobRegistrationStore = jobRegistrationStore;
        _jobSystemInfoProvider = jobSystemInfoProvider;
    }

    private static JobEntity FindJobById(Guid id) => InMemoryJobStoreCache.Jobs.TryGetValue(id, out var entity) ? entity : null;

    private static JobEntity ModifyJob(Guid id, Action<JobEntity> act)
    {
        if (!InMemoryJobStoreCache.Jobs.TryGetValue(id, out var entity))
        {
            return null;
        }

        act(entity);
        return entity;
    }

    public async Task<JobInfo<TJobParams, TJobState>> AddJobAsync<TJobParams, TJobState>(JobRequest<TJobParams, TJobState> jobRequest,
        CancellationToken cancellationToken)
        where TJobParams : IJobParams
        where TJobState : IJobState
    {
        if (string.IsNullOrWhiteSpace(jobRequest.JobName))
        {
            throw new ArgumentException("Job name cannot be null or whitespace.", nameof(jobRequest));
        }

        var registration = await _jobRegistrationStore.GetByJobNameAsync(jobRequest.JobName, cancellationToken)
                           ?? throw new Exception($"Job registration not found for JobName {jobRequest.JobName}");

        var systemInfo = _jobSystemInfoProvider.GetSystemInfo();

        jobRequest.JobId = jobRequest.JobId.Coalesce();

        var entity = InMemoryJobStoreCache.Jobs.GetOrAdd(
            jobRequest.JobId,
            static (_, args)
                => JobEntity.FromRequest(args.jobRequest, args.registration.Id, args.systemInfo),
            (jobRequest, registration, systemInfo));

        var info = entity?.ToJobInfo<TJobParams, TJobState>();

        return info;
    }

    public Task<JobInfo<TJobParams, TJobState>> SetJobAttempts<TJobParams, TJobState>(Guid jobId, int attempts, CancellationToken cancellationToken)
        where TJobParams : IJobParams
        where TJobState : IJobState
    {
        var entity = ModifyJob(jobId, x => x.CurrentNumberOfTries = attempts);
        var info = entity?.ToJobInfo<TJobParams, TJobState>();
        return Task.FromResult(info);
    }

    public Task SetJobAttempts(Guid jobId, int attempts, CancellationToken cancellationToken)
    {
        ModifyJob(jobId, x => x.CurrentNumberOfTries = attempts);
        return Task.CompletedTask;
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

    public Task SetHeartbeatAsync(Guid jobId, DateTimeOffset heartbeatTime, CancellationToken cancellationToken)
    {
        ModifyJob(jobId, x => x.LastHeartbeatTime = heartbeatTime);
        return Task.CompletedTask;
    }

    public Task<int> ReclaimOrphanedJobsAsync(int staleMultiplier, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var systemInfo = _jobSystemInfoProvider.GetSystemInfo();

        lock (InMemoryJobStoreCache.ReclaimLock)
        {
            var candidates = InMemoryJobStoreCache.Jobs.Values
                .Where(x => x.SystemInfo.SystemMoniker == systemInfo.SystemMoniker)
                .Where(x => x.Status == JobStatus.InProgress)
                .Where(x => x.LastHeartbeatTime.HasValue)
                .Where(x => x.LastHeartbeatTime!.Value.Add(x.JobWatchInterval * staleMultiplier) < now)
                .Select(x => (x.Id, OriginalHeartbeat: x.LastHeartbeatTime))
                .ToList();

            var reclaimed = 0;
            foreach (var (id, originalHeartbeat) in candidates)
            {
                if (InMemoryJobStoreCache.Jobs.TryGetValue(id, out var currentJob))
                {
                    currentJob.Status = JobStatus.Faulted;
                    currentJob.FaultedReason = JobbaCoreOptions.OrphanedJobFaultedReason;
                    reclaimed++;
                }
            }

            return Task.FromResult(reclaimed);
        }
    }
}
