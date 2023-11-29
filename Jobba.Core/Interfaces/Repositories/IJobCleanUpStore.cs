using System;
using System.Threading;
using System.Threading.Tasks;

namespace Jobba.Core.Interfaces.Repositories;

/// <summary>
/// Encapsulates logic for cleaning up previous jobs that have completed.
/// </summary>
public interface IJobCleanUpStore
{
    /// <summary>
    /// Removes jobs given a duration.
    /// </summary>
    /// <param name="duration">
    /// How long the job should be kept before being removed.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token
    /// </param>
    /// <returns></returns>
    Task CleanUpJobsAsync(TimeSpan duration, CancellationToken cancellationToken);
}
