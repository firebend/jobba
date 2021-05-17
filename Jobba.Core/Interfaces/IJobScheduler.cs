using System;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Models;

namespace Jobba.Core.Interfaces
{
    public interface IJobScheduler
    {
        Task<JobInfo<TJobParams, TJobState>> ScheduleJobAsync<TJobParams, TJobState>(JobRequest<TJobParams, TJobState> request, CancellationToken cancellationToken);

        Task CancelJobAsync(Guid jobId, CancellationToken cancellationToken);
    }
}
