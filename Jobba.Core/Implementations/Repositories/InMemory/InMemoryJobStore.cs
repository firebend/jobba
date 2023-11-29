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
        var entity = FindJobById(id);

        if (entity is not null)
        {
            act(entity);
        }

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
