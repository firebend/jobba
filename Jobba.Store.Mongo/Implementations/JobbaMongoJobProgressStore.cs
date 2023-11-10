using System;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Events;
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
    private readonly IJobEventPublisher _jobEventPublisher;
    private readonly IJobbaMongoRepository<JobEntity> _jobRepository;
    private readonly IJobbaMongoRepository<JobProgressEntity> _repository;

    public JobbaMongoJobProgressStore(IJobbaMongoRepository<JobProgressEntity> repository,
        IJobEventPublisher jobEventPublisher,
        IJobbaMongoRepository<JobEntity> jobRepository,
        IJobbaGuidGenerator guidGenerator)
    {
        _repository = repository;
        _jobEventPublisher = jobEventPublisher;
        _jobRepository = jobRepository;
        _guidGenerator = guidGenerator;
    }

    public async Task LogProgressAsync<TJobState>(JobProgress<TJobState> jobProgress, CancellationToken cancellationToken)
    {
        var entity = JobProgressEntity.FromJobProgress(jobProgress);
        entity.Id = await _guidGenerator.GenerateGuidAsync(cancellationToken);

        var added = await _repository.AddAsync(entity, cancellationToken);

        await _jobEventPublisher.PublishJobProgressEventAsync(
            new JobProgressEvent(added.Id, added.JobId, added.JobRegistrationId),
            cancellationToken);

        var update = Builders<JobEntity>
            .Update
            .Set(x => x.JobState, jobProgress.JobState)
            .Set(x => x.LastProgressDate, added.Date)
            .Set(x => x.LastProgressPercentage, added.Progress);

        await _jobRepository.UpdateAsync(jobProgress.JobId, update, cancellationToken);
    }

    public Task<JobProgressEntity> GetProgressById(Guid id, CancellationToken cancellationToken)
        => _repository.GetFirstOrDefaultAsync(x => x.Id == id, cancellationToken);
}
