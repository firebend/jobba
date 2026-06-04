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

public class JobbaMongoJobProgressStore : IJobProgressStore
{
    private readonly IJobbaGuidGenerator _guidGenerator;
    private readonly IJobEventDispatcher _dispatcher;
    private readonly IJobbaMongoRepository<JobEntity> _jobRepository;
    private readonly IJobbaMongoRepository<JobProgressEntity> _repository;
    private readonly JobSystemInfo _systemInfo;

    public JobbaMongoJobProgressStore(IJobbaMongoRepository<JobProgressEntity> repository,
        IJobEventDispatcher dispatcher,
        IJobbaMongoRepository<JobEntity> jobRepository,
        IJobbaGuidGenerator guidGenerator,
        IJobSystemInfoProvider systemInfoProvider)
    {
        _repository = repository;
        _dispatcher = dispatcher;
        _jobRepository = jobRepository;
        _guidGenerator = guidGenerator;
        _systemInfo = systemInfoProvider.GetSystemInfo();
    }

    public async Task LogProgressAsync<TJobState>(JobProgress<TJobState> jobProgress, CancellationToken cancellationToken)
        where TJobState : IJobState
    {
        var entity = JobProgressEntity.FromJobProgress(jobProgress);
        entity.Id = await _guidGenerator.GenerateGuidAsync(cancellationToken);

        var added = await _repository.AddAsync(entity, cancellationToken);

        var jobEntity = await _jobRepository.GetFirstOrDefaultAsync(
            x => x.Id == added.JobId && x.SystemInfo.SystemMoniker == _systemInfo.SystemMoniker,
            cancellationToken);
        if (jobEntity is not null)
        {
            var jobType = Type.GetType(jobEntity.JobType);
            if (jobType is not null)
            {
                await _dispatcher.PublishProgressAsync(jobType, added.Id, added.JobId, added.JobRegistrationId, cancellationToken);
            }
        }

        var update = Builders<JobEntity>
            .Update
            .Set(x => x.JobState, jobProgress.JobState)
            .Set(x => x.LastProgressDate, added.Date)
            .Set(x => x.LastProgressPercentage, added.Progress);

        await _jobRepository.UpdateAsync(
            x => x.Id == jobProgress.JobId && x.SystemInfo.SystemMoniker == _systemInfo.SystemMoniker,
            update,
            cancellationToken);
    }

    public Task<JobProgressEntity> GetProgressById(Guid id, CancellationToken cancellationToken)
        => _repository.GetFirstOrDefaultAsync(x => x.Id == id, cancellationToken);
}
