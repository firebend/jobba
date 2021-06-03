using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Extensions;
using Jobba.Core.Interfaces.Repositories;
using Jobba.Core.Models;
using Jobba.Core.Models.Entities;
using Jobba.Store.Mongo.Interfaces;

namespace Jobba.Store.Mongo.Implementations
{
    public class JobbaMongoJobListStore : IJobListStore
    {
        private readonly IJobbaMongoRepository<JobEntity> _repository;
        private static readonly Expression<Func<JobEntity, bool>> JobsFaultedExpression = x => x.Status == JobStatus.Faulted;
        //todo: you can't do this in mongo; we'll have to put a field that we query explicitly
        private static readonly Expression<Func<JobEntity, bool>> JobsTriesExpression = x =>  x.MaxNumberOfTries < x.CurrentNumberOfTries;
        private static readonly Expression<Func<JobEntity, bool>> JobsToRetryExpression = JobsFaultedExpression.AndAlso(JobsTriesExpression);
        private static readonly Expression<Func<JobEntity, bool>> JobsInProgressExpression =
            x => x.Status == JobStatus.InProgress || x.Status == JobStatus.Enqueued;

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
            => GetJobInfoBases(JobsInProgressExpression, cancellationToken);

        public Task<IEnumerable<JobInfoBase>> GetJobsToRetry(CancellationToken cancellationToken)
            => GetJobInfoBases(JobsToRetryExpression, cancellationToken);
    }
}
