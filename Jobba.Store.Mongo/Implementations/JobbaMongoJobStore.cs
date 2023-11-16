using System;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Interfaces;
using Jobba.Core.Interfaces.Repositories;
using Jobba.Core.Models;
using Jobba.Core.Models.Entities;
using Jobba.Store.Mongo.Interfaces;
using MongoDB.Driver;

namespace Jobba.Store.Mongo.Implementations;

public class JobbaMongoJobStore : IJobStore
{
    private readonly IJobbaMongoRepository<JobEntity> _repository;
    private readonly IJobRegistrationStore _jobRegistrationStore;
    private readonly IJobSystemInfoProvider _systemInfoProvider;

    public JobbaMongoJobStore(IJobbaMongoRepository<JobEntity> repository,
        IJobRegistrationStore jobRegistrationStore,
        IJobSystemInfoProvider systemInfoProvider)
    {
        _repository = repository;
        _jobRegistrationStore = jobRegistrationStore;
        _systemInfoProvider = systemInfoProvider;
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

        var jobRegistration = await _jobRegistrationStore.GetByJobNameAsync(jobRequest.JobName, cancellationToken)
                              ?? throw new Exception($"Job registration not found for JobName {jobRequest.JobName}");

        var systemInfo = _systemInfoProvider.GetSystemInfo();
        var request = JobEntity.FromRequest(jobRequest, jobRegistration.Id, systemInfo);

        var added = await _repository.AddAsync(request, cancellationToken);
        var info = added.ToJobInfo<TJobParams, TJobState>();

        return info;
    }

    public async Task<JobInfo<TJobParams, TJobState>> SetJobAttempts<TJobParams, TJobState>(Guid jobId, int attempts, CancellationToken cancellationToken)
        where TJobParams : IJobParams
        where TJobState : IJobState
    {
        var updateDef = Builders<JobEntity>.Update
            .Set(x => x.CurrentNumberOfTries, attempts);

        var updated = await _repository.UpdateAsync(jobId, updateDef, cancellationToken);
        var info = updated.ToJobInfo<TJobParams, TJobState>();
        return info;
    }

    public async Task SetJobStatusAsync(Guid jobId, JobStatus status, DateTimeOffset date, CancellationToken cancellationToken)
    {
        var update = Builders<JobEntity>
            .Update
            .Set(x => x.Status, status)
            .Set(x => x.LastProgressDate, date);

        await _repository.UpdateAsync(jobId, update, cancellationToken);
    }

    public async Task LogFailureAsync(Guid jobId, Exception ex, CancellationToken cancellationToken)
    {
        var update = Builders<JobEntity>
            .Update
            .Set(x => x.FaultedReason, ex.ToString())
            .Set(x => x.Status, JobStatus.Faulted);

        await _repository.UpdateAsync(jobId, update, cancellationToken);
    }

    public async Task<JobInfoBase> GetJobByIdAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetFirstOrDefaultAsync(x => x.Id == jobId, cancellationToken);

        var jobInfoBase = entity?.ToJobInfoBase();
        return jobInfoBase;
    }

    public async Task<JobInfo<TJobParams, TJobState>> GetJobByIdAsync<TJobParams, TJobState>(Guid jobId, CancellationToken cancellationToken)
        where TJobParams : IJobParams
        where TJobState : IJobState
    {
        var entity = await _repository.GetFirstOrDefaultAsync(x => x.Id == jobId, cancellationToken);

        var jobInfo = entity?.ToJobInfo<TJobParams, TJobState>();
        return jobInfo;
    }
}
