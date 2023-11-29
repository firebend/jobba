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
        CancellationToken cancellationToken)
        where TJobParams : IJobParams
        where TJobState : IJobState;

    /// <summary>
    /// Schedule a job by registration id
    /// </summary>
    /// <param name="registrationId">
    /// The job registration id.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <param name="parameters">
    /// The parameters to pass to the job.
    /// </param>
    /// <param name="state">
    /// The state to pass to the job.
    /// </param>
    /// <typeparam name="TJobParams">
    /// The type of job parameters.
    /// </typeparam>
    /// <typeparam name="TJobState">
    /// The type of job state.
    /// </typeparam>
    /// <returns></returns>
    Task<JobInfo<TJobParams, TJobState>> ScheduleJobAsync<TJobParams, TJobState>(Guid registrationId,
        TJobParams parameters,
        TJobState state,
        CancellationToken cancellationToken)
        where TJobParams : IJobParams
        where TJobState : IJobState;

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
