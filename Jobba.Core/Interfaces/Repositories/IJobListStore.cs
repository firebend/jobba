using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Models;

namespace Jobba.Core.Interfaces.Repositories;

/// <summary>
/// Encapsulates logic for retrieving jobs from the store.
/// </summary>
public interface IJobListStore
{
    /// <summary>
    /// Get jobs that are actively in progress and executing.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<IEnumerable<JobInfoBase>> GetActiveJobs(CancellationToken cancellationToken);

    /// <summary>
    /// Gets jobs that should be retried.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<IEnumerable<JobInfoBase>> GetJobsToRetry(CancellationToken cancellationToken);
}
