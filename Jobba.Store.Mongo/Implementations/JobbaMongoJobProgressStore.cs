using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Interfaces.Repositories;
using Jobba.Core.Models;
using Jobba.Core.Models.Entities;
using Jobba.Store.Mongo.Interfaces;

namespace Jobba.Store.Mongo.Implementations
{
    public class JobbaMongoJobProgressStore : IJobProgressStore
    {
        private readonly IJobbaMongoRepository<JobProgressEntity> _repository;

        public JobbaMongoJobProgressStore(IJobbaMongoRepository<JobProgressEntity> repository)
        {
            _repository = repository;
        }

        public Task LogProgressAsync<TJobState>(JobProgress<TJobState> jobProgress, CancellationToken cancellationToken)
        {
            var entity = JobProgressEntity.FromJobProgress(jobProgress);
            return _repository.AddAsync(entity, cancellationToken);
        }
    }
}
