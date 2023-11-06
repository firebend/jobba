using System;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Models;

namespace Jobba.Core.Interfaces;

/// <summary>
/// Encapsulates logic for scheduling jobs.
/// </summary>
public interface IJobScheduler
{
    /// <summary>
    /// Schedule a job.
    /// </summary>
    /// <param name="request">
    /// The job request.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <typeparam name="TJobParams">
    /// The type of job parameters.
    /// </typeparam>
    /// <typeparam name="TJobState">
    /// The type of job state.
    /// </typeparam>
    /// <returns></returns>
    Task<JobInfo<TJobParams, TJobState>> ScheduleJobAsync<TJobParams, TJobState>(
        JobRequest<TJobParams, TJobState> request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Cancel a job.
    /// </summary>
    /// <param name="jobId">
    /// The job id.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns></returns>
    Task CancelJobAsync(Guid jobId, CancellationToken cancellationToken);
}
