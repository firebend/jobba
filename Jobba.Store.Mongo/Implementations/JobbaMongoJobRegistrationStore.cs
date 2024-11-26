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
using Jobba.Store.Mongo.Interfaces;
using Microsoft.Extensions.Logging;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Jobba.Store.Mongo.Implementations;

public class JobbaMongoJobRegistrationStore(
    IJobbaMongoRepository<JobRegistration> repo,
    IJobbaGuidGenerator guidGenerator,
    IJobSystemInfoProvider systemInfoProvider,
    ILogger<JobbaMongoJobRegistrationStore> logger)
    : IJobRegistrationStore
{
    private readonly JobSystemInfo _systemInfo = systemInfoProvider.GetSystemInfo();

    public async Task<JobRegistration> RegisterJobAsync(JobRegistration registration, CancellationToken cancellationToken)
    {
        Expression<Func<JobRegistration, bool>> jobNameFilter = x => x.JobName == registration.JobName;

        var existing = await repo.GetFirstOrDefaultAsync(
            jobNameFilter,
            cancellationToken);

        if (existing is not null)
        {
            logger.LogDebug("Registering job and job already exits {JobName}", registration.JobName);

            if (registration.CronExpression is not null)
            {
                if (registration.CronExpression == existing.CronExpression)
                {
                    registration.NextExecutionDate = existing.NextExecutionDate;
                    registration.PreviousExecutionDate = existing.PreviousExecutionDate;
                }
                else
                {
                    registration.NextExecutionDate = null;
                    registration.PreviousExecutionDate = null;
                }
            }
        }
        else
        {
            BsonClassMap.LookupClassMap(registration.JobParamsType);
            BsonClassMap.LookupClassMap(registration.JobStateType);

            logger.LogDebug("Registering job and job does not exist {JobName}", registration.JobName);

            if (registration.Id == Guid.Empty)
            {
                registration.Id = await guidGenerator.GenerateGuidAsync(cancellationToken);
            }
        }

        var updated = await repo.UpsertAsync(jobNameFilter,
            registration,
            cancellationToken);

        return updated;
    }

    public Task<JobRegistration> GetJobRegistrationAsync(Guid registrationId, CancellationToken cancellationToken)
        => repo.GetFirstOrDefaultAsync(x => x.Id == registrationId, cancellationToken);

    public async Task<IEnumerable<JobRegistration>> GetJobsWithCronExpressionsAsync(CancellationToken cancellationToken)
        => await repo.GetAllAsync(RepositoryExpressions.GetCronJobRegistrationsExpression(_systemInfo), cancellationToken);

    public Task UpdateNextAndPreviousInvocationDatesAsync(Guid registrationId,
        DateTimeOffset? nextInvocationDate,
        DateTimeOffset? previousInvocationDate,
        CancellationToken cancellationToken)
    {
        logger.LogDebug("Updating next and previous invocation dates for job {JobId} {Next} {Previous}",
            registrationId,
            nextInvocationDate,
            previousInvocationDate);

        return repo.UpdateAsync(
            registrationId,
            Builders<JobRegistration>.Update
                .Set(x => x.NextExecutionDate, nextInvocationDate)
                .Set(x => x.PreviousExecutionDate, previousInvocationDate),
            cancellationToken);
    }

    public Task<JobRegistration> GetByJobNameAsync(string name, CancellationToken cancellationToken)
        => repo.GetFirstOrDefaultAsync(RepositoryExpressions.GetJobByNameExpression(_systemInfo, name), cancellationToken);

    public async Task<JobRegistration> RemoveByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await repo.DeleteManyAsync(x => x.Id == id, cancellationToken);

        return deleted.FirstOrDefault();
    }

    public Task<JobRegistration> SetIsInactiveAsync(Guid registrationId, bool isInactive, CancellationToken cancellationToken)
    {
        logger.LogDebug("Setting job registration {JobId} to inactive {IsInactive}", registrationId, isInactive);

        return repo.UpdateAsync(registrationId,
            Builders<JobRegistration>.Update
                .Set(x => x.IsInactive, isInactive),
            cancellationToken);
    }
}
