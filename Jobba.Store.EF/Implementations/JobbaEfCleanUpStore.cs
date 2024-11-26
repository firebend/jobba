using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Implementations.Repositories;
using Jobba.Core.Interfaces;
using Jobba.Core.Interfaces.Repositories;
using Jobba.Store.EF.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobba.Store.EF.Implementations;

public class JobbaEfCleanUpStore(
    IDbContextProvider dbContextProvider,
    IJobSystemInfoProvider systemInfoProvider,
    ILogger<JobbaEfCleanUpStore> logger)
    : IJobCleanUpStore
{
    public async Task CleanUpJobsAsync(TimeSpan duration, CancellationToken cancellationToken)
    {
        var date = DateTimeOffset.UtcNow.Subtract(duration);

        logger.LogDebug("Cleaning up jobs that have a last progress data <= {Date}", date);

        var filter = RepositoryExpressions.GetCleanUpExpression(systemInfoProvider.GetSystemInfo(), date);

        var dbContext = await dbContextProvider.GetDbContextAsync(cancellationToken);
        var jobsToDelete = await dbContext.Jobs.Where(filter).ToListAsync(cancellationToken);

        if (jobsToDelete.Count > 0)
        {
            logger.LogInformation("Deleting {Count} jobs", jobsToDelete.Count);
            dbContext.Jobs.RemoveRange(jobsToDelete);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        else
        {
            logger.LogInformation("No jobs to delete");
        }
    }
}
