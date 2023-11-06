using System;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Models;

namespace Jobba.Core.Interfaces.Repositories;

/// <summary>
/// Encapsulates logic for interacting with jobs in the store.
/// </summary>
public interface IJobStore
{
    /// <summary>
    /// Adds a new job to the store.
    /// </summary>
    /// <param name="jobRequest">
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
    Task<JobInfo<TJobParams, TJobState>> AddJobAsync<TJobParams, TJobState>(JobRequest<TJobParams, TJobState> jobRequest, CancellationToken cancellationToken);

    /// <summary>
    /// Sets the job attempts for a given job id
    /// </summary>
    /// <param name="jobId">
    /// The job id.
    /// </param>
    /// <param name="attempts">
    /// The number of attempts.
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
    Task<JobInfo<TJobParams, TJobState>> SetJobAttempts<TJobParams, TJobState>(Guid jobId, int attempts, CancellationToken cancellationToken);

    /// <summary>
    /// Sets the job status for a given job id.
    /// </summary>
    /// <param name="jobId">
    /// The job id.
    /// </param>
    /// <param name="status">
    /// The job status.
    /// </param>
    /// <param name="date">
    /// The date the job status was set.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns></returns>
    Task SetJobStatusAsync(Guid jobId, JobStatus status, DateTimeOffset date, CancellationToken cancellationToken);

    /// <summary>
    /// Logs a failure for a given job id.
    /// </summary>
    /// <param name="jobId">
    /// The job id.
    /// </param>
    /// <param name="ex">
    /// Exception that occurred.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns></returns>
    Task LogFailureAsync(Guid jobId, Exception ex, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a job by id.
    /// </summary>
    /// <param name="jobId">
    /// The job id.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns></returns>
    Task<JobInfoBase> GetJobByIdAsync(Guid jobId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a job by id.
    /// </summary>
    /// <param name="jobId">
    /// The job id.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns></returns>
    Task<JobInfo<TJobParams, TJobState>> GetJobByIdAsync<TJobParams, TJobState>(Guid jobId, CancellationToken cancellationToken);
}
