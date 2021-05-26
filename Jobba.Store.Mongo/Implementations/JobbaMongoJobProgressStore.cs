using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Events;
using Jobba.Core.Interfaces;
using Jobba.Core.Interfaces.Repositories;
using Jobba.Core.Models;
using Jobba.Core.Models.Entities;
using Jobba.Store.Mongo.Interfaces;

namespace Jobba.Store.Mongo.Implementations
{
    public class JobbaMongoJobProgressStore : IJobProgressStore
    {
        private readonly IJobbaMongoRepository<JobProgressEntity> _repository;
        private readonly IJobEventPublisher _jobEventPublisher;

        public JobbaMongoJobProgressStore(IJobbaMongoRepository<JobProgressEntity> repository,
            IJobEventPublisher jobEventPublisher)
        {
            _repository = repository;
            _jobEventPublisher = jobEventPublisher;
        }

        public async Task LogProgressAsync<TJobState>(JobProgress<TJobState> jobProgress, CancellationToken cancellationToken)
        {
            var entity = JobProgressEntity.FromJobProgress(jobProgress);
            var added = await _repository.AddAsync(entity, cancellationToken);
            await _jobEventPublisher.PublishJobProgressEventAsync(new JobProgressEvent(added.Id, added.JobId), cancellationToken);
        }
    }
}
