using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Interfaces.Repositories;
using Jobba.Core.Models;

namespace Jobba.Core.Implementations.Repositories.InMemory;

public class InMemoryJobListStore : IJobListStore
{
    public Task<IEnumerable<JobInfoBase>> GetActiveJobs(CancellationToken cancellationToken)
        => Task.FromResult(InMemoryJobStoreCache.Jobs.Values
            .Where(RepositoryExpressions.JobsInProgressExpression.Compile())
            .Select(x => x.ToJobInfoBase()));

    public Task<IEnumerable<JobInfoBase>> GetJobsToRetry(CancellationToken cancellationToken)
        => Task.FromResult(InMemoryJobStoreCache.Jobs.Values
            .Where(RepositoryExpressions.JobsInProgressExpression.Compile())
            .Select(x => x.ToJobInfoBase()));
}
