using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Implementations.Repositories;
using Jobba.Core.Interfaces;
using Jobba.Core.Interfaces.Repositories;
using Jobba.Core.Models.Entities;
using Jobba.Store.Mongo.Interfaces;
using Microsoft.Extensions.Logging;

namespace Jobba.Store.Mongo.Implementations;

public class JobbaMongoCleanUpStore(
    IJobbaMongoRepository<JobEntity> jobRepo,
    IJobbaMongoRepository<JobProgressEntity> jobProgressRepo,
    IJobSystemInfoProvider systemInfoProvider,
    ILogger<JobbaMongoCleanUpStore> logger)
    : IJobCleanUpStore
{
    public async Task CleanUpJobsAsync(TimeSpan duration, CancellationToken cancellationToken)
    {
        var date = DateTimeOffset.UtcNow.Subtract(duration);

        logger.LogDebug("Cleaning up jobs that have a last progress data <= {Date}", date);

        var filter = RepositoryExpressions.GetCleanUpExpression(systemInfoProvider.GetSystemInfo(), date);

        var jobs = await jobRepo.DeleteManyAsync(filter, cancellationToken);
        var jobIds = jobs.Select(x => x.Id).ToList();

        logger.LogInformation("Deleted {Count} jobs", jobs.Count);

        await jobProgressRepo.DeleteManyAsync(x => jobIds.Contains(x.JobId), cancellationToken);
    }
}
