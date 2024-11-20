using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Implementations.Repositories;
using Jobba.Core.Interfaces.Repositories;
using Jobba.Store.EF.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobba.Store.EF.Implementations;

public class JobbaEfCleanUpStore(
    JobbaDbContext dbContext,
    ILogger<JobbaEfCleanUpStore> logger)
    : IJobCleanUpStore
{
    public async Task CleanUpJobsAsync(TimeSpan duration, CancellationToken cancellationToken)
    {
        var date = DateTimeOffset.UtcNow.Subtract(duration);

        logger.LogDebug("Cleaning up jobs that have a last progress data <= {Date}", date);

        var filter = RepositoryExpressions.GetCleanUpExpression(date);

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
