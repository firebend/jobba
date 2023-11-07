using System;
using System.Collections.Concurrent;
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
}
