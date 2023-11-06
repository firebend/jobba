using System;
using System.Threading;
using System.Threading.Tasks;

namespace Jobba.Core.Interfaces;

/// <summary>
/// Encapsulates logic for watching jobs and restarting them if they are faulted or not ran to completion.
/// </summary>
/// <typeparam name="TJobParams">
/// The type of job parameters.
/// </typeparam>
/// <typeparam name="TJobState">
/// The type of job state.
/// </typeparam>
public interface IJobWatcher<TJobParams, TJobState>
{
    /// <summary>
    /// Watch a job and restart it if it is faulted or not ran to completion.
    /// </summary>
    /// <param name="jobId">
    /// The job id.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns></returns>
    Task WatchJobAsync(Guid jobId, CancellationToken cancellationToken);
}
