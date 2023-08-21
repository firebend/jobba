using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Implementations.Repositories;
using Jobba.Core.Interfaces.Repositories;
using Jobba.Core.Models;
using Jobba.Core.Models.Entities;
using Jobba.Store.Mongo.Interfaces;

namespace Jobba.Store.Mongo.Implementations;

public class JobbaMongoJobListStore : IJobListStore
{
    private readonly IJobbaMongoRepository<JobEntity> _repository;

    public JobbaMongoJobListStore(IJobbaMongoRepository<JobEntity> repository)
    {
        _repository = repository;
    }

    public Task<IEnumerable<JobInfoBase>> GetActiveJobs(CancellationToken cancellationToken)
        => GetJobInfoBases(RepositoryExpressions.JobsInProgressExpression, cancellationToken);

    public Task<IEnumerable<JobInfoBase>> GetJobsToRetry(CancellationToken cancellationToken)
        => GetJobInfoBases(RepositoryExpressions.JobRetryExpression, cancellationToken);

    private async Task<IEnumerable<JobInfoBase>> GetJobInfoBases(Expression<Func<JobEntity, bool>> filter, CancellationToken cancellationToken)
    {
        var activeJobs = await _repository
            .GetAllAsync(filter, cancellationToken);

        if (activeJobs == null)
        {
            return Enumerable.Empty<JobInfoBase>();
        }

        var jobInfoBases = activeJobs.Select(x => x.ToJobInfoBase()).ToArray();

        return jobInfoBases;
    }
}
