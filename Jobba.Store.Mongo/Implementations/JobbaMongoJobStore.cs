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

    public JobbaMongoJobStore(IJobbaMongoRepository<JobEntity> repository)
    {
        _repository = repository;
    }

    public async Task<JobInfo<TJobParams, TJobState>> AddJobAsync<TJobParams, TJobState>(JobRequest<TJobParams, TJobState> jobRequest,
        CancellationToken cancellationToken)
        where TJobParams : IJobParams
        where TJobState : IJobState
    {
        var added = await _repository.AddAsync(JobEntity.FromRequest(jobRequest), cancellationToken);
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
