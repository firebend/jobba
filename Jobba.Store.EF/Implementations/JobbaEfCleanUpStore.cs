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

public partial class JobbaEfCleanUpStore(
    IDbContextProvider dbContextProvider,
    IJobSystemInfoProvider systemInfoProvider,
    ILogger<JobbaEfCleanUpStore> logger)
    : IJobCleanUpStore
{
    public async Task CleanUpJobsAsync(TimeSpan duration, int cleanUpBatchSize, CancellationToken cancellationToken)
    {
        var date = DateTimeOffset.UtcNow.Subtract(duration);

        LogCleaningUpJobsThatHaveALastProgressDataDate(date);

        var filter = RepositoryExpressions.GetCleanUpExpression(systemInfoProvider.GetSystemInfo(), date);

        var dbContext = await dbContextProvider.GetDbContextAsync(cancellationToken);
        var isSqlite = dbContext.Database.ProviderName?.Contains("Sqlite") == true;

        int recordsAffected;

        do
        {
            var jobsQuery = dbContext.Jobs.Where(filter);
            jobsQuery = isSqlite ? jobsQuery : jobsQuery.OrderBy(x => x.EnqueuedTime);
            jobsQuery = jobsQuery.Take(cleanUpBatchSize);

            var jobIds = await jobsQuery.Select(x => x.Id).ToArrayAsync(cancellationToken);

            if (jobIds.Length == 0)
            {
                break;
            }

            recordsAffected = await dbContext.Jobs
                .Where(x => jobIds.AsEnumerable().Contains(x.Id))
                .ExecuteDeleteAsync(cancellationToken);

            await dbContext.JobProgress
                .Where(x => jobIds.AsEnumerable().Contains(x.JobId))
                .ExecuteDeleteAsync(cancellationToken);

            LogDeletedRowsFromJobsTable(recordsAffected);

        } while (recordsAffected > 0);
    }

    [LoggerMessage(LogLevel.Debug, "Deleted {Rows} from Jobs Table")]
    partial void LogDeletedRowsFromJobsTable(int rows);

    [LoggerMessage(LogLevel.Debug, "Cleaning up jobs that have a last progress data <= {Date}")]
    partial void LogCleaningUpJobsThatHaveALastProgressDataDate(DateTimeOffset date);
}
