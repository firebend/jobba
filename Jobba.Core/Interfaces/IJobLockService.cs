using System;
using System.Threading;
using System.Threading.Tasks;

namespace Jobba.Core.Interfaces;
public record SystemLockResult(bool WasLockAcquired, IDisposable Lock);

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
    public ValueTask<IDisposable> LockJobAsync(Guid jobId, CancellationToken cancellationToken);

    /// <summary>
    /// Locks on the provided system moniker so that no other concurrently running system can do an action.
    /// </summary>
    /// <param name="systemMoniker">
    ///     The system moniker to lock on
    /// </param>
    /// <param name="span">
    ///     How long to try and wait for the lock
    /// </param>
    /// <param name="cancellationToken">
    ///     The cancellation token.
    /// </param>
    /// <returns></returns>
    public Task<SystemLockResult> LockSystemAsync(string systemMoniker, TimeSpan span, CancellationToken cancellationToken);
}
