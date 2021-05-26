using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Interfaces.Repositories;
using Jobba.Core.Models;
using Jobba.Core.Models.Entities;
using Jobba.Store.Mongo.Interfaces;

namespace Jobba.Store.Mongo.Implementations
{
    public class JobbaMongoJobListStore : IJobListStore
    {
        private readonly IJobbaMongoRepository<JobEntity> _repository;

        public JobbaMongoJobListStore(IJobbaMongoRepository<JobEntity> repository)
        {
            _repository = repository;
        }

        private async Task<IEnumerable<JobInfoBase>> GetJobInfoBases(Expression<Func<JobEntity, bool>> filter, CancellationToken cancellationToken)
        {
            var activeJobs = await _repository
                .GetAllAsync(filter, cancellationToken);

            if (activeJobs == null)
            {
                return Enumerable.Empty<JobInfoBase>();
            }

            var jobInfoBases =  activeJobs.Select(x => x.ToJobInfoBase()).ToArray();

            return jobInfoBases;
        }

        public Task<IEnumerable<JobInfoBase>> GetActiveJobs(CancellationToken cancellationToken)
            => GetJobInfoBases(x => x.Status == JobStatus.InProgress || x.Status == JobStatus.Enqueued, cancellationToken);

        public Task<IEnumerable<JobInfoBase>> GetJobsToRetry(CancellationToken cancellationToken)
            => GetJobInfoBases(x => x.Status == JobStatus.Faulted && x.MaxNumberOfTries > x.CurrentNumberOfTries, cancellationToken);
    }
}
