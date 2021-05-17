using System;
using System.Threading;
using System.Threading.Tasks;

namespace Jobba.Core.Interfaces.Subscribers
{
    public interface IJobCancelSubscriber
    {
        Task CancelJob(Guid jobId, CancellationToken cancellationToken);
    }
}
