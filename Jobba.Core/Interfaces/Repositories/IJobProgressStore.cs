using System;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Models;
using Jobba.Core.Models.Entities;

namespace Jobba.Core.Interfaces.Repositories;

/// <summary>
/// Encapsulates logic for storing job progress.
/// </summary>
public interface IJobProgressStore
{
    /// <summary>
    /// Logs progress for the job to the store
    /// </summary>
    /// <param name="jobProgress">
    /// A <see cref="JobProgress{TJobState}"/> record detailing progress for the job.
    /// </param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="TJobState">
    /// The type of job state.
    /// </typeparam>
    /// <returns></returns>
    public Task LogProgressAsync<TJobState>(JobProgress<TJobState> jobProgress, CancellationToken cancellationToken)
        where TJobState : IJobState;

    /// <summary>
    /// Retrieves progress for a job by id.
    /// </summary>
    /// <param name="id">
    /// The id of the job.
    /// </param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<JobProgressEntity> GetProgressById(Guid id, CancellationToken cancellationToken);
}
