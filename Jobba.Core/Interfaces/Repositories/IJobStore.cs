using System;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Models;

namespace Jobba.Core.Interfaces.Repositories
{
    public interface IJobStore
    {
        Task<JobInfo<TJobParams, TJobState>> AddJobAsync<TJobParams, TJobState>(JobRequest<TJobParams, TJobState> jobRequest, CancellationToken cancellationToken);

        Task<JobInfo<TJobParams, TJobState>> SetJobAttempts<TJobParams, TJobState>(Guid jobId, int attempts, CancellationToken cancellationToken);

        Task SetJobStatusAsync(Guid jobId, JobStatus status, DateTimeOffset date, CancellationToken cancellationToken);

        Task LogFailureAsync(Guid jobId, Exception ex, CancellationToken cancellationToken);

        Task<JobInfoBase> GetJobByIdAsync(Guid jobId, CancellationToken cancellationToken);

        Task<JobInfo<TJobParams, TJobState>> GetJobByIdAsync<TJobParams, TJobState>(Guid jobId, CancellationToken cancellationToken);
    }
}
