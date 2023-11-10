using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Interfaces.Repositories;
using Jobba.Core.Models;
using Jobba.Store.Mongo.Interfaces;
using MongoDB.Driver;

namespace Jobba.Store.Mongo.Implementations;

public class JobbaMongoJobRegistrationStore : IJobRegistrationStore
{
    private readonly IJobbaMongoRepository<JobRegistration> _repo;
    public JobbaMongoJobRegistrationStore(IJobbaMongoRepository<JobRegistration> repo)
    {
        _repo = repo;
    }

    public Task<JobRegistration> RegisterJobAsync(JobRegistration registration, CancellationToken cancellationToken)
        => _repo.UpsertAsync(x => x.JobName == registration.JobName, registration, cancellationToken);

    public Task<JobRegistration> GetJobRegistrationAsync(Guid registrationId, CancellationToken cancellationToken)
        => _repo.GetFirstOrDefaultAsync(x => x.Id == registrationId, cancellationToken);

    public async Task<IEnumerable<JobRegistration>> GetJobsWithCronExpressionsAsync(CancellationToken cancellationToken)
        => await _repo.GetAllAsync(x => x.CronExpression != null, cancellationToken);

    public Task UpdateNextAndPreviousInvocationDatesAsync(Guid registrationId,
        DateTimeOffset? nextInvocationDate,
        DateTimeOffset? previousInvocationDate,
        CancellationToken cancellationToken)
        => _repo.UpdateAsync(
            registrationId,
            Builders<JobRegistration>.Update
                .Set(x => x.NextExecutionDate, nextInvocationDate)
                .Set(x => x.PreviousExecutionDate, previousInvocationDate),
            cancellationToken);
}
