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
    public Task<JobRegistration> RegisterJobAsync(JobRegistration registration, CancellationToken cancellationToken);

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
    public Task<JobRegistration> GetJobRegistrationAsync(Guid registrationId, CancellationToken cancellationToken);

    /// <summary>
    /// Returns all jobs that have a cron expression configured.
    /// </summary>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns></returns>
    public Task<IEnumerable<JobRegistration>> GetJobsWithCronExpressionsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Updates a registration's next and previous invocation dates.
    /// </summary>
    /// <param name="registrationId">
    /// The registration id.
    /// </param>
    /// <param name="nextInvocationDate">
    /// The next invocation date.
    /// </param>
    /// <param name="previousInvocationDate">
    /// The previous invocation date.
    /// </param>
    /// <param name="cancellationToken"></param>
    /// The cancellation token.
    /// <returns></returns>
    public Task UpdateNextAndPreviousInvocationDatesAsync(Guid registrationId,
        DateTimeOffset? nextInvocationDate,
        DateTimeOffset? previousInvocationDate,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets a job registration by job name.
    /// </summary>
    /// <param name="name">
    /// The job name.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns></returns>
    public Task<JobRegistration> GetByJobNameAsync(string name, CancellationToken cancellationToken);

    /// <summary>
    /// Removes a Job Registration by id.
    /// </summary>
    /// <param name="id">
    /// The Job Registration id.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns>
    /// The removed Job Registration.
    /// </returns>
    public Task<JobRegistration> RemoveByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Sets the IsInactive flag on a Job Registration
    /// </summary>
    /// <param name="registrationId">
    /// The registration id
    /// </param>
    /// <param name="isInactive">
    /// True if the registration should be inactivated; otherwise, false.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns></returns>
    public Task<JobRegistration> SetIsInactiveAsync(Guid registrationId, bool isInactive, CancellationToken cancellationToken);
}
