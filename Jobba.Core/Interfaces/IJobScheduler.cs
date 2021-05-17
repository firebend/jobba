using System;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Models;

namespace Jobba.Core.Interfaces
{
    public interface IJobScheduler
    {
        Task ScheduleJobAsync<TJob, TJobParams, TJobState>(JobRequest<TJobParams, TJobState> request, CancellationToken cancellationToken);

        Task CancelJobAsync(Guid jobId, CancellationToken cancellationToken);
    }
}
