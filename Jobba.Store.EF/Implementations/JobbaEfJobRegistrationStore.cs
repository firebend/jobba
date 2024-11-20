using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Interfaces;
using Jobba.Core.Interfaces.Repositories;
using Jobba.Core.Models;
using Jobba.Store.EF.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobba.Store.EF.Implementations;

public class JobbaEfJobRegistrationStore(
    JobbaDbContext dbContext,
    IJobbaGuidGenerator guidGenerator,
    ILogger<JobbaEfJobRegistrationStore> logger)
    : IJobRegistrationStore
{
    public async Task<JobRegistration> RegisterJobAsync(JobRegistration registration,
        CancellationToken cancellationToken)
    {
        var jobName = registration.JobName;
        var existing = await dbContext.JobRegistrations.Where(x => x.JobName == jobName).FirstOrDefaultAsync(cancellationToken);

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
        CancellationToken cancellationToken) =>
        await dbContext.JobRegistrations.FindAsync([registrationId], cancellationToken);

    private async Task<JobRegistration> GetRequiredJobRegistrationAsync(Guid registrationId,
        CancellationToken cancellationToken)
    {
        var registration = await GetJobRegistrationAsync(registrationId, cancellationToken);

        if (registration is null)
        {
            logger.LogWarning("Job registration not found {JobId}", registrationId);
            throw new InvalidOperationException($"Job registration not found {registrationId}");
        }

        return registration;
    }

    public async Task<IEnumerable<JobRegistration>> GetJobsWithCronExpressionsAsync(CancellationToken cancellationToken)
        => await dbContext.JobRegistrations
            .Where(x => x.CronExpression != null && x.CronExpression != "" && x.IsInactive != true)
            .ToListAsync(cancellationToken);

    public async Task UpdateNextAndPreviousInvocationDatesAsync(Guid registrationId,
        DateTimeOffset? nextInvocationDate,
        DateTimeOffset? previousInvocationDate,
        CancellationToken cancellationToken)
    {
        logger.LogDebug("Updating next and previous invocation dates for job {JobId} {Next} {Previous}",
            registrationId,
            nextInvocationDate,
            previousInvocationDate);

        var registration = await GetRequiredJobRegistrationAsync(registrationId, cancellationToken);

        registration.NextExecutionDate = nextInvocationDate;
        registration.PreviousExecutionDate = previousInvocationDate;

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogDebug("Updated next and previous invocation dates for job {JobId} {Next} {Previous}",
            registrationId,
            nextInvocationDate,
            previousInvocationDate);
    }

    public Task<JobRegistration?> GetByJobNameAsync(string name, CancellationToken cancellationToken)
        => dbContext.JobRegistrations.Where(x => x.JobName == name).FirstOrDefaultAsync(cancellationToken);

    public async Task<JobRegistration?> RemoveByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        logger.LogDebug("Removing job registration {JobId}", id);

        var deleted = await GetRequiredJobRegistrationAsync(id, cancellationToken);

        dbContext.JobRegistrations.Remove(deleted);

        await dbContext.SaveChangesAsync(cancellationToken);

        return deleted;
    }

    public async Task<JobRegistration> SetIsInactiveAsync(Guid registrationId, bool isInactive,
        CancellationToken cancellationToken)
    {
        logger.LogDebug("Setting job registration {JobId} to inactive {IsInactive}", registrationId, isInactive);

        var registration = await GetRequiredJobRegistrationAsync(registrationId, cancellationToken);

        if (registration.IsInactive != isInactive)
        {
            registration.IsInactive = isInactive;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return registration;
    }
}
