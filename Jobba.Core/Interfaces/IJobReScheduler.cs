using System.Threading;
using System.Threading.Tasks;

namespace Jobba.Core.Interfaces;

/// <summary>
/// Encapsulates logic for restarting faulted jobs.
/// </summary>
public interface IJobReScheduler
{
    /// <summary>
    /// Restart jobs that are in a faulted state that have not exceeded the maximum number of retries.
    /// </summary>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns></returns>
    public Task RestartFaultedJobsAsync(CancellationToken cancellationToken);
}
