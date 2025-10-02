using System;
using System.Threading;

namespace Jobba.Core.Interfaces;

/// <summary>
/// Encapsulates logic for creating a cancellation token for a job.
/// </summary>
public interface IJobCancellationTokenStore
{
    /// <summary>
    /// Creates a cancellation token for a job.
    /// </summary>
    /// <param name="jobId">
    /// The job id.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns></returns>
    public CancellationToken CreateJobCancellationToken(Guid jobId, CancellationToken cancellationToken);

    /// <summary>
    /// Cancels a job by id.
    /// </summary>
    /// <param name="id">
    /// The id of the job.
    /// </param>
    /// <returns></returns>
    public bool CancelJob(Guid id);

    /// <summary>
    /// Cancels all jobs.
    /// </summary>
    public void CancelAllJobs();

    /// <summary>
    /// Removes a completed job so that it's token will not need to be cancelled later.
    /// </summary>
    /// <param name="id">
    /// The id of the job.
    /// </param>
    /// <returns></returns>
    public bool RemoveCompletedJob(Guid id);
}
