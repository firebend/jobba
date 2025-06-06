using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Interfaces;
using Jobba.Core.Interfaces.Repositories;
using Jobba.Core.Models;

namespace Jobba.Core.Implementations.Repositories.InMemory;

public class InMemoryJobListStore(IJobSystemInfoProvider systemInfoProvider) : IJobListStore
{
    private readonly JobSystemInfo _systemInfo = systemInfoProvider.GetSystemInfo();

    public Task<IEnumerable<JobInfoBase>> GetActiveJobs(CancellationToken cancellationToken)
        => Task.FromResult(InMemoryJobStoreCache.Jobs.Values
            .Where(RepositoryExpressions.JobsInProgressExpression(_systemInfo).Compile())
            .Select(x => x.ToJobInfoBase()));

    public Task<IEnumerable<JobInfoBase>> GetJobsToRetry(CancellationToken cancellationToken)
        => Task.FromResult(InMemoryJobStoreCache.Jobs.Values
            .Where(RepositoryExpressions.JobsInProgressExpression(_systemInfo).Compile())
            .Select(x => x.ToJobInfoBase()));
}
