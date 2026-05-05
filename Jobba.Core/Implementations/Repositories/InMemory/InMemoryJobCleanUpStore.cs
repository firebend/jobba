using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Interfaces;
using Jobba.Core.Interfaces.Repositories;

namespace Jobba.Core.Implementations.Repositories.InMemory;

public class InMemoryJobCleanUpStore(IJobSystemInfoProvider systemInfoProvider) : IJobCleanUpStore
{
    public Task CleanUpJobsAsync(TimeSpan duration, int cleanUpBatchSize, CancellationToken cancellationToken)
    {
        var date = DateTimeOffset.UtcNow.Subtract(duration);
        var filter = RepositoryExpressions.GetCleanUpExpression(systemInfoProvider.GetSystemInfo(), date);
        var filterFunc = filter.Compile();

        var jobIds = InMemoryJobStoreCache.Jobs.Values
            .Where(filterFunc)
            .Select(x => x.Id)
            .Distinct()
            .ToArray()
            .Chunk(cleanUpBatchSize);

        foreach (var chunk in jobIds)
        {
            foreach (var jobId in chunk)
            {
                InMemoryJobStoreCache.Jobs.Remove(jobId, out _);

                var progressIds = InMemoryJobProgressStoreCache.Progress.Values
                    .Where(x => x.JobId == jobId)
                    .Select(x => x.Id)
                    .Distinct()
                    .ToArray();

                foreach (var progressId in progressIds)
                {
                    InMemoryJobProgressStoreCache.Progress.Remove(progressId, out _);
                }
            }
        }

        return Task.CompletedTask;
    }
}
