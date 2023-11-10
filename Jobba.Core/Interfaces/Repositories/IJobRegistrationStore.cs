using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Models;

namespace Jobba.Core.Interfaces.Repositories;

/// <summary>
/// Encapsulates logic for registering jobs to the scheduler.
/// </summary>
public interface IJobRegistrationStore
{
    /// <summary>
    /// Register a job with the scheduler.
    /// </summary>
    /// <param name="registration">
    ///     The job registration.
    /// </param>
    /// <param name="cancellationToken">
    ///     The cancellation token.
    /// </param>
    /// <returns></returns>
    Task<JobRegistration> RegisterJobAsync(JobRegistration registration, CancellationToken cancellationToken);

    /// <summary>
    /// Resolves a job registration by id.
    /// </summary>
    /// <param name="registrationId">
    /// The registration id.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns></returns>
    Task<JobRegistration> GetJobRegistrationAsync(Guid registrationId, CancellationToken cancellationToken);

    /// <summary>
    /// Returns all jobs that have a cron expression configured.
    /// </summary>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns></returns>
    Task<IEnumerable<JobRegistration>> GetJobsWithCronExpressionsAsync(CancellationToken cancellationToken);

    Task UpdateNextAndPreviousInvocationDatesAsync(Guid registrationId,
        DateTimeOffset? nextInvocationDate,
        DateTimeOffset? previousInvocationDate,
        CancellationToken cancellationToken);
}
