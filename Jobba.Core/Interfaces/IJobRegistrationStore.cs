using System;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Models;

namespace Jobba.Core.Interfaces;

/// <summary>
/// Encapsulates logic for registering jobs to the scheduler.
/// </summary>
public interface IJobRegistrationStore
{
    /// <summary>
    /// Register a job with the scheduler.
    /// </summary>
    /// <param name="registration">
    /// The job registration.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns></returns>
    Task RegisterJobAsync(JobRegistration registration, CancellationToken cancellationToken);

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
}
