using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Implementations.Repositories;
using Jobba.Core.Interfaces;
using Jobba.Core.Interfaces.Repositories;
using Jobba.Core.Models;
using Jobba.Core.Models.Entities;
using Jobba.Store.EF.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Jobba.Store.EF.Implementations;

public class JobbaEfJobListStore(IDbContextProvider dbContextProvider, IJobSystemInfoProvider systemInfoProvider) : IJobListStore
{
    private readonly JobSystemInfo _systemInfo = systemInfoProvider.GetSystemInfo();

    public Task<IEnumerable<JobInfoBase>> GetActiveJobs(CancellationToken cancellationToken)
        => GetJobInfoBases(RepositoryExpressions.JobsInProgressExpression(_systemInfo), cancellationToken);

    public Task<IEnumerable<JobInfoBase>> GetJobsToRetry(CancellationToken cancellationToken)
        => GetJobInfoBases(RepositoryExpressions.JobRetryExpression(_systemInfo), cancellationToken);

    private async Task<IEnumerable<JobInfoBase>> GetJobInfoBases(Expression<Func<JobEntity, bool>> filter,
        CancellationToken cancellationToken)
    {
        var dbContext = await dbContextProvider.GetDbContextAsync(cancellationToken);
        var activeJobs = await dbContext.Jobs.Where(filter).AsNoTracking().ToListAsync(cancellationToken);

        if (activeJobs.Count == 0)
        {
            return [];
        }

        var jobInfoBases = activeJobs.Select(x => x.ToJobInfoBase()).ToArray();

        return jobInfoBases;
    }
}
