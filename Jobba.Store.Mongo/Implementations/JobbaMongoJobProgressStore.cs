using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Events;
using Jobba.Core.Interfaces;
using Jobba.Core.Interfaces.Repositories;
using Jobba.Core.Models;
using Jobba.Core.Models.Entities;
using Jobba.Store.Mongo.Interfaces;
using Microsoft.AspNetCore.JsonPatch;

namespace Jobba.Store.Mongo.Implementations
{
    public class JobbaMongoJobProgressStore : IJobProgressStore
    {
        private readonly IJobbaMongoRepository<JobEntity> _jobRepository;
        private readonly IJobbaMongoRepository<JobProgressEntity> _repository;
        private readonly IJobEventPublisher _jobEventPublisher;

        public JobbaMongoJobProgressStore(IJobbaMongoRepository<JobProgressEntity> repository,
            IJobEventPublisher jobEventPublisher,
            IJobbaMongoRepository<JobEntity> jobRepository)
        {
            _repository = repository;
            _jobEventPublisher = jobEventPublisher;
            _jobRepository = jobRepository;
        }

        public async Task LogProgressAsync<TJobState>(JobProgress<TJobState> jobProgress, CancellationToken cancellationToken)
        {
            var entity = JobProgressEntity.FromJobProgress(jobProgress);

            var added = await _repository.AddAsync(entity, cancellationToken);

            await _jobEventPublisher.PublishJobProgressEventAsync(new JobProgressEvent(added.Id, added.JobId), cancellationToken);

            //todo: add this in a test
            var statePatch = new JsonPatchDocument<JobEntity>();
            statePatch.Replace(x => x.JobState, jobProgress.JobState);

            await _jobRepository.UpdateAsync(jobProgress.JobId, statePatch, cancellationToken);
        }
    }
}
