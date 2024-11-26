using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Implementations.Repositories;
using Jobba.Core.Interfaces;
using Jobba.Core.Interfaces.Repositories;
using Jobba.Core.Models;
using Jobba.Store.EF.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobba.Store.EF.Implementations;

public class JobbaEfJobRegistrationStore(
    IDbContextProvider dbContextProvider,
    IJobbaGuidGenerator guidGenerator,
    IJobSystemInfoProvider systemInfoProvider,
    ILogger<JobbaEfJobRegistrationStore> logger)
    : IJobRegistrationStore
{
    private readonly JobSystemInfo _systemInfo = systemInfoProvider.GetSystemInfo();

    public async Task<JobRegistration> RegisterJobAsync(JobRegistration registration,
        CancellationToken cancellationToken)
    {
        var jobName = registration.JobName;
        var dbContext = await dbContextProvider.GetDbContextAsync(cancellationToken);
        var existing = await dbContext.JobRegistrations.Where(x => x.JobName == jobName)
            .FirstOrDefaultAsync(cancellationToken);

        if (existing is not null)
        {
            logger.LogDebug("Registering job and job already exits {JobName}", registration.JobName);

            existing.DefaultMaxNumberOfTries = registration.DefaultMaxNumberOfTries;
            existing.DefaultJobWatchInterval = registration.DefaultJobWatchInterval;
            existing.IsInactive = registration.IsInactive;
            existing.JobType = registration.JobType;
            existing.JobStateType = registration.JobStateType;
            existing.DefaultState = registration.DefaultState;
            existing.JobParamsType = registration.JobParamsType;
            existing.DefaultParams = registration.DefaultParams;
            existing.Description = registration.Description;
            existing.TimeZoneId = registration.TimeZoneId;
            existing.SystemMoniker = registration.SystemMoniker;

            if (registration.CronExpression is not null && registration.CronExpression != existing.CronExpression)
            {
                existing.CronExpression = registration.CronExpression;
                existing.NextExecutionDate = null;
                existing.PreviousExecutionDate = null;
            }

            registration = existing;
        }
        else
        {
            logger.LogDebug("Registering job and job does not exist {JobName}", registration.JobName);

            if (registration.Id == Guid.Empty)
            {
                registration.Id = await guidGenerator.GenerateGuidAsync(cancellationToken);
            }

            dbContext.JobRegistrations.Add(registration);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogDebug("Registered job {JobName} {JobId}", registration.JobName, registration.Id);

        return registration;
    }

    public async Task<JobRegistration?> GetJobRegistrationAsync(Guid registrationId,
        CancellationToken cancellationToken)
    {
        var dbContext = await dbContextProvider.GetDbContextAsync(cancellationToken);
        return await dbContext.JobRegistrations.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == registrationId, cancellationToken);
    }


    private async Task<JobRegistration> GetTrackedJobRegistrationAsync(IJobbaDbContext dbContext, Guid registrationId,
        CancellationToken cancellationToken)
    {
        var registration = await dbContext.JobRegistrations.FindAsync([registrationId], cancellationToken);

        if (registration is null)
        {
            logger.LogWarning("Job registration not found {JobId}", registrationId);
            throw new InvalidOperationException($"Job registration not found {registrationId}");
        }

        return registration;
    }

    public async Task<IEnumerable<JobRegistration>> GetJobsWithCronExpressionsAsync(CancellationToken cancellationToken)
    {
        var dbContext = await dbContextProvider.GetDbContextAsync(cancellationToken);
        return await dbContext.JobRegistrations
            .Where(RepositoryExpressions.GetCronJobRegistrationsExpression(_systemInfo))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateNextAndPreviousInvocationDatesAsync(Guid registrationId,
        DateTimeOffset? nextInvocationDate,
        DateTimeOffset? previousInvocationDate,
        CancellationToken cancellationToken)
    {
        logger.LogDebug("Updating next and previous invocation dates for job {JobId} {Next} {Previous}",
            registrationId,
            nextInvocationDate,
            previousInvocationDate);

        var dbContext = await dbContextProvider.GetDbContextAsync(cancellationToken);
        var registration = await GetTrackedJobRegistrationAsync(dbContext, registrationId, cancellationToken);

        registration.NextExecutionDate = nextInvocationDate;
        registration.PreviousExecutionDate = previousInvocationDate;
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogDebug("Updated next and previous invocation dates for job {JobId} {Next} {Previous}",
            registrationId,
            nextInvocationDate,
            previousInvocationDate);
    }

    public async Task<JobRegistration?> GetByJobNameAsync(string name, CancellationToken cancellationToken)
    {
        var dbContext = await dbContextProvider.GetDbContextAsync(cancellationToken);
        return await dbContext.JobRegistrations.Where(RepositoryExpressions.GetJobByNameExpression(_systemInfo, name))
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<JobRegistration?> RemoveByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        logger.LogDebug("Removing job registration {JobId}", id);

        var dbContext = await dbContextProvider.GetDbContextAsync(cancellationToken);
        var deleted = await GetTrackedJobRegistrationAsync(dbContext, id, cancellationToken);

        dbContext.JobRegistrations.Remove(deleted);

        await dbContext.SaveChangesAsync(cancellationToken);

        return deleted;
    }

    public async Task<JobRegistration> SetIsInactiveAsync(Guid registrationId, bool isInactive,
        CancellationToken cancellationToken)
    {
        logger.LogDebug("Setting job registration {JobId} to inactive {IsInactive}", registrationId, isInactive);

        var dbContext = await dbContextProvider.GetDbContextAsync(cancellationToken);
        var registration = await GetTrackedJobRegistrationAsync(dbContext, registrationId, cancellationToken);

        if (registration.IsInactive != isInactive)
        {
            registration.IsInactive = isInactive;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return registration;
    }
}
