using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Interfaces.Repositories;
using Jobba.Core.Models;
using Jobba.Store.Mongo.Interfaces;

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

    //todo: implement
    public Task<IEnumerable<JobRegistration>> GetJobsWithCronExpressionsAsync(CancellationToken cancellationToken)
        => null;
}
