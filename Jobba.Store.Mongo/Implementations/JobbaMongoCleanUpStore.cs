using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Implementations.Repositories;
using Jobba.Core.Interfaces.Repositories;
using Jobba.Core.Models;
using Jobba.Core.Models.Entities;
using Jobba.Store.Mongo.Interfaces;
using Microsoft.Extensions.Logging;

namespace Jobba.Store.Mongo.Implementations;

public class JobbaMongoCleanUpStore : IJobCleanUpStore
{
    private readonly IJobbaMongoRepository<JobEntity> _jobRepo;
    private readonly IJobbaMongoRepository<JobProgressEntity> _jobProgressRepo;
    private readonly ILogger<JobbaMongoCleanUpStore> _logger;

    public JobbaMongoCleanUpStore(IJobbaMongoRepository<JobEntity> jobRepo,
        IJobbaMongoRepository<JobProgressEntity> jobProgressRepo,
        ILogger<JobbaMongoCleanUpStore> logger)
    {
        _jobRepo = jobRepo;
        _jobProgressRepo = jobProgressRepo;
        _logger = logger;
    }

    public async Task CleanUpJobsAsync(TimeSpan duration, CancellationToken cancellationToken)
    {
        var date = DateTimeOffset.UtcNow.Subtract(duration);

        _logger.LogDebug("Cleaning up jobs that have a last progress data <= {Date}", date);

        var filter = RepositoryExpressions.GetCleanUpExpression(date);

        var jobs = await _jobRepo.DeleteManyAsync(filter, cancellationToken);
        var jobIds = jobs.Select(x => x.Id).ToList();

        _logger.LogInformation("Deleted {Count} jobs", jobs.Count);

        await _jobProgressRepo.DeleteManyAsync(x => jobIds.Contains(x.JobId), cancellationToken);
    }
}
