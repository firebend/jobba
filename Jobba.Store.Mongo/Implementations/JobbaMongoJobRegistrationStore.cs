using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Interfaces;
using Jobba.Core.Interfaces.Repositories;
using Jobba.Core.Models;
using Jobba.Store.Mongo.Interfaces;
using Microsoft.Extensions.Logging;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Jobba.Store.Mongo.Implementations;

public class JobbaMongoJobRegistrationStore : IJobRegistrationStore
{
    private readonly IJobbaMongoRepository<JobRegistration> _repo;
    private readonly IJobbaGuidGenerator _guidGenerator;
    private readonly ILogger<JobbaMongoJobRegistrationStore> _logger;

    public JobbaMongoJobRegistrationStore(IJobbaMongoRepository<JobRegistration> repo,
        IJobbaGuidGenerator guidGenerator,
        ILogger<JobbaMongoJobRegistrationStore> logger)
    {
        _repo = repo;
        _guidGenerator = guidGenerator;
        _logger = logger;
    }

    public async Task<JobRegistration> RegisterJobAsync(JobRegistration registration, CancellationToken cancellationToken)
    {
        Expression<Func<JobRegistration, bool>> jobNameFilter = x => x.JobName == registration.JobName;

        var existing = await _repo.GetFirstOrDefaultAsync(
            jobNameFilter,
            cancellationToken);

        if (existing is not null)
        {
            _logger.LogDebug("Registering job and job already exits {JobName}", registration.JobName);

            if (registration.CronExpression is not null)
            {
                registration.NextExecutionDate = existing.NextExecutionDate;
                registration.PreviousExecutionDate = existing.PreviousExecutionDate;
            }
        }
        else
        {
            BsonClassMap.LookupClassMap(registration.JobParamsType);
            BsonClassMap.LookupClassMap(registration.JobStateType);

            _logger.LogDebug("Registering job and job does not exist {JobName}", registration.JobName);

            if (registration.Id == Guid.Empty)
            {
                registration.Id = await _guidGenerator.GenerateGuidAsync(cancellationToken);
            }
        }

        var updated = await _repo.UpsertAsync(jobNameFilter,
            registration,
            cancellationToken);

        return updated;
    }

    public Task<JobRegistration> GetJobRegistrationAsync(Guid registrationId, CancellationToken cancellationToken)
        => _repo.GetFirstOrDefaultAsync(x => x.Id == registrationId, cancellationToken);

    public async Task<IEnumerable<JobRegistration>> GetJobsWithCronExpressionsAsync(CancellationToken cancellationToken)
        => await _repo.GetAllAsync(x => x.CronExpression != null, cancellationToken);

    public Task UpdateNextAndPreviousInvocationDatesAsync(Guid registrationId,
        DateTimeOffset? nextInvocationDate,
        DateTimeOffset? previousInvocationDate,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Updating next and previous invocation dates for job {JobId} {Next} {Previous}",
            registrationId,
            nextInvocationDate,
            previousInvocationDate);

        return _repo.UpdateAsync(
            registrationId,
            Builders<JobRegistration>.Update
                .Set(x => x.NextExecutionDate, nextInvocationDate)
                .Set(x => x.PreviousExecutionDate, previousInvocationDate),
            cancellationToken);
    }

    public Task<JobRegistration> GetByJobNameAsync(string name, CancellationToken cancellationToken)
        => _repo.GetFirstOrDefaultAsync(x => x.JobName == name, cancellationToken);

    public async Task<JobRegistration> RemoveByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _repo.DeleteManyAsync(x => x.Id == id, cancellationToken);

        return deleted.FirstOrDefault();
    }
}
