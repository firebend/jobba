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

public class InMemoryJobRegistrationStore : IJobRegistrationStore
{
    private readonly IJobbaGuidGenerator _guidGenerator;

    public InMemoryJobRegistrationStore(IJobbaGuidGenerator guidGenerator)
    {
        _guidGenerator = guidGenerator;
    }

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

    public async Task<JobRegistration> RegisterJobAsync(JobRegistration registration, CancellationToken cancellationToken)
    {
        if (registration.Id == Guid.Empty)
        {
            var registrationId = await _guidGenerator.GenerateGuidAsync(cancellationToken);
            registration.Id = registrationId;
        }

        DefaultJobRegistrationStoreCache.Registrations.AddOrUpdate(registration.Id, registration, (_, _) => registration);
        return registration;
    }

    public Task<JobRegistration> GetJobRegistrationAsync(Guid registrationId, CancellationToken cancellationToken)
        => DefaultJobRegistrationStoreCache.Registrations.TryGetValue(registrationId, out var registration)
            ? Task.FromResult(registration)
            : Task.FromResult<JobRegistration>(null);

    public Task<IEnumerable<JobRegistration>> GetJobsWithCronExpressionsAsync(CancellationToken cancellationToken)
        => Task.FromResult(
            DefaultJobRegistrationStoreCache.Registrations
                .Where(x => string.IsNullOrWhiteSpace(x.Value.CronExpression) is false)
                .Where(x => x.Value.IsInactive is false)
                .Select(x => x.Value)
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
        var registration = DefaultJobRegistrationStoreCache.Registrations
            .FirstOrDefault(x => x.Value.JobName == name)
            .Value;

        return Task.FromResult(registration);
    }

    public Task<JobRegistration> RemoveByIdAsync(Guid id, CancellationToken cancellationToken)
        => Task.FromResult(DefaultJobRegistrationStoreCache.Registrations.TryRemove(id, out var registration) ? registration : null);

    public Task<JobRegistration> SetIsInactiveAsync(Guid registrationId, bool isInactive, CancellationToken cancellationToken)
        => Update(registrationId, registration => registration.IsInactive = isInactive);
}
