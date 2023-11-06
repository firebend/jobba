using System;
using System.Threading;
using System.Threading.Tasks;

namespace Jobba.Core.Interfaces;

/// <summary>
/// Encapsulates logic for locking a job so that only one invocation at a time may occur.
/// </summary>
public interface IJobLockService
{
    /// <summary>
    /// Lock a job by id so that other concurrent invocations will not occur.
    /// </summary>
    /// <param name="jobId">
    /// The id of the job.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns></returns>
    ValueTask<IDisposable> LockJobAsync(Guid jobId, CancellationToken cancellationToken);
}
