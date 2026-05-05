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

public partial class JobbaMongoCleanUpStore(
    IJobbaMongoRepository<JobEntity> jobRepo,
    IJobbaMongoRepository<JobProgressEntity> jobProgressRepo,
    IJobSystemInfoProvider systemInfoProvider,
    ILogger<JobbaMongoCleanUpStore> logger)
    : IJobCleanUpStore
{
    public async Task CleanUpJobsAsync(TimeSpan duration, int cleanUpBatchSize, CancellationToken cancellationToken)
    {
        var date = DateTimeOffset.UtcNow.Subtract(duration);

        LogCleaningUpJobsThatHaveALastProgressDataDate(date);

        var filter = RepositoryExpressions.GetCleanUpExpression(systemInfoProvider.GetSystemInfo(), date);

        int rowsAffected;

        do
        {

            var jobs = await jobRepo.DeleteManyAsync(filter, cleanUpBatchSize, cancellationToken);
            rowsAffected = jobs.Count;

            if (rowsAffected == 0)
            {
                break;
            }

            var jobIds = jobs.Select(x => x.Id).ToList();

            LogDeletedCountJobs(jobs.Count);

            await jobProgressRepo.DeleteManyAsync(x => jobIds.Contains(x.JobId), cancellationToken);
        } while (rowsAffected > 0);
    }

    [LoggerMessage(LogLevel.Information, "Deleted {Count} jobs")]
    partial void LogDeletedCountJobs(int count);

    [LoggerMessage(LogLevel.Debug, "Cleaning up jobs that have a last progress data <= {Date}")]
    partial void LogCleaningUpJobsThatHaveALastProgressDataDate(DateTimeOffset date);
}
