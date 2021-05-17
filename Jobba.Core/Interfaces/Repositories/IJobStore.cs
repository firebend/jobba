using System;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Models;

namespace Jobba.Core.Interfaces.Repositories
{
    public interface IJobStore<TJobParams, TJobState>
    {
        Task<JobInfo<TJobParams, TJobState>> AddJobAsync(JobRequest<TJobParams, TJobState> jobRequest, CancellationToken cancellationToken);

        Task LogProgressAsync(JobProgress<TJobState> jobProgress, CancellationToken cancellationToken);

        Task<JobInfo<TJobParams, TJobState>> GetJobByIdAsync(Guid jobId, CancellationToken cancellationToken);
    }
}
