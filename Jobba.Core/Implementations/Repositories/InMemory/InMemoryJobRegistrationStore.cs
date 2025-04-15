using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Interfaces;
using Jobba.Core.Interfaces.Repositories;
using Jobba.Core.Models;

namespace Jobba.Core.Implementations.Repositories.InMemory;

public static class DefaultJobRegistrationStoreCache
{
    public static ConcurrentDictionary<Guid, JobRegistration> Registrations { get; } = new();
}

public class InMemoryJobRegistrationStore(IJobbaGuidGenerator guidGenerator, IJobSystemInfoProvider systemInfoProvider)
    : IJobRegistrationStore
{
    private readonly JobSystemInfo _systemInfo = systemInfoProvider.GetSystemInfo();

    private static Task<JobRegistration> Update(Guid id, Action<JobRegistration> update)
    {
        if (DefaultJobRegistrationStoreCache.Registrations.TryGetValue(id, out var registration) is false)
        {
            return Task.FromResult<JobRegistration>(null);
        }

        update(registration);

        DefaultJobRegistrationStoreCache.Registrations[id] = registration;

        return Task.FromResult(registration);
    }

    public async Task<JobRegistration> RegisterJobAsync(JobRegistration registration,
        CancellationToken cancellationToken)
    {
        var existing = DefaultJobRegistrationStoreCache.Registrations
            .FirstOrDefault(x => x.Value.JobName == registration.JobName)
            .Value;

        if (existing is not null)
        {
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
            if (registration.Id == Guid.Empty)
            {
                registration.Id = await guidGenerator.GenerateGuidAsync(cancellationToken);
            }
        }

        DefaultJobRegistrationStoreCache.Registrations[registration.Id] = registration;

        return registration;
    }

    public Task<JobRegistration> GetJobRegistrationAsync(Guid registrationId, CancellationToken cancellationToken)
        => DefaultJobRegistrationStoreCache.Registrations.TryGetValue(registrationId, out var registration)
            ? Task.FromResult(registration)
            : Task.FromResult<JobRegistration>(null);

    public Task<IEnumerable<JobRegistration>> GetJobsWithCronExpressionsAsync(CancellationToken cancellationToken)
        => Task.FromResult(
            DefaultJobRegistrationStoreCache.Registrations.Values
                .Where(RepositoryExpressions.GetCronJobRegistrationsExpression(_systemInfo).Compile())
                .ToArray()
                .AsEnumerable());

    public Task UpdateNextAndPreviousInvocationDatesAsync(Guid registrationId,
        DateTimeOffset? nextInvocationDate,
        DateTimeOffset? previousInvocationDate,
        CancellationToken cancellationToken)
        => Update(registrationId, registration =>
        {
            registration.NextExecutionDate = nextInvocationDate;
            registration.PreviousExecutionDate = previousInvocationDate;
        });

    public Task<JobRegistration> GetByJobNameAsync(string name, CancellationToken cancellationToken)
    {
        var registration = DefaultJobRegistrationStoreCache.Registrations.Values
            .FirstOrDefault(RepositoryExpressions.GetJobByNameExpression(_systemInfo, name).Compile());

        return Task.FromResult(registration);
    }

    public Task<JobRegistration> RemoveByIdAsync(Guid id, CancellationToken cancellationToken)
        => Task.FromResult(DefaultJobRegistrationStoreCache.Registrations.TryRemove(id, out var registration)
            ? registration
            : null);

    public Task<JobRegistration> SetIsInactiveAsync(Guid registrationId, bool isInactive,
        CancellationToken cancellationToken)
        => Update(registrationId, registration => registration.IsInactive = isInactive);
}
