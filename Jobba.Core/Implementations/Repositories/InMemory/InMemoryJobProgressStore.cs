using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Extensions;
using Jobba.Core.Interfaces;
using Jobba.Core.Interfaces.Repositories;
using Jobba.Core.Models;
using Jobba.Core.Models.Entities;

namespace Jobba.Core.Implementations.Repositories.InMemory;
internal static class InMemoryJobProgressStoreCache
{
    public static ConcurrentDictionary<Guid, JobProgressEntity> Progress { get; } = new();
}

public class InMemoryJobProgressStore : IJobProgressStore
{
    public Task LogProgressAsync<TJobState>(JobProgress<TJobState> jobProgress, CancellationToken cancellationToken)
        where TJobState : IJobState
    {
        var entity = JobProgressEntity.FromJobProgress(jobProgress);
        entity.Id = entity.Id.Coalesce();
        InMemoryJobProgressStoreCache.Progress.GetOrAdd(entity.Id, entity);
        return Task.CompletedTask;
    }

    public Task<JobProgressEntity> GetProgressById(Guid id, CancellationToken cancellationToken)
    {
        if (InMemoryJobProgressStoreCache.Progress.TryGetValue(id, out var progress))
        {
            return Task.FromResult(progress);
        }

        return Task.FromResult<JobProgressEntity>(null);
    }
}
