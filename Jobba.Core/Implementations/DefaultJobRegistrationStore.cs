using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Interfaces;
using Jobba.Core.Models;

namespace Jobba.Core.Implementations;

public static class DefaultJobRegistrationStoreStatics
{
    public static ConcurrentDictionary<Guid, JobRegistration> Registrations { get; } = new();
}

public class DefaultJobRegistrationStore : IJobRegistrationStore
{
    private static DefaultJobRegistrationStore _instance;
    public static DefaultJobRegistrationStore Instance => _instance ??= new DefaultJobRegistrationStore();

    public Task RegisterJobAsync(JobRegistration registration, CancellationToken cancellationToken)
    {
        DefaultJobRegistrationStoreStatics.Registrations.AddOrUpdate(registration.Id, registration, (_, _) => registration);
        return Task.CompletedTask;
    }

    public Task<JobRegistration> GetJobRegistrationAsync(Guid registrationId, CancellationToken cancellationToken)
    {
        if (DefaultJobRegistrationStoreStatics.Registrations.TryGetValue(registrationId, out var registration))
        {
            return Task.FromResult(registration);
        }

        return Task.FromResult<JobRegistration>(null);
    }
}
